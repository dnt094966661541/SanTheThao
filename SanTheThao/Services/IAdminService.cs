using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IAdminService
{
    Task<AdminStats?> GetStatsAsync();
    Task<List<AdminUser>> GetUsersAsync();
    Task<string?> ToggleLockAsync(Guid id);                // null = thành công, ngược lại là nội dung lỗi
    Task<string?> SetRoleAsync(Guid id, string role);
    Task<List<OwnerApplication>> GetApplicationsAsync();
    Task<string?> ApproveApplicationAsync(int id);
    Task<string?> RejectApplicationAsync(int id, string note);
}