using SanTheThao.Models;

namespace SanTheThao.Services;

public interface INotificationService
{
    int UnreadCount { get; }
    event Action? UnreadChanged;

    void StartPolling();
    void StopPolling();

    Task<List<NotificationItem>> GetAllAsync();
    Task MarkReadAsync(int id);
    Task MarkAllReadAsync();

    // Đặt lại lịch nhắc giờ chơi cho các đơn sắp tới
    Task SyncRemindersAsync();
}