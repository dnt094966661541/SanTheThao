using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class RecurringEndpoints
{
    public static void MapRecurringEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/recurring").RequireAuthorization();

        g.MapPost("/", async (RecurringRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            if (uid is null) return Results.Unauthorized();

            var hours = req.Hours.Distinct().OrderBy(h => h).ToList();
            if (hours.Count == 0 || hours.Any(h => h < 6 || h > 21))
                return Results.BadRequest("Khung giờ không hợp lệ.");
            if (req.Weeks < 2 || req.Weeks > 12)
                return Results.BadRequest("Số tuần phải từ 2 đến 12.");

            var start = req.StartDate.Date;
            if (start < DateTime.Today)
                return Results.BadRequest("Ngày bắt đầu không hợp lệ.");

            var venue = await db.Venues.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == req.VenueId && v.IsActive);
            if (venue is null) return Results.NotFound("Không tìm thấy sân.");

            var dates = Enumerable.Range(0, req.Weeks).Select(i => start.AddDays(7 * i)).ToList();
            var taken = await db.BookingSlots.AsNoTracking()
                .Where(s => s.VenueId == venue.Id && dates.Contains(s.Date)).ToListAsync();

            var rec = new RecurringBooking
            {
                UserId = uid.Value,
                VenueId = venue.Id,
                Weekday = (int)start.DayOfWeek,
                HoursCsv = string.Join(",", hours),
                StartDate = start,
                Weeks = req.Weeks
            };

            var now = DateTime.Now;
            var created = new List<Booking>();
            var skipped = new List<string>();

            foreach (var date in dates)
            {
                if (hours.Any(h => date.AddHours(h + 1) <= now))
                {
                    skipped.Add($"{date:dd/MM/yyyy} (đã qua giờ)");
                    continue;
                }
                if (hours.Any(h => taken.Any(t => t.Date == date && t.Hour == h)))
                {
                    skipped.Add($"{date:dd/MM/yyyy} (trùng giờ)");
                    continue;
                }

                created.Add(new Booking
                {
                    UserId = uid.Value,
                    VenueId = venue.Id,
                    Date = date,
                    HoursCsv = rec.HoursCsv,
                    TotalPrice = hours.Sum(h => Pricing.HourPrice(venue, date, h)),
                    RecurringId = rec.Id,
                    Slots = hours.Select(h => new BookingSlot { VenueId = venue.Id, Date = date, Hour = h }).ToList()
                });
            }

            if (created.Count == 0)
                return Results.BadRequest("Tất cả các tuần đều trùng giờ hoặc đã qua. Bạn hãy chọn khung giờ khác.");

            db.RecurringBookings.Add(rec);
            db.Bookings.AddRange(created);
            try
            {
                await db.SaveChangesAsync();   // một lần lưu duy nhất: hoặc được hết, hoặc không được buổi nào
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Có khung giờ vừa bị người khác đặt. Bạn hãy thử lại.");
            }

            return Results.Ok(new RecurringResult(rec.Id, created.Count, created.Sum(b => b.TotalPrice), skipped));
        });

        g.MapGet("/", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var list = await db.RecurringBookings.AsNoTracking()
                .Where(r => r.UserId == uid).OrderByDescending(r => r.CreatedAt).ToListAsync();

            var ids = list.Select(r => r.Id).ToList();
            var venueIds = list.Select(r => r.VenueId).Distinct().ToList();
            var venues = await db.Venues.AsNoTracking().Where(v => venueIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id);
            var bookings = await db.Bookings.AsNoTracking()
                .Where(b => b.RecurringId != null && ids.Contains(b.RecurringId.Value))
                .ToListAsync();

            var now = DateTime.Now;
            return list.Select(r =>
            {
                var mine = bookings.Where(b => b.RecurringId == r.Id).ToList();
                var venue = venues.GetValueOrDefault(r.VenueId);
                return new RecurringDto(
                    r.Id,
                    venue?.Name ?? "",
                    venue?.Icon ?? "",
                    r.Weekday,
                    r.HoursCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(),
                    r.StartDate,
                    r.Weeks,
                    mine.Count(b => b.Status == "Đã đặt" && BookingTime.EndOf(b) > now),
                    mine.Count(b => b.Status == "Đã đặt" && BookingTime.EndOf(b) <= now),
                    mine.Count(b => b.Status == "Đã hủy"));
            }).ToList();
        });

        // Hủy các buổi chưa đến của một lịch cố định
        g.MapPost("/{id:guid}/cancel", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var rec = await db.RecurringBookings.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == uid);
            if (rec is null) return Results.NotFound();

            var now = DateTime.Now;
            var future = (await db.Bookings.Include(b => b.Slots)
                    .Where(b => b.RecurringId == id && b.Status == "Đã đặt" && b.CheckedInAt == null)
                    .ToListAsync())
                .Where(b => BookingTime.EndOf(b) > now).ToList();

            foreach (var b in future)
            {
                b.Status = "Đã hủy";
                db.BookingSlots.RemoveRange(b.Slots);   // trả giờ lại cho người khác đặt
            }
            await db.SaveChangesAsync();
            return Results.Ok(future.Count);
        });
    }

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}