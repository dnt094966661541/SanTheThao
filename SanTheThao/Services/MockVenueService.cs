using SanTheThao.Models;

namespace SanTheThao.Services;

public class MockVenueService : IVenueService
{
    public Task<List<Venue>> GetVenuesAsync()
    {
        var list = new List<Venue>
        {
            new() { Id = 1, Name = "Sân cầu lông Thăng Long", Sport = "Cầu lông", Icon = "🏸",
                    Address = "12 Láng Hạ", District = "Đống Đa", PricePerHour = 80000, Rating = 4.6, ReviewCount = 128 },
            new() { Id = 2, Name = "Pickleball Cầu Giấy", Sport = "Pickleball", Icon = "🏓",
                    Address = "45 Trần Thái Tông", District = "Cầu Giấy", PricePerHour = 150000, Rating = 4.8, ReviewCount = 64 },
            new() { Id = 3, Name = "Tennis Tây Hồ", Sport = "Tennis", Icon = "🎾",
                    Address = "8 Xuân Diệu", District = "Tây Hồ", PricePerHour = 220000, Rating = 4.5, ReviewCount = 92 },
            new() { Id = 4, Name = "Sân bóng rổ Hoàng Mai", Sport = "Bóng rổ", Icon = "🏀",
                    Address = "101 Giải Phóng", District = "Hoàng Mai", PricePerHour = 120000, Rating = 4.3, ReviewCount = 37 },
            new() { Id = 5, Name = "Cầu lông Mỹ Đình", Sport = "Cầu lông", Icon = "🏸",
                    Address = "20 Lê Đức Thọ", District = "Nam Từ Liêm", PricePerHour = 90000, Rating = 4.7, ReviewCount = 210 },
        };
        return Task.FromResult(list);
    }
    public async Task<Venue?> GetVenueAsync(int id)
    {
        var all = await GetVenuesAsync();
        return all.FirstOrDefault(v => v.Id == id);
    }

    public Task<List<int>> GetBookedHoursAsync(int venueId, DateTime date)
    {
        // Dữ liệu giả: mỗi sân, mỗi ngày có vài giờ đã bị đặt
        var rng = new Random(venueId * 1000 + date.DayOfYear);
        var booked = Enumerable.Range(6, 16)
                               .Where(_ => rng.Next(100) < 30)
                               .ToList();
        return Task.FromResult(booked);
    }
}