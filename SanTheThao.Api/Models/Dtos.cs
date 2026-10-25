namespace SanTheThao.Api.Models;

public record CreateBookingRequest(int VenueId, DateTime Date, List<int> Hours, string? PromoCode);

public record BookingDto(
    Guid Id, int VenueId, string VenueName, string Icon, DateTime Date,
    List<int> Hours, decimal TotalPrice, string Status, DateTime CreatedAt, string CustomerName,
    string? PromoCode, decimal DiscountAmount, DateTime? CheckedInAt, bool Reviewed, string? ImageUrl)
{
    public static BookingDto From(Booking b) => new(
        b.Id,
        b.VenueId,
        b.Venue?.Name ?? "",
        b.Venue?.Icon ?? "",
        b.Date,
        b.HoursCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(),
        b.TotalPrice,
        b.Status,
        b.CreatedAt,
        b.User?.FullName ?? "",
        b.PromoCode,
        b.DiscountAmount,
        b.CheckedInAt,
        b.Reviewed,
        b.Venue?.ImageUrl);
}

public record RegisterRequest(string FullName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string FullName, string Email, string Role, DateTime ExpiresAt);
public record VenueRequest(
    string Name, string Sport, string Icon, string Address, string District,
    decimal PricePerHour, decimal PeakPricePerHour, int PeakStartHour, int PeakEndHour,
    decimal WeekendPricePerHour);
public record RevenueDay(DateTime Date, decimal Total, int Count);
public record RevenueResponse(string Month, decimal Total, int BookingCount, List<RevenueDay> PerDay);
public record ReviewRequest(int Rating, string? Comment);
public record ReviewDto(string UserName, int Rating, string Comment, DateTime CreatedAt);
public record PromoInfo(string Code, string Description, int PercentOff, decimal MaxDiscount, decimal MinTotal);
public record CheckInRequest(string Code);
public record PromotionRequest(
    string Code, string? Description, int PercentOff, decimal MaxDiscount, decimal MinTotal,
    DateTime ValidFrom, DateTime ValidTo, int UsageLimit, int? VenueId);
public record UpdateProfileRequest(string FullName);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminStatsResponse(int Customers, int Owners, int Venues, int Bookings, decimal Revenue);
public record AdminUserDto(Guid Id, string FullName, string Email, string Role, bool IsLocked, DateTime CreatedAt);
public record SetRoleRequest(string Role);
public record OwnerApplicationRequest(string BusinessName, string Phone, string Address, string? Note);
public record RejectApplicationRequest(string? Note);
public record OwnerApplicationDto(
    int Id, string BusinessName, string Phone, string Address, string Note,
    string Status, string? AdminNote, DateTime CreatedAt, string UserName, string Email);