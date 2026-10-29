using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }

    public bool ShowName => !IsMine;
    public LayoutOptions Align => IsMine ? LayoutOptions.End : LayoutOptions.Start;
    public Color BubbleColor => Color.FromArgb(IsMine ? "#1B8A3A" : "#FFFFFF");
    public Color TextColor => Color.FromArgb(IsMine ? "#FFFFFF" : "#222222");
    public Color TimeColor => Color.FromArgb(IsMine ? "#DDF2E3" : "#9CA3AF");

    public string TimeText => CreatedAt.Date == DateTime.Today
        ? CreatedAt.ToString("HH:mm")
        : CreatedAt.ToString("dd/MM HH:mm");
}

public class ChatState
{
    public List<ChatMessage> Messages { get; set; } = new();
    public bool CanSend { get; set; }
    public int ParticipantCount { get; set; }
    public string Title { get; set; } = "";
}