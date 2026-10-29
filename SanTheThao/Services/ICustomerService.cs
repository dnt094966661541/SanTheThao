using SanTheThao.Models;

namespace SanTheThao.Services;

public interface ICustomerService
{
    Task<List<Review>> GetReviewsAsync(int venueId);
    Task<string?> SubmitReviewAsync(Guid bookingId, int rating, string comment);   // null = thành công
    Task<(PromoInfo? Info, string? Error)> CheckPromoAsync(string code, int venueId);
    Task<List<int>> GetFavoriteIdsAsync();
    Task<bool> ToggleFavoriteAsync(int venueId);   // true = đang yêu thích
    Task<string?> SubmitOwnerApplicationAsync(string businessName, string phone, string address, string note);
    Task<OwnerApplication?> GetMyApplicationAsync();
    Task<List<VenueImage>> GetVenueImagesAsync(int venueId);
}