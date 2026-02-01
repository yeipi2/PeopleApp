using System.Net.Http.Json;
using System.Text.Json;
using PeopleApp.Client.Dtos;
using PeopleApp.Client.Services.Http;


namespace PeopleApp.Client.Services.Auth;

public class AuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? throw new InvalidOperationException("No se recibió token en la respuesta.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? throw new InvalidOperationException("No se recibió token en la respuesta.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task<UserMeDto> MeAsync()
    {
        var response = await _httpClient.GetAsync("api/auth/me");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<UserMeDto>();
            return result ?? throw new InvalidOperationException("No se recibieron datos del usuario.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }
    public async Task<object> GoogleLoginAsync(GoogleLoginRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/google-login", dto);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[GoogleLogin Raw Response]: {jsonString}");

            try
            {
                // Deserializar usando JsonDocument
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                // Verificar si tiene requires2FA
                if (root.TryGetProperty("requires2FA", out var requires2FAProp))
                {
                    var requires2FA = requires2FAProp.GetBoolean();
                    var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                    var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "";

                    Console.WriteLine($"[GoogleLogin] requires2FA={requires2FA}, email={email}");

                    if (requires2FA)
                    {
                        return new Dictionary<string, object>
                        {
                            ["requires2FA"] = true,
                            ["email"] = email ?? "",
                            ["message"] = message ?? ""
                        };
                    }
                }

                // Si tiene token, es login exitoso
                if (root.TryGetProperty("token", out var tokenProp))
                {
                    var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Console.WriteLine($"[GoogleLogin] Token received");
                    return authResponse ?? throw new InvalidOperationException("No se pudo deserializar el token.");
                }

                throw new InvalidOperationException($"Respuesta inesperada: {jsonString}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GoogleLogin] JSON Parse Error: {ex.Message}");
                throw new InvalidOperationException($"Error al parsear respuesta: {ex.Message}");
            }
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task<object> RegisterWithVerificationAsync(RegisterRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/register", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<object>();
            return result ?? throw new InvalidOperationException("No se recibió respuesta.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task<AuthResponseDto> VerifyRegistrationAsync(VerifyCodeRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/verify-registration", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? throw new InvalidOperationException("No se recibió token.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task ResendVerificationAsync(ResendCodeRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/resend-verification", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }

    public async Task<object> LoginWithPossible2FAAsync(LoginRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", dto);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();

            // ✅ Primero intentar deserializar como JsonElement para inspeccionar
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // ✅ Caso 1: Tiene la propiedad "requires2FA"
            if (root.TryGetProperty("requires2FA", out var requires2FA))
            {
                return new Dictionary<string, object>
                {
                    ["requires2FA"] = requires2FA.GetBoolean(),
                    ["message"] = root.TryGetProperty("message", out var messageElement)
                        ? messageElement.GetString() ?? ""
                        : ""
                };
            }

            // ✅ Caso 2: Es un AuthResponseDto (tiene "token")
            if (root.TryGetProperty("token", out var token))
            {
                var authResponse = System.Text.Json.JsonSerializer.Deserialize<AuthResponseDto>(jsonString, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return authResponse ?? throw new InvalidOperationException("No se pudo deserializar el token.");
            }

            throw new InvalidOperationException("Respuesta inesperada del servidor.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task<AuthResponseDto> LoginWith2FAAsync(LoginWith2FARequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login-2fa", dto);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return result ?? throw new InvalidOperationException("No se recibió token.");
        }

        var msg = await ApiErrorParser.ToUserMessageAsync(response);
        throw new HttpRequestException(msg);
    }

    public async Task Toggle2FAAsync(Toggle2FARequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/toggle-2fa", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }

    public async Task RequestPasswordChangeAsync(RequestPasswordChangeDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/request-password-change", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }

    public async Task VerifyPasswordChangeAsync(VerifyPasswordChangeDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/verify-password-change", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }

    public async Task UpdatePhoneAsync(UpdatePhoneDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/update-phone", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }
    public async Task SetPasswordAsync(SetPasswordDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/set-password", dto);

        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorParser.ToUserMessageAsync(response);
            throw new HttpRequestException(msg);
        }
    }

}
