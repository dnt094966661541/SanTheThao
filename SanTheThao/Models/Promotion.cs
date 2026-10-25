using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class Promotion
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public int PercentOff { get; set; }
    public decimal MaxDiscount { get; set; }
    public decimal MinTotal { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public int? VenueId { get; set; }
    public bool IsActive { get; set; } = true;

    public string PeriodText => $"{ValidFrom:dd/MM/yyyy} - {ValidTo:dd/MM/yyyy}";
    public string UsageText => UsageLimit == 0
        ? $"Đã dùng {UsedCount} lượt (không giới hạn)"
        : $"Đã dùng {UsedCount}/{UsageLimit} lượt";
    public string StatusText => IsActive ? "Đang bật" : "Đã tắt";
    public string ToggleText => IsActive ? "Tắt mã" : "Bật mã";
    public Color StatusColor => Color.FromArgb(IsActive ? "#1B8A3A" : "#6B7280");
    public Color StatusBackColor => Color.FromArgb(IsActive ? "#E8F5EC" : "#F0F1F0");
}