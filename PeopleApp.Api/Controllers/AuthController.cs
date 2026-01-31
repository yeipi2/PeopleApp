using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PeopleApp.Api.Data;
using PeopleApp.Api.Dtos.Auth;
using PeopleApp.Api.Models;
using PeopleApp.Api.Services;

namespace PeopleApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly EmailService _emailService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        EmailService emailService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    /// <summary>
    /// Endpoint para registrar un nuevo usuario (envía código de verificación)
    /// POST: /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        // 1) Verificar si el email ya existe
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return Conflict("El email ya está registrado.");

        // 2) Crear usuario PERO sin EmailConfirmed
        var user = new ApplicationUser
        {
            Email = dto.Email,
            UserName = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            EmailConfirmed = false // ❌ NO confirmado aún
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BadRequest(createResult.Errors.Select(e => e.Description));

        var roleResult = await _userManager.AddToRoleAsync(user, RolesNames.User);
        if (!roleResult.Succeeded)
            return StatusCode(500, roleResult.Errors.Select(e => e.Description));

        // 3) Generar código de verificación
        var code = VerificationCodeGenerator.GenerateCode(6);
        user.VerificationCode = code;
        user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userManager.UpdateAsync(user);

        // 4) Enviar email
        var emailSent = await _emailService.SendVerificationCodeAsync(
            user.Email,
            code,
            $"{user.FirstName} {user.LastName}"
        );

        if (!emailSent)
        {
            _logger.LogWarning("No se pudo enviar email de verificación a {Email}", user.Email);
        }

        return Ok(new { message = "Usuario registrado. Verifica tu correo para continuar." });
    }

    /// <summary>
    /// Endpoint para verificar el código de registro
    /// POST: /api/auth/verify-registration
    /// </summary>
    [HttpPost("verify-registration")]
    public async Task<ActionResult<AuthResponseDto>> VerifyRegistration([FromBody] VerifyCodeRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        if (user.EmailConfirmed)
            return BadRequest("El email ya está verificado.");

        if (string.IsNullOrWhiteSpace(user.VerificationCode))
            return BadRequest("No hay código de verificación pendiente.");

        if (user.VerificationCodeExpiry < DateTime.UtcNow)
            return BadRequest("El código ha expirado. Solicita uno nuevo.");

        if (user.VerificationCode != dto.Code.ToUpper())
            return BadRequest("Código inválido.");

        // ✅ Código correcto
        user.EmailConfirmed = true;
        user.VerificationCode = null;
        user.VerificationCodeExpiry = null;
        await _userManager.UpdateAsync(user);

        // Generar token JWT
        var roles = await _userManager.GetRolesAsync(user);
        var auth = _tokenService.CreateToken(user, roles);

        return Ok(auth);
    }

    /// <summary>
    /// Endpoint para reenviar código de verificación
    /// POST: /api/auth/resend-verification
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendCodeRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        if (user.EmailConfirmed)
            return BadRequest("El email ya está verificado.");

        // Generar nuevo código
        var code = VerificationCodeGenerator.GenerateCode(6);
        user.VerificationCode = code;
        user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userManager.UpdateAsync(user);

        // Enviar email
        await _emailService.SendVerificationCodeAsync(
            user.Email,
            code,
            $"{user.FirstName} {user.LastName}"
        );

        return Ok(new { message = "Código reenviado." });
    }

    /// <summary>
    /// Endpoint para login (con soporte 2FA)
    /// POST: /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return Unauthorized("Credenciales inválidas.");

        if (!user.EmailConfirmed)
            return Unauthorized("Debes verificar tu email antes de iniciar sesión.");

        var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!ok)
            return Unauthorized("Credenciales inválidas.");

        // ✅ VERIFICAR SI TIENE 2FA ACTIVADO
        if (user.TwoFactorEmailEnabled)
        {
            _logger.LogInformation("Usuario {Email} tiene 2FA activado", user.Email);

            // Generar código de verificación
            var code = VerificationCodeGenerator.GenerateCode(6);
            user.VerificationCode = code;
            user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            // Enviar código por email
            await _emailService.Send2FACodeAsync(
                user.Email!,
                code,
                $"{user.FirstName} {user.LastName}"
            );

            _logger.LogInformation("Código 2FA enviado a {Email}. Devolviendo requires2FA=true", user.Email);

            // IMPORTANTE: Usar objeto anónimo explícito
            var response = new
            {
                requires2FA = true,
                email = user.Email,
                message = "Código enviado a tu correo."
            };

            _logger.LogInformation("Respuesta: {@Response}", response);

            return Ok(response);
        }

        // Login normal (sin 2FA)
        var roles = await _userManager.GetRolesAsync(user);
        var auth = _tokenService.CreateToken(user, roles);

        return Ok(auth);
    }

    /// <summary>
    /// Endpoint para completar login con 2FA
    /// POST: /api/auth/login-2fa
    /// </summary>
    [HttpPost("login-2fa")]
    public async Task<ActionResult<AuthResponseDto>> LoginWith2FA([FromBody] LoginWith2FARequestDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(user.VerificationCode))
            return BadRequest("No hay código de verificación pendiente.");

        if (user.VerificationCodeExpiry < DateTime.UtcNow)
            return BadRequest("El código ha expirado. Inicia sesión nuevamente.");

        if (user.VerificationCode != dto.Code.ToUpper())
            return BadRequest("Código inválido.");

        // ✅ Código correcto
        user.VerificationCode = null;
        user.VerificationCodeExpiry = null;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var auth = _tokenService.CreateToken(user, roles);

        return Ok(auth);
    }

    /// <summary>
    /// Endpoint para obtener datos del usuario autenticado
    /// GET: /api/auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserMeDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("No se pudo identificar al usuario.");

        return Ok(new UserMeDto
        {
            Id = user.Id,
            Email = user.Email ?? "",
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? "",
            TwoFactorEmailEnabled = user.TwoFactorEmailEnabled
        });
    }

    /// <summary>
    /// Endpoint para activar/desactivar 2FA
    /// POST: /api/auth/toggle-2fa
    /// </summary>
    [HttpPost("toggle-2fa")]
    [Authorize]
    public async Task<IActionResult> Toggle2FA([FromBody] Toggle2FARequestDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("No se pudo identificar al usuario.");

        user.TwoFactorEmailEnabled = dto.Enabled;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = dto.Enabled ? "2FA activado" : "2FA desactivado", enabled = dto.Enabled });
    }



    /// <summary>
    /// Endpoint para login con Google (con soporte 2FA)
    /// POST: /api/auth/google-login
    /// </summary>
    [HttpPost("google-login")]
    public async Task<ActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.IdToken))
                return BadRequest("Token de Google es requerido");

            var payload = await VerifyGoogleTokenAsync(dto.IdToken);
            if (payload == null)
                return BadRequest("Token de Google inválido");

            var user = await _userManager.FindByEmailAsync(payload.Email);

            // Si el usuario no existe, crearlo
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FirstName = payload.GivenName ?? "Usuario",
                    LastName = payload.FamilyName ?? "Google",
                    PhoneNumber = "",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Error creando usuario desde Google: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return StatusCode(500, "No se pudo crear el usuario");
                }

                await _userManager.AddToRoleAsync(user, RolesNames.User);
                _logger.LogInformation("Usuario creado desde Google: {Email}", user.Email);
            }

            // ✅ VERIFICAR SI TIENE 2FA ACTIVADO
            if (user.TwoFactorEmailEnabled)
            {
                _logger.LogInformation("Google Login: Usuario {Email} tiene 2FA activado", user.Email);

                // Generar código de verificación
                var code = VerificationCodeGenerator.GenerateCode(6);
                user.VerificationCode = code;
                user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
                await _userManager.UpdateAsync(user);

                // Enviar código por email
                await _emailService.Send2FACodeAsync(
                    user.Email!,
                    code,
                    $"{user.FirstName} {user.LastName}"
                );

                // ✅ CRITICAL FIX: Crear objeto con propiedades explícitas
                var response = new
                {
                    requires2FA = true,
                    email = user.Email, // ← ESTO ES LO QUE FALTABA
                    message = "Código enviado a tu correo."
                };

                _logger.LogInformation("Google Login: Devolviendo 2FA response para {Email}", user.Email);

                return Ok(response);
            }

            // Login normal sin 2FA
            _logger.LogInformation("Google Login: Usuario {Email} sin 2FA, generando token", user.Email);

            var roles = await _userManager.GetRolesAsync(user);
            var auth = _tokenService.CreateToken(user, roles);

            return Ok(auth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GoogleLogin");
            return StatusCode(500, "Error interno del servidor");
        }
    }



    private async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("ClientId de Google no configurado");
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando token de Google");
            return null;
        }
    }

    /// <summary>
    /// Endpoint para solicitar cambio de contraseña
    /// POST: /api/auth/request-password-change
    /// </summary>
    [HttpPost("request-password-change")]
    [Authorize]
    public async Task<IActionResult> RequestPasswordChange([FromBody] RequestPasswordChangeDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("No se pudo identificar al usuario.");

        // Verificar contraseña actual
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
        if (!isPasswordValid)
            return BadRequest("La contraseña actual es incorrecta.");

        // Generar código de verificación
        var code = VerificationCodeGenerator.GenerateCode(6);
        user.VerificationCode = code;
        user.VerificationCodeExpiry = DateTime.UtcNow.AddMinutes(10);
        await _userManager.UpdateAsync(user);

        // Enviar código por email
        var emailSent = await _emailService.SendPasswordChangeCodeAsync(
            user.Email!,
            code,
            $"{user.FirstName} {user.LastName}"
        );

        if (!emailSent)
        {
            _logger.LogWarning("No se pudo enviar email de cambio de contraseña a {Email}", user.Email);
        }

        return Ok(new { message = "Código enviado a tu correo." });
    }

    /// <summary>
    /// Endpoint para completar cambio de contraseña
    /// POST: /api/auth/verify-password-change
    /// </summary>
    [HttpPost("verify-password-change")]
    [Authorize]
    public async Task<IActionResult> VerifyPasswordChange([FromBody] VerifyPasswordChangeDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("No se pudo identificar al usuario.");

        if (string.IsNullOrWhiteSpace(user.VerificationCode))
            return BadRequest("No hay código de verificación pendiente.");

        if (user.VerificationCodeExpiry < DateTime.UtcNow)
            return BadRequest("El código ha expirado. Solicita uno nuevo.");

        if (user.VerificationCode != dto.Code.ToUpper())
            return BadRequest("Código inválido.");

        // ✅ SOLUCIÓN: Cambiar contraseña directamente sin usar token
        // Primero, removemos el hash de contraseña actual
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, dto.NewPassword);

        // Actualizar el usuario
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            _logger.LogError("Error al actualizar contraseña: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest("No se pudo actualizar la contraseña.");
        }

        // Limpiar código de verificación
        user.VerificationCode = null;
        user.VerificationCodeExpiry = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Contraseña actualizada exitosamente para {Email}", user.Email);

        return Ok(new { message = "Contraseña actualizada exitosamente." });
    }

    /// <summary>
    /// Endpoint para actualizar número de teléfono
    /// POST: /api/auth/update-phone
    /// </summary>
    [HttpPost("update-phone")]
    [Authorize]
    public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized("No se pudo identificar al usuario.");

        user.PhoneNumber = dto.PhoneNumber;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok(new { message = "Teléfono actualizado exitosamente." });
    }

}