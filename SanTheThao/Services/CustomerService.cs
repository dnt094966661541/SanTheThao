using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class CustomerService(HttpClient http) : ICustomerService
{
    public async Task<List<Review>> GetReviewsAsync(int venueId) =>
        await http.GetFromJsonAsync<List<Review>>($"api/venues/{venueId}/reviews") ?? new();

    public async Task<string?> SubmitReviewAsync(Guid bookingId, int rating, string comment)
    {
        try
        {
            var res = await http.PostAsJsonAsync($"api/bookings/{bookingId}/review", new { rating, comment });
            if (res.IsSuccessStatusCode) return null;
            return await ReadError(res, "Không gửi được đánh giá.");
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<(PromoInfo? Info, string? Error)> CheckPromoAsync(string code, int venueId)
    {
        try
        {
            var url = $"api/promotions/{Uri.EscapeDataString(code)}?venueId={venueId}";
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
                return (null, await ReadError(res, "Mã giảm giá không dùng được."));

            return (await res.Content.ReadFromJsonAsync<PromoInfo>(), null);
        }
        catch (Exception)
        {
            return (null, "Không kết nối được máy chủ.");
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage res, string fallback)
    {
        try { return await res.Content.ReadFromJsonAsync<string>() ?? fallback; }
        catch { return fallback; }
    }
    public async Task<List<int>> GetFavoriteIdsAsync() =>
        await http.GetFromJsonAsync<List<int>>("api/favorites") ?? new();

    public async Task<bool> ToggleFavoriteAsync(int venueId)
    {
        var res = await http.PostAsync($"api/favorites/{venueId}/toggle", null);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<bool>();
    }
    public async Task<string?> SubmitOwnerApplicationAsync(string businessName, string phone, string address, string note)
    {
        try
        {
            var res = await http.PostAsJsonAsync("api/owner-applications",
                new { businessName, phone, address, note });
            if (res.IsSuccessStatusCode) return null;
            return await ReadError(res, "Không gửi được hồ sơ.");
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<OwnerApplication?> GetMyApplicationAsync() =>
        await http.GetFromJsonAsync<OwnerApplication>("api/owner-applications/mine");
    public async Task<List<VenueImage>> GetVenueImagesAsync(int venueId) =>
    await http.GetFromJsonAsync<List<VenueImage>>($"api/venues/{venueId}/images") ?? new();
}