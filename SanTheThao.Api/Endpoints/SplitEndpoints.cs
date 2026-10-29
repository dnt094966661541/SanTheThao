using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class SplitEndpoints
{
    public static void MapSplitEndpoints(this WebApplication app)
    {
        // Xem cách chia tiền của một đơn (chưa chia thì trả về null)
        app.MapGet("/api/bookings/{id:guid}/split", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var s = await db.CostSplits.AsNoTracking().Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.BookingId == id && x.UserId == uid);
            return Results.Ok(s is null ? null : ToDto(s));
        }).RequireAuthorization();

        app.MapPost("/api/bookings/{id:guid}/split", async (Guid id, CreateSplitRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var b = await db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (b is null) return Results.NotFound();
            if (b.Status != "Đã đặt") return Results.BadRequest("Đơn đã hủy nên không chia tiền được.");
            if (req.People < 2 || req.People > 20) return Results.BadRequest("Số người từ 2 đến 20.");

            var existing = await db.CostSplits.Include(x => x.Members).FirstOrDefaultAsync(x => x.BookingId == id);
            if (existing != null)
            {
                if (existing.Members.Any(m => !m.IsOwner && m.Paid))
                    return Results.BadRequest("Đã có người trả tiền nên không đổi cách chia được.");
                db.CostSplits.Remove(existing);
                await db.SaveChangesAsync();
            }

            // Mỗi bạn trả phần làm tròn xuống nghìn đồng, người đặt sân gánh phần lẻ
            var share = Math.Floor(b.TotalPrice / req.People / 1000m) * 1000m;
            var names = (req.Names ?? new()).Select(n => (n ?? "").Trim()).Where(n => n != "").ToList();

            var split = new CostSplit
            {
                BookingId = b.Id,
                UserId = b.UserId,
                Total = b.TotalPrice,
                Note = (req.Note ?? "").Trim()[..Math.Min((req.Note ?? "").Trim().Length, 200)]
            };

            split.Members.Add(new SplitMember
            {
                Sort = 0,
                Name = "Bạn (người đặt)",
                Amount = b.TotalPrice - share * (req.People - 1),
                IsOwner = true,
                Paid = true,
                PaidAt = DateTime.Now
            });

            for (var i = 1; i < req.People; i++)
            {
                var name = i <= names.Count ? names[i - 1] : $"Bạn {i}";
                split.Members.Add(new SplitMember
                {
                    Sort = i,
                    Name = name[..Math.Min(name.Length, 30)],
                    Amount = share
                });
            }

            db.CostSplits.Add(split);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { return Results.Conflict("Không lưu được, bạn thử lại."); }

            return Results.Ok(ToDto(split));
        }).RequireAuthorization();

        app.MapDelete("/api/bookings/{id:guid}/split", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var s = await db.CostSplits.Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.BookingId == id && x.UserId == uid);
            if (s is null) return Results.NotFound();
            if (s.Members.Any(m => !m.IsOwner && m.Paid))
                return Results.BadRequest("Đã có người trả tiền nên không xóa được.");

            db.CostSplits.Remove(s);
            await db.SaveChangesAsync();
            return Results.Ok();
        }).RequireAuthorization();

        // Bấm lần đầu thì đánh dấu đã trả, bấm lần nữa thì bỏ đánh dấu
        app.MapPost("/api/splits/members/{memberId:guid}/paid", async (Guid memberId, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var m = await db.SplitMembers.FirstOrDefaultAsync(x => x.Id == memberId);
            if (m is null) return Results.NotFound();

            var s = await db.CostSplits.Include(x => x.Members)
                .FirstOrDefaultAsync(x => x.Id == m.CostSplitId && x.UserId == uid);
            if (s is null) return Results.NotFound();
            if (m.IsOwner) return Results.BadRequest("Phần của người đặt sân luôn được tính là đã trả.");

            var member = s.Members.First(x => x.Id == memberId);
            member.Paid = !member.Paid;
            member.PaidAt = member.Paid ? DateTime.Now : null;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(s));
        }).RequireAuthorization();
    }

    private static SplitDto ToDto(CostSplit s) => new(
        s.Id, s.BookingId, s.Total, s.Note,
        s.Members.Where(m => m.Paid).Sum(m => m.Amount),
        s.Members.OrderBy(m => m.Sort)
            .Select(m => new SplitMemberDto(m.Id, m.Name, m.Amount, m.Paid, m.IsOwner)).ToList());

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}