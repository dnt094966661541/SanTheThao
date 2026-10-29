using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static SanTheThao.Api.Models.Promotion;

namespace SanTheThao.Api.Endpoints;

public static class OwnerEndpoints
{
    private const int MaxImagesPerVenue = 10;

    public static void MapOwnerEndpoints(this WebApplication app)
    {
        // Chỉ Chủ sân và Admin mới gọi được các đường dẫn bên dưới
        var g = app.MapGroup("/api/owner")
                   .RequireAuthorization(p => p.RequireRole("Owner", "Admin"));

        // ----- Sân của tôi -----
        g.MapGet("/venues", async (ClaimsPrincipal me, AppDbContext db) =>
            await OwnVenues(me, db).AsNoTracking().OrderBy(v => v.Id).ToListAsync());

        g.MapPost("/venues", async (VenueRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var error = Validate(req);
            if (error != null) return Results.BadRequest(error);

            var v = new Venue { OwnerId = UserIdOf(me) };
            Apply(v, req);
            db.Venues.Add(v);
            await db.SaveChangesAsync();
            return Results.Ok(v);
        });

        g.MapPut("/venues/{id:int}", async (int id, VenueRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var error = Validate(req);
            if (error != null) return Results.BadRequest(error);

            var v = await OwnVenues(me, db).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return Results.NotFound();

            Apply(v, req);
            await db.SaveChangesAsync();
            return Results.Ok(v);
        });

        g.MapPost("/venues/{id:int}/toggle", async (int id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var v = await OwnVenues(me, db).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return Results.NotFound();

            v.IsActive = !v.IsActive;
            await db.SaveChangesAsync();
            return Results.Ok(v);
        });

        // ----- Quản lý ảnh sân -----
        g.MapPost("/venues/{id:int}/images", async (int id, HttpRequest request, ClaimsPrincipal me,
                                                    AppDbContext db, IWebHostEnvironment env) =>
        {
            var v = await OwnVenues(me, db).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return Results.NotFound();
            if (await db.VenueImages.CountAsync(i => i.VenueId == id) >= MaxImagesPerVenue)
                return Results.BadRequest($"Mỗi sân chỉ có tối đa {MaxImagesPerVenue} ảnh.");
            const int maxBytes = 10 * 1024 * 1024;
            if (request.ContentLength > maxBytes)
                return Results.BadRequest("Ảnh quá lớn, tối đa 10 MB.");
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await request.Body.ReadAsync(buffer)) > 0)
            {
                ms.Write(buffer, 0, read);
                if (ms.Length > maxBytes)
                    return Results.BadRequest("Ảnh quá lớn, tối đa 10 MB.");
            }
            var data = ms.ToArray();
            var ext = ImageExtension(data);
            if (ext is null)
                return Results.BadRequest("Chỉ nhận ảnh JPG, PNG hoặc WebP.");
            var dir = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(dir);
            var fileName = $"venue-{v.Id}-{Guid.NewGuid():N}{ext}";
            await File.WriteAllBytesAsync(Path.Combine(dir, fileName), data);
            var img = new VenueImage { VenueId = id, Url = "/uploads/" + fileName };
            db.VenueImages.Add(img);
            // Ảnh đầu tiên của sân tự thành ảnh chính
            if (string.IsNullOrEmpty(v.ImageUrl)) v.ImageUrl = img.Url;
            await db.SaveChangesAsync();
            return Results.Ok(new VenueImageDto(img.Id, img.Url, v.ImageUrl == img.Url));
        });

        g.MapDelete("/venues/{id:int}/images/{imageId:int}", async (int id, int imageId, ClaimsPrincipal me,
                                                                    AppDbContext db, IWebHostEnvironment env) =>
        {
            var v = await OwnVenues(me, db).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return Results.NotFound();
            var img = await db.VenueImages.FirstOrDefaultAsync(i => i.Id == imageId && i.VenueId == id);
            if (img is null) return Results.NotFound();
            var path = Path.Combine(env.ContentRootPath, "uploads", Path.GetFileName(img.Url));
            if (File.Exists(path)) File.Delete(path);
            db.VenueImages.Remove(img);
            // Xóa đúng ảnh chính thì lấy ảnh còn lại đầu tiên làm ảnh chính
            if (v.ImageUrl == img.Url)
            {
                v.ImageUrl = await db.VenueImages
                    .Where(i => i.VenueId == id && i.Id != imageId)
                    .OrderBy(i => i.Id)
                    .Select(i => i.Url)
                    .FirstOrDefaultAsync();
            }
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/venues/{id:int}/images/{imageId:int}/main", async (int id, int imageId, ClaimsPrincipal me, AppDbContext db) =>
        {
            var v = await OwnVenues(me, db).FirstOrDefaultAsync(x => x.Id == id);
            if (v is null) return Results.NotFound();
            var img = await db.VenueImages.FirstOrDefaultAsync(i => i.Id == imageId && i.VenueId == id);
            if (img is null) return Results.NotFound();
            v.ImageUrl = img.Url;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // ----- Đơn đặt của các sân tôi sở hữu -----
        g.MapGet("/bookings", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var ids = await OwnVenues(me, db).Select(v => v.Id).ToListAsync();
            var list = await db.Bookings
                .Include(b => b.Venue)
                .Include(b => b.User)
                .Where(b => ids.Contains(b.VenueId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return list.Select(BookingDto.From);
        });

        // ----- Doanh thu theo tháng (month = yyyy-MM) -----
        g.MapGet("/revenue", async (string? month, ClaimsPrincipal me, AppDbContext db) =>
        {
            var today = DateTime.Today;
            var first = DateTime.TryParseExact(month + "-01", "yyyy-MM-dd",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                        ? d
                        : new DateTime(today.Year, today.Month, 1);
            var next = first.AddMonths(1);

            var ids = await OwnVenues(me, db).Select(v => v.Id).ToListAsync();

            var rows = await db.Bookings
                .Where(b => ids.Contains(b.VenueId)
                            && b.Status == "Đã đặt"
                            && b.Date >= first && b.Date < next)
                .Select(b => new { b.Date, b.TotalPrice })
                .ToListAsync();

            var perDay = rows
                .GroupBy(r => r.Date.Date)
                .OrderBy(x => x.Key)
                .Select(x => new RevenueDay(x.Key, x.Sum(r => r.TotalPrice), x.Count()))
                .ToList();

            return Results.Ok(new RevenueResponse(
                first.ToString("MM/yyyy"), rows.Sum(r => r.TotalPrice), rows.Count, perDay));
        });

        // ----- Check-in khách bằng mã đơn -----
        g.MapPost("/checkin", async (CheckInRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            // Nhận cả mã 8 ký tự lẫn nội dung đầy đủ của mã QR
            var input = new string((req.Code ?? "").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (input.StartsWith("SANTHETHAO")) input = input["SANTHETHAO".Length..];
            if (input.Length < 8) return Results.BadRequest("Mã đơn cần có ít nhất 8 ký tự.");

            var ids = await OwnVenues(me, db).Select(v => v.Id).ToListAsync();
            var today = DateTime.Today;

            var candidates = await db.Bookings
                .Include(b => b.Venue)
                .Include(b => b.User)
                .Where(b => ids.Contains(b.VenueId) && b.Status == "Đã đặt" && b.Date == today)
                .ToListAsync();

            var booking = candidates.FirstOrDefault(b =>
                b.Id.ToString("N").ToUpperInvariant().StartsWith(input));
            if (booking is null)
                return Results.BadRequest("Không tìm thấy đơn nào của sân bạn với mã này trong hôm nay.");

            var hours = booking.HoursCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            var start = booking.Date.AddHours(hours.Min());
            var end = booking.Date.AddHours(hours.Max() + 1);
            var now = DateTime.Now;

            if (booking.CheckedInAt != null)
                return Results.BadRequest($"Đơn này đã check-in lúc {booking.CheckedInAt:HH:mm}.");
            if (now < start.AddMinutes(-30))
                return Results.BadRequest($"Chưa đến giờ. Khách check-in sớm nhất 30 phút trước {start:HH:mm}.");
            if (now > end)
                return Results.BadRequest("Đơn này đã qua giờ chơi.");

            booking.CheckedInAt = now;
            await db.SaveChangesAsync();
            return Results.Ok(BookingDto.From(booking));
        });

        // ----- Mã giảm giá -----
        g.MapGet("/promotions", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var query = db.Promotions.AsNoTracking().AsQueryable();
            if (!me.IsInRole("Admin"))
            {
                var uid = UserIdOf(me);
                query = query.Where(p => p.OwnerId == uid);
            }
            return await query.OrderByDescending(p => p.Id).ToListAsync();
        });

        g.MapPost("/promotions", async (PromotionRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var code = (req.Code ?? "").Trim().ToUpperInvariant();
            if (!Regex.IsMatch(code, "^[A-Z0-9]{3,20}$"))
                return Results.BadRequest("Mã cần từ 3 đến 20 ký tự, chỉ gồm chữ không dấu và số.");
            if (req.PercentOff < 1 || req.PercentOff > 100)
                return Results.BadRequest("Phần trăm giảm phải từ 1 đến 100.");
            if (req.MaxDiscount < 0 || req.MinTotal < 0 || req.UsageLimit < 0)
                return Results.BadRequest("Số tiền và số lượt không được âm.");
            if (req.ValidTo.Date < req.ValidFrom.Date)
                return Results.BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");
            if (await db.Promotions.AnyAsync(p => p.Code == code))
                return Results.Conflict("Mã này đã tồn tại, bạn hãy chọn mã khác.");
            if (req.VenueId != null && !await OwnVenues(me, db).AnyAsync(v => v.Id == req.VenueId))
                return Results.BadRequest("Sân này không thuộc về bạn.");

            var promo = new Promotion
            {
                Code = code,
                Description = string.IsNullOrWhiteSpace(req.Description) ? BuildDescription(req) : req.Description.Trim(),
                PercentOff = req.PercentOff,
                MaxDiscount = req.MaxDiscount,
                MinTotal = req.MinTotal,
                ValidFrom = req.ValidFrom.Date,
                ValidTo = req.ValidTo.Date,
                UsageLimit = req.UsageLimit,
                VenueId = req.VenueId,
                OwnerId = me.IsInRole("Admin") ? null : UserIdOf(me)
            };

            db.Promotions.Add(promo);
            await db.SaveChangesAsync();
            return Results.Ok(promo);
        });

        g.MapPost("/promotions/{id:int}/toggle", async (int id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var p = await db.Promotions.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            if (!me.IsInRole("Admin") && p.OwnerId != UserIdOf(me))
                return Results.StatusCode(403);

            p.IsActive = !p.IsActive;
            await db.SaveChangesAsync();
            return Results.Ok(p);
        });
    }

    // Admin thấy tất cả sân, Chủ sân chỉ thấy sân của mình
    private static IQueryable<Venue> OwnVenues(ClaimsPrincipal me, AppDbContext db)
    {
        if (me.IsInRole("Admin")) return db.Venues;
        var uid = UserIdOf(me);
        return db.Venues.Where(v => v.OwnerId == uid);
    }

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;

    private static string? Validate(VenueRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Name)) return "Bạn chưa nhập tên sân.";
        if (r.PricePerHour <= 0) return "Giá thường phải lớn hơn 0.";
        if (r.PeakPricePerHour < 0 || r.WeekendPricePerHour < 0) return "Giá không được âm.";
        if (r.PeakStartHour < 6 || r.PeakEndHour > 22 || r.PeakStartHour >= r.PeakEndHour)
            return "Giờ cao điểm phải nằm trong khoảng 6 đến 22, và giờ bắt đầu nhỏ hơn giờ kết thúc.";
        return null;
    }

    private static void Apply(Venue v, VenueRequest r)
    {
        v.Name = r.Name.Trim();
        v.Sport = r.Sport;
        v.Icon = r.Icon;
        v.Address = (r.Address ?? "").Trim();
        v.District = (r.District ?? "").Trim();
        v.PricePerHour = r.PricePerHour;
        v.PeakPricePerHour = r.PeakPricePerHour;
        v.PeakStartHour = r.PeakStartHour;
        v.PeakEndHour = r.PeakEndHour;
        v.WeekendPricePerHour = r.WeekendPricePerHour;
    }

    // Nhận ra loại ảnh từ nội dung file, không dựa vào tên file hay loại do app gửi
    private static string? ImageExtension(byte[] d)
    {
        if (d.Length > 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return ".jpg";
        if (d.Length > 8 && d[0] == 0x89 && d[1] == 0x50 && d[2] == 0x4E && d[3] == 0x47) return ".png";
        if (d.Length > 12 && d[0] == 'R' && d[1] == 'I' && d[2] == 'F' && d[3] == 'F'
                            && d[8] == 'W' && d[9] == 'E' && d[10] == 'B' && d[11] == 'P') return ".webp";
        return null;
    }

    private static string BuildDescription(PromotionRequest r)
    {
        var vn = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
        var text = $"Giảm {r.PercentOff}%";
        if (r.MaxDiscount > 0) text += $" (tối đa {r.MaxDiscount.ToString("N0", vn)} đ)";
        if (r.MinTotal > 0) text += $" cho đơn từ {r.MinTotal.ToString("N0", vn)} đ";
        return text;
    }
}