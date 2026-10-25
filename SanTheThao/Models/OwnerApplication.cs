using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class OwnerApplication
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Note { get; set; } = "";
    public string Status { get; set; } = "";
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";

    public string DateText => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    public bool CanReview => Status == "Chờ duyệt";
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
    public bool HasAdminNote => !string.IsNullOrWhiteSpace(AdminNote);

    public string StatusText => Status switch
    {
        "Chờ duyệt" => "Chờ duyệt",
        "Đã duyệt" => "Đã duyệt",
        "Từ chối" => "Từ chối",
        _ => Status
    };

    public Color StatusColor => Color.FromArgb(
        Status == "Đã duyệt" ? "#1B8A3A" : Status == "Từ chối" ? "#D32F2F" : "#B7791F");

    public Color StatusBackColor => Color.FromArgb(
        Status == "Đã duyệt" ? "#E8F5EC" : Status == "Từ chối" ? "#FDECEA" : "#FEF3D7");
}