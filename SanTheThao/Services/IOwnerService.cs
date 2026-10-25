using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IOwnerService
{
    Task<List<Venue>> GetVenuesAsync();
    Task<(string? Error, int VenueId)> SaveVenueAsync(Venue venue);      // Error = null là thành công
    Task ToggleVenueAsync(int id);
    Task<List<Booking>> GetBookingsAsync();
    Task<RevenueResult?> GetRevenueAsync(DateTime month);
    Task<(bool Ok, string Message)> CheckInAsync(string code);
    Task<List<Promotion>> GetPromotionsAsync();
    Task<string?> CreatePromotionAsync(Promotion promo);   // null = thành công, ngược lại là nội dung lỗi
    Task TogglePromotionAsync(int id);
    Task<string?> UploadVenueImageAsync(int venueId, byte[] data, string contentType);
}