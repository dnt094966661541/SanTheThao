using System.Globalization;
using System.Net.Http.Json;
using SanTheThao.Models;
using System.Net.Http.Headers;

namespace SanTheThao.Services;

public class OwnerService(HttpClient http) : IOwnerService
{
    public async Task<List<Venue>> GetVenuesAsync() =>
        await http.GetFromJsonAsync<List<Venue>>("api/owner/venues") ?? new();

    public async Task<(string? Error, int VenueId)> SaveVenueAsync(Venue v)
    {
        var body = new
        {
            v.Name,
            v.Sport,
            v.Icon,
            v.Address,
            v.District,
            v.PricePerHour,
            v.PeakPricePerHour,
            v.PeakStartHour,
            v.PeakEndHour,
            v.WeekendPricePerHour
        };
        try
        {
            var res = v.Id == 0
                ? await http.PostAsJsonAsync("api/owner/venues", body)
                : await http.PutAsJsonAsync($"api/owner/venues/{v.Id}", body);
            if (!res.IsSuccessStatusCode)
            {
                try { return (await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra.", 0); }
                catch { return ("Có lỗi xảy ra.", 0); }
            }
            var saved = await res.Content.ReadFromJsonAsync<Venue>();
            return (null, saved?.Id ?? v.Id);
        }
        catch (Exception)
        {
            return ("Không kết nối được máy chủ.", 0);
        }
    }

    public async Task<string?> UploadVenueImageAsync(int venueId, byte[] data, string contentType)
    {
        try
        {
            using var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var res = await http.PostAsync($"api/owner/venues/{venueId}/images", content);
            if (res.IsSuccessStatusCode) return null;
            try { return await res.Content.ReadFromJsonAsync<string>() ?? "Không tải được ảnh."; }
            catch { return "Không tải được ảnh."; }
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task ToggleVenueAsync(int id)
    {
        var res = await http.PostAsync($"api/owner/venues/{id}/toggle", null);
        res.EnsureSuccessStatusCode();
    }

    public async Task<List<Booking>> GetBookingsAsync() =>
        await http.GetFromJsonAsync<List<Booking>>("api/owner/bookings") ?? new();

    public async Task<RevenueResult?> GetRevenueAsync(DateTime month)
    {
        var m = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        return await http.GetFromJsonAsync<RevenueResult>($"api/owner/revenue?month={m}");
    }

    public async Task<(bool Ok, string Message)> CheckInAsync(string code)
    {
        try
        {
            var res = await http.PostAsJsonAsync("api/owner/checkin", new { code });
            if (!res.IsSuccessStatusCode)
            {
                try { return (false, await res.Content.ReadFromJsonAsync<string>() ?? "Không check-in được."); }
                catch { return (false, "Không check-in được."); }
            }

            var b = await res.Content.ReadFromJsonAsync<Booking>();
            return (true, b == null
                ? "Check-in thành công."
                : $"{b.CustomerName} · {b.VenueName}\n{b.Date:dd/MM/yyyy} · {b.HoursText}");
        }
        catch (Exception)
        {
            return (false, "Không kết nối được máy chủ.");
        }
    }

    public async Task<List<Promotion>> GetPromotionsAsync() =>
        await http.GetFromJsonAsync<List<Promotion>>("api/owner/promotions") ?? new();

    public async Task<string?> CreatePromotionAsync(Promotion p)
    {
        var body = new
        {
            p.Code,
            p.Description,
            p.PercentOff,
            p.MaxDiscount,
            p.MinTotal,
            p.ValidFrom,
            p.ValidTo,
            p.UsageLimit,
            p.VenueId
        };

        try
        {
            var res = await http.PostAsJsonAsync("api/owner/promotions", body);
            if (res.IsSuccessStatusCode) return null;

            try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
            catch { return "Có lỗi xảy ra."; }
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task TogglePromotionAsync(int id)
    {
        var res = await http.PostAsync($"api/owner/promotions/{id}/toggle", null);
        res.EnsureSuccessStatusCode();
    }
    public async Task<string?> DeleteVenueImageAsync(int venueId, int imageId)
    {
        try
        {
            var res = await http.DeleteAsync($"api/owner/venues/{venueId}/images/{imageId}");
            return res.IsSuccessStatusCode ? null : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<string?> SetMainVenueImageAsync(int venueId, int imageId)
    {
        try
        {
            var res = await http.PostAsync($"api/owner/venues/{venueId}/images/{imageId}/main", null);
            return res.IsSuccessStatusCode ? null : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage res)
    {
        try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
        catch { return "Có lỗi xảy ra."; }
    }
}