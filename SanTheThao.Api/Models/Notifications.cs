namespace SanTheThao.Api.Models;

public class Notification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = "";        // booking, match, owner, review, admin
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Route { get; set; }            // trang mở khi bấm vào, ví dụ "MatchDetail?id=..."
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReadAt { get; set; }
}

public record NotificationDto(int Id, string Type, string Title, string Body, string? Route, DateTime CreatedAt, bool IsRead);
public record NotificationSummaryDto(int Unread, List<NotificationDto> Latest);

public static class Notify
{
    // Chỉ thêm vào bộ nhớ, được lưu cùng lần SaveChanges của chỗ gọi
    public static void Add(Data.AppDbContext db, Guid userId, string type, string title, string body, string? route = null)
    {
        db.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Route = route
        });
    }
}