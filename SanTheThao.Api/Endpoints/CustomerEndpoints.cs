using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;
using System.Security.Claims;
using static SanTheThao.Api.Models.Promotion;

namespace SanTheThao.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        // Đánh giá của một sân (ai cũng xem được)
        app.MapGet("/api/venues/{id:int}/reviews", async (int id, AppDbContext db) =>
        {
            var query =
                from r in db.Reviews.AsNoTracking()
                join u in db.Users.AsNoTracking() on r.UserId equals u.Id
                where r.VenueId == id
                orderby r.CreatedAt descending
                select new ReviewDto(u.FullName, r.Rating, r.Comment, r.CreatedAt);

            return await query.Take(20).ToListAsync();
        });

        // Gửi đánh giá cho một đơn
        app.MapPost("/api/bookings/{id:guid}/review", async (Guid id, ReviewRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var b = await db.Bookings.Include(x => x.Venue).FirstOrDefaultAsync(x => x.Id == id);
            if (b is null || b.UserId != uid) return Results.NotFound();

            if (b.Status != "Đã đặt") return Results.BadRequest("Đơn đã hủy nên không đánh giá được.");
            if (b.Reviewed) return Results.BadRequest("Bạn đã đánh giá đơn này rồi.");
            if (req.Rating < 1 || req.Rating > 5) return Results.BadRequest("Số sao phải từ 1 đến 5.");

            var lastHour = b.HoursCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).Max();
            if (b.Date.AddHours(lastHour + 1) > DateTime.Now)
                return Results.BadRequest("Bạn chỉ đánh giá được sau khi chơi xong.");

            db.Reviews.Add(new Review
            {
                BookingId = b.Id,
                VenueId = b.VenueId,
                UserId = b.UserId,
                Rating = req.Rating,
                Comment = (req.Comment ?? "").Trim()
            });
            b.Reviewed = true;

            // Cập nhật điểm trung bình của sân
            var v = b.Venue!;
            v.Rating = Math.Round((v.Rating * v.ReviewCount + req.Rating) / (v.ReviewCount + 1), 1);
            v.ReviewCount++;

            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization();

        // Kiểm tra mã giảm giá (chưa tính tiền, chỉ xem mã dùng được không)
        app.MapGet("/api/promotions/{code}", async (string code, int venueId, AppDbContext db) =>
        {
            var venue = await db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == venueId);
            if (venue is null) return Results.NotFound("Không tìm thấy sân.");

            var c = code.Trim().ToUpperInvariant();
            var p = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == c);

            var error = PromoRules.Validate(p, venue, DateTime.Now);
            if (error != null) return Results.BadRequest(error);

            return Results.Ok(new PromoInfo(p!.Code, p.Description, p.PercentOff, p.MaxDiscount, p.MinTotal));
        }).RequireAuthorization();

        // ----- Sân yêu thích -----
        app.MapGet("/api/favorites", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            return await db.Favorites.AsNoTracking()
                .Where(f => f.UserId == uid)
                .Select(f => f.VenueId)
                .ToListAsync();
        }).RequireAuthorization();

        // Bấm lần đầu thì thêm vào yêu thích, bấm lần nữa thì bỏ. Trả về true nếu đang yêu thích.
        app.MapPost("/api/favorites/{venueId:int}/toggle", async (int venueId, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            if (uid is null) return Results.Unauthorized();

            var fav = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == uid && f.VenueId == venueId);
            if (fav != null)
            {
                db.Favorites.Remove(fav);
                await db.SaveChangesAsync();
                return Results.Ok(false);
            }

            if (!await db.Venues.AnyAsync(v => v.Id == venueId))
                return Results.NotFound();

            db.Favorites.Add(new Favorite { UserId = uid.Value, VenueId = venueId });
            await db.SaveChangesAsync();
            return Results.Ok(true);
        }).RequireAuthorization();

        // ----- Đăng ký làm chủ sân -----
        app.MapPost("/api/owner-applications", async (OwnerApplicationRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            if (user is null) return Results.Unauthorized();
            if (user.Role != "Customer")
                return Results.BadRequest("Tài khoản của bạn đã có quyền chủ sân hoặc quản trị.");

            var name = (req.BusinessName ?? "").Trim();
            var phone = new string((req.Phone ?? "").Where(char.IsDigit).ToArray());
            var address = (req.Address ?? "").Trim();

            if (name == "") return Results.BadRequest("Bạn chưa nhập tên cơ sở kinh doanh.");
            if (phone.Length < 9 || phone.Length > 11) return Results.BadRequest("Số điện thoại chưa đúng.");
            if (address == "") return Results.BadRequest("Bạn chưa nhập địa chỉ.");

            if (await db.OwnerApplications.AnyAsync(a => a.UserId == user.Id && a.Status == "Chờ duyệt"))
                return Results.BadRequest("Bạn đã có một hồ sơ đang chờ duyệt.");

            db.OwnerApplications.Add(new OwnerApplication
            {
                UserId = user.Id,
                BusinessName = name,
                Phone = phone,
                Address = address,
                Note = (req.Note ?? "").Trim()
            });

            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization();

        // Hồ sơ gần nhất của tôi (chưa gửi thì trả về trạng thái "Chưa gửi")
        app.MapGet("/api/owner-applications/mine", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var a = await db.OwnerApplications.AsNoTracking()
                .Where(x => x.UserId == uid)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (a is null)
                return new OwnerApplicationDto(0, "", "", "", "", "Chưa gửi", null, DateTime.Now, "", "");

            return new OwnerApplicationDto(a.Id, a.BusinessName, a.Phone, a.Address, a.Note,
                a.Status, a.AdminNote, a.CreatedAt, "", "");
        }).RequireAuthorization();
    }

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}