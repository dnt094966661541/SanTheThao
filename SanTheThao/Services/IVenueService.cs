using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IVenueService
{
    Task<List<Venue>> GetVenuesAsync();
    Task<Venue?> GetVenueAsync(int id);
    Task<List<int>> GetBookedHoursAsync(int venueId, DateTime date);
}