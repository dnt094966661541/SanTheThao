using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Sport { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Address { get; set; } = "";
    public string District { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public decimal PricePerHour { get; set; }
    public decimal PeakPricePerHour { get; set; }
    public int PeakStartHour { get; set; } = 17;
    public int PeakEndHour { get; set; } = 22;
    public decimal WeekendPricePerHour { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    public bool IsFavorite { get; set; }
    public string FavoriteText => "❤️ Sân yêu thích";

    public string StatusText => IsActive ? "Đang hoạt động" : "Tạm dừng";
    public string ToggleText => IsActive ? "Tạm dừng" : "Mở lại";
    public Color StatusColor => Color.FromArgb(IsActive ? "#1B8A3A" : "#6B7280");
    public Color StatusBackColor => Color.FromArgb(IsActive ? "#E8F5EC" : "#F0F1F0");

    public bool HasPeak => PeakPricePerHour > 0;
    public bool HasWeekend => WeekendPricePerHour > 0;
    public string PeakText => $"Giờ cao điểm ({PeakStartHour}h - {PeakEndHour}h)";
    public string? ImageUrl { get; set; }
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public string? FullImageUrl => HasImage ? SanTheThao.Services.ApiConfig.BaseUrl + ImageUrl : null;
    // Cùng cách tính với server. Server mới là nơi tính tiền chính thức.
    public decimal PriceAt(DateTime date, int hour)
    {
        var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (weekend && WeekendPricePerHour > 0)
            return WeekendPricePerHour;

        if (PeakPricePerHour > 0 && hour >= PeakStartHour && hour < PeakEndHour)
            return PeakPricePerHour;

        return PricePerHour;
    }
}