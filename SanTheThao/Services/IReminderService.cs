namespace SanTheThao.Services;

public interface IReminderService
{
    Task<bool> EnsurePermissionAsync();
    void Schedule(int id, DateTime at, string title, string body);
    void Cancel(int id);
    void ShowNow(int id, string title, string body);
}