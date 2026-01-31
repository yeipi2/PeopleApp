namespace PeopleApp.Api.Services;

public static class VerificationCodeGenerator
{
    private static readonly Random _random = new();
    private const string _chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Sin caracteres confusos (I, 1, O, 0)

    public static string GenerateCode(int length = 6)
    {
        var code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = _chars[_random.Next(_chars.Length)];
        }
        return new string(code);
    }
}