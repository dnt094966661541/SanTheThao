using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class NotificationItem
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Route { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }

    public bool IsUnread => !IsRead;
    public FontAttributes TitleStyle => IsRead ? FontAttributes.None : FontAttributes.Bold;
    public Color BackColor => Color.FromArgb(IsRead ? "#FFFFFF" : "#F1FAF3");

    public string Icon => Type switch
    {
        "booking" => "📅",
        "match" => "🤝",
        "owner" => "🏟️",
        "review" => "⭐",
        "admin" => "🛡️",
        "chat" => "💬",
        _ => "🔔"
    };

    public string TimeText
    {
        get
        {
            var span = DateTime.Now - CreatedAt;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return CreatedAt.ToString("dd/MM/yyyy");
        }
    }
}

public class NotificationSummary
{
    public int Unread { get; set; }
    public List<NotificationItem> Latest { get; set; } = new();
}