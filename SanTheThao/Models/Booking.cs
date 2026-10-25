using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int VenueId { get; set; }
    public string VenueName { get; set; } = "";
    public string Icon { get; set; } = "";
    public DateTime Date { get; set; }
    public List<int> Hours { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Đã đặt";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CustomerName { get; set; } = "";
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool Reviewed { get; set; }

    public string Code => Id.ToString("N")[..8].ToUpper();

    public string HoursText =>
        string.Join(", ", Hours.OrderBy(h => h).Select(h => $"{h:00}h-{h + 1:00}h"));

    public DateTime EndTime =>
        Date.Date.AddHours((Hours.Count == 0 ? 0 : Hours.Max()) + 1);

    public bool IsCheckedIn => CheckedInAt != null;
    public bool HasDiscount => DiscountAmount > 0;
    public string DiscountText => $"Đã giảm {DiscountAmount:N0} đ (mã {PromoCode})";

    public bool IsCancelled => Status == "Đã hủy";
    public bool IsDone => Status == "Đã đặt" && EndTime <= DateTime.Now;

    public string StatusLabel =>
        IsCancelled ? "Đã hủy" :
        IsDone ? "Hoàn thành" :
        IsCheckedIn ? "Đã check-in" : "Sắp tới";

    public Color StatusColor => Color.FromArgb(
        IsCancelled ? "#D32F2F" : IsDone ? "#1B8A3A" : "#B7791F");

    public Color StatusBackColor => Color.FromArgb(
        IsCancelled ? "#FDECEA" : IsDone ? "#E8F5EC" : "#FEF3D7");

    public bool CanCancel => Status == "Đã đặt" && CheckedInAt == null && Date.Date >= DateTime.Today;
    public bool CanShowQr => Status == "Đã đặt" && CheckedInAt == null && EndTime > DateTime.Now;
    public bool CanReview => Status == "Đã đặt" && !Reviewed && EndTime <= DateTime.Now;
    public string? ImageUrl { get; set; }
    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);
    public string? FullImageUrl => HasImage ? SanTheThao.Services.ApiConfig.BaseUrl + ImageUrl : null;
}