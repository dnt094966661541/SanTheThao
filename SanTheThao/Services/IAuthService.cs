using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IAuthService
{
    AuthResponse? CurrentUser { get; }
    string RoleText { get; }
    Task<bool> TryRestoreAsync();
    Task<string?> LoginAsync(string email, string password);
    Task<(string? Error, OtpSent? Sent)> RegisterAsync(string fullName, string email, string phone, string password, string channel);
    Task<(string? Error, OtpSent? Sent)> SendOtpAsync(string identifier, string channel);
    Task<string?> VerifyOtpAsync(string identifier, string code);
    void Logout();
    Task<string?> UpdateProfileAsync(string fullName);
    Task<string?> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<string?> RefreshAsync();

}