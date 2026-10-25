using System.Globalization;
using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class ApiVenueService(HttpClient http) : IVenueService
{
    public async Task<List<Venue>> GetVenuesAsync() =>
        await http.GetFromJsonAsync<List<Venue>>("api/venues") ?? new();

    public async Task<Venue?> GetVenueAsync(int id) =>
        await http.GetFromJsonAsync<Venue>($"api/venues/{id}");

    public async Task<List<int>> GetBookedHoursAsync(int venueId, DateTime date)
    {
        var d = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await http.GetFromJsonAsync<List<int>>($"api/venues/{venueId}/booked-hours?date={d}") ?? new();
    }
}