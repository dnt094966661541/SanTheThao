using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IAuthService
{
    AuthResponse? CurrentUser { get; }
    string RoleText { get; }
    Task<bool> TryRestoreAsync();
    Task<string?> LoginAsync(string email, string password);
    Task<string?> RegisterAsync(string fullName, string email, string password);
    void Logout();
    Task<string?> UpdateProfileAsync(string fullName);
    Task<string?> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<string?> RefreshAsync();

}