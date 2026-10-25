using SanTheThao.Models;

namespace SanTheThao.Services;

public interface IBookingService
{
    Task<string?> CreateAsync(Booking booking);     // null = thành công, ngược lại là nội dung lỗi
    Task<List<int>> GetBookedHoursAsync(int venueId, DateTime date);
    Task<List<Booking>> GetAllAsync();
    Task CancelAsync(Guid id);
}