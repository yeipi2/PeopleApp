using PeopleApp.Client.Auth;
using PeopleApp.Client.Dtos;
using System.Text.Json;

namespace PeopleApp.Client.Services.Auth;

public class AuthService
{
    private readonly AuthApiClient _authApiClient;
    private readonly ITokenStore _tokenStore;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthService(AuthApiClient authApiClient, ITokenStore tokenStore, JwtAuthenticationStateProvider authStateProvider)
    {
        _authApiClient = authApiClient;
        _tokenStore = tokenStore;
        _authStateProvider = authStateProvider;
    }

    // ✅ NUEVO: Registro con verificación
    public async Task RegisterWithVerificationAsync(RegisterRequestDto dto)
    {
        await _authApiClient.RegisterWithVerificationAsync(dto);
    }

    // ✅ NUEVO: Verificar código de registro
    public async Task VerifyRegistrationAsync(VerifyCodeRequestDto dto)
    {
        var authResponse = await _authApiClient.VerifyRegistrationAsync(dto);
        await _tokenStore.SetTokenAsync(authResponse.Token);
        _authStateProvider.NotifyUserAuthentication(authResponse.Token);
    }

    // ✅ NUEVO: Reenviar código
    public async Task ResendVerificationAsync(ResendCodeRequestDto dto)
    {
        await _authApiClient.ResendVerificationAsync(dto);
    }

    // ✅ MODIFICADO: Login con soporte 2FA
    public async Task<LoginResult> LoginWithPossible2FAAsync(LoginRequestDto dto)
    {
        var result = await _authApiClient.LoginWithPossible2FAAsync(dto);

        // ✅ Caso 1: Requiere 2FA (el backend devuelve un objeto con "requires2FA")
        if (result is JsonElement jsonElement)
        {
            if (jsonElement.TryGetProperty("requires2FA", out var requires2FA) && requires2FA.GetBoolean())
            {
                return new LoginResult { Requires2FA = true };
            }
        }
        else if (result is Dictionary<string, object> dict)
        {
            if (dict.ContainsKey("requires2FA"))
            {
                return new LoginResult { Requires2FA = true };
            }
        }

        // ✅ Caso 2: Login exitoso SIN 2FA (el backend devuelve AuthResponseDto)
        if (result is AuthResponseDto authResponse)
        {
            await _tokenStore.SetTokenAsync(authResponse.Token);
            _authStateProvider.NotifyUserAuthentication(authResponse.Token);
            return new LoginResult { Requires2FA = false };
        }

        // ✅ Caso 3: Si result es JsonElement y tiene "token", convertirlo a AuthResponseDto
        if (result is JsonElement jsonToken)
        {
            if (jsonToken.TryGetProperty("token", out var tokenProp))
            {
                var token = tokenProp.GetString();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await _tokenStore.SetTokenAsync(token);
                    _authStateProvider.NotifyUserAuthentication(token);
                    return new LoginResult { Requires2FA = false };
                }
            }
        }

        throw new InvalidOperationException("Respuesta de login inválida");
    }

    // ✅ NUEVO: Completar login con 2FA
    public async Task LoginWith2FAAsync(LoginWith2FARequestDto dto)
    {
        var authResponse = await _authApiClient.LoginWith2FAAsync(dto);
        await _tokenStore.SetTokenAsync(authResponse.Token);
        _authStateProvider.NotifyUserAuthentication(authResponse.Token);
    }

    // ✅ NUEVO: Toggle 2FA
    public async Task Toggle2FAAsync(bool enabled)
    {
        await _authApiClient.Toggle2FAAsync(new Toggle2FARequestDto { Enabled = enabled });
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _tokenStore.ClearAsync();
            _authStateProvider.NotifyUserLogout();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al cerrar sesión: {ex.Message}", ex);
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _tokenStore.GetTokenAsync();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _tokenStore.GetTokenAsync();
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<UserMeDto> GetUserAsync()
    {
        return await _authApiClient.MeAsync();
    }

    public async Task<LoginResult> GoogleLoginAsync(string idToken)
    {
        try
        {
            Console.WriteLine("[AuthService.GoogleLoginAsync] Starting...");

            var dto = new GoogleLoginRequestDto { IdToken = idToken };
            var result = await _authApiClient.GoogleLoginAsync(dto);

            Console.WriteLine($"[AuthService] Result type: {result?.GetType().Name}");

            // Caso 1: Dictionary con requires2FA
            if (result is Dictionary<string, object> dict)
            {
                Console.WriteLine($"[AuthService] Processing Dictionary with {dict.Count} keys");

                foreach (var key in dict.Keys)
                {
                    Console.WriteLine($"[AuthService] Key: {key}, Value: {dict[key]}, Type: {dict[key]?.GetType().Name}");
                }

                if (dict.TryGetValue("requires2FA", out var req2FAObj) && req2FAObj is bool req2FA && req2FA)
                {
                    var email = dict.TryGetValue("email", out var emailObj) ? emailObj?.ToString() : null;
                    Console.WriteLine($"[AuthService] 2FA Required. Email: '{email}'");

                    return new LoginResult
                    {
                        Requires2FA = true,
                        Email = email
                    };
                }
            }

            // Caso 2: AuthResponseDto (login sin 2FA)
            if (result is AuthResponseDto authResponse)
            {
                Console.WriteLine("[AuthService] Login successful without 2FA");
                await _tokenStore.SetTokenAsync(authResponse.Token);
                _authStateProvider.NotifyUserAuthentication(authResponse.Token);
                return new LoginResult { Requires2FA = false };
            }

            Console.WriteLine("[AuthService] ERROR: Unhandled result type");
            throw new InvalidOperationException("Respuesta de Google login inválida");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] EXCEPTION: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[AuthService] StackTrace: {ex.StackTrace}");
            throw new Exception($"Error en Google login: {ex.Message}", ex);
        }
    }

    public async Task RequestPasswordChangeAsync(RequestPasswordChangeDto dto)
    {
        await _authApiClient.RequestPasswordChangeAsync(dto);
    }

    public async Task VerifyPasswordChangeAsync(VerifyPasswordChangeDto dto)
    {
        await _authApiClient.VerifyPasswordChangeAsync(dto);
    }

    public async Task UpdatePhoneAsync(UpdatePhoneDto dto)
    {
        await _authApiClient.UpdatePhoneAsync(dto);
    }

    public async Task SetPasswordAsync(SetPasswordDto dto)
    {
        await _authApiClient.SetPasswordAsync(dto);
    }

}

// ✅ CLASE ACTUALIZADA
public class LoginResult
{
    public bool Requires2FA { get; set; }
    public string? Email { get; set; }
}