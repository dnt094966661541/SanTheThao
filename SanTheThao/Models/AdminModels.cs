using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class AdminStats
{
    public int Customers { get; set; }
    public int Owners { get; set; }
    public int Venues { get; set; }
    public int Bookings { get; set; }
    public decimal Revenue { get; set; }
}

public class AdminUser
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }

    public string RoleText => Role switch
    {
        "Admin" => "Quản trị viên",
        "Owner" => "Chủ sân",
        _ => "Khách hàng"
    };

    public string StatusText => IsLocked ? "Đã khóa" : "Đang hoạt động";
    public Color StatusColor => Color.FromArgb(IsLocked ? "#D32F2F" : "#1B8A3A");
    public Color StatusBackColor => Color.FromArgb(IsLocked ? "#FDECEA" : "#E8F5EC");
    public string LockButtonText => IsLocked ? "Mở khóa" : "Khóa";
    public bool CanManage => Role != "Admin";
}