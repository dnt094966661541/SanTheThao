using System.Globalization;
using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class ApiBookingService(HttpClient http) : IBookingService
{
    public async Task<string?> CreateAsync(Booking booking)
    {
        var req = new
        {
            venueId = booking.VenueId,
            date = booking.Date,
            hours = booking.Hours,
            promoCode = booking.PromoCode
        };

        try
        {
            var res = await http.PostAsJsonAsync("api/bookings", req);
            if (!res.IsSuccessStatusCode)
            {
                try { return await res.Content.ReadFromJsonAsync<string>() ?? "Không đặt được sân."; }
                catch { return "Không đặt được sân. Bạn thử đăng nhập lại."; }
            }

            // Lấy mã đơn và số tiền thật do server tính
            var created = await res.Content.ReadFromJsonAsync<Booking>();
            if (created != null)
            {
                booking.Id = created.Id;
                booking.TotalPrice = created.TotalPrice;
                booking.DiscountAmount = created.DiscountAmount;
            }
            return null;
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<List<int>> GetBookedHoursAsync(int venueId, DateTime date)
    {
        var d = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await http.GetFromJsonAsync<List<int>>($"api/venues/{venueId}/booked-hours?date={d}") ?? new();
    }

    public async Task<List<Booking>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<Booking>>("api/bookings") ?? new();

    public async Task CancelAsync(Guid id)
    {
        var res = await http.PostAsync($"api/bookings/{id}/cancel", null);
        res.EnsureSuccessStatusCode();
    }
}