public class LoginResultDto
{
    public bool Requires2FA { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
}
