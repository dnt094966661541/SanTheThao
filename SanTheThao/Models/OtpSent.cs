namespace SanTheThao.Models;

public class OtpSent
{
    public string Identifier { get; set; } = "";
    public string Channel { get; set; } = "email";
    public string Destination { get; set; } = "";
    public string? DevMessage { get; set; }
    public int ExpiresInSeconds { get; set; }
}