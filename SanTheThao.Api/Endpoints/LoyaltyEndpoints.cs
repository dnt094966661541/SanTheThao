using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class LoyaltyEndpoints
{
    private const decimal VndPerPoint = 10000m;

    private record Reward(string Id, string Title, int Cost, int Percent, decimal MaxDiscount);

    private static readonly List<Reward> Rewards = new()
    {
        new("r1", "Giảm 10% (tối đa 30.000 đ)", 100, 10, 30000m),
        new("r2", "Giảm 20% (tối đa 80.000 đ)", 250, 20, 80000m)
    };

    public static void MapLoyaltyEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/loyalty").RequireAuthorization();

        g.MapGet("/", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            if (uid is null) return Results.Unauthorized();

            await SettleAsync(db, uid.Value);
            return Results.Ok(await BuildAsync(db, uid.Value));
        });

        g.MapPost("/redeem", async (RedeemRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            if (uid is null) return Results.Unauthorized();

            var reward = Rewards.FirstOrDefault(r => r.Id == req.RewardId);
            if (reward is null) return Results.BadRequest("Phần quà không tồn tại.");

            await SettleAsync(db, uid.Value);
            var balance = await db.PointTransactions
                .Where(t => t.UserId == uid).SumAsync(t => (int?)t.Points) ?? 0;
            if (balance < reward.Cost)
                return Results.BadRequest("Bạn chưa đủ điểm để đổi quà này.");

            // Tạo mã dùng một lần, khó đoán
            string code;
            do { code = "TV" + RandomCode(6); }
            while (await db.Promotions.AnyAsync(p => p.Code == code));

            var promo = new Promotion
            {
                Code = code,
                Description = $"Đổi từ điểm thưởng: {reward.Title}",
                PercentOff = reward.Percent,
                MaxDiscount = reward.MaxDiscount,
                MinTotal = 0,
                ValidFrom = DateTime.Today,
                ValidTo = DateTime.Today.AddDays(30),
                UsageLimit = 1
            };
            db.Promotions.Add(promo);
            db.PointTransactions.Add(new PointTransaction
            {
                UserId = uid.Value,
                Points = -reward.Cost,
                Reason = $"Đổi quà: {reward.Title}"
            });
            await db.SaveChangesAsync();

            return Results.Ok(new RedeemResponse(code, promo.Description, balance - reward.Cost, promo.ValidTo));
        });
    }

    // Cộng điểm cho các đơn đã chơi xong mà chưa được cộng
    private static async Task SettleAsync(AppDbContext db, Guid uid)
    {
        var now = DateTime.Now;
        var ended = (await db.Bookings.Include(b => b.Venue)
                .Where(b => b.UserId == uid && b.Status == "Đã đặt").ToListAsync())
            .Where(b => BookingTime.EndOf(b) <= now).ToList();
        if (ended.Count == 0) return;

        var ids = ended.Select(b => b.Id).ToList();
        var done = await db.PointTransactions
            .Where(t => t.BookingId != null && ids.Contains(t.BookingId.Value))
            .Select(t => t.BookingId!.Value).ToListAsync();

        var todo = ended.Where(b => !done.Contains(b.Id)).OrderBy(b => b.Date).ToList();
        if (todo.Count == 0) return;

        var lifetime = await db.PointTransactions
            .Where(t => t.UserId == uid && t.Points > 0).SumAsync(t => (int?)t.Points) ?? 0;

        foreach (var b in todo)
        {
            var basePoints = (int)(b.TotalPrice / VndPerPoint);
            if (basePoints <= 0) continue;

            var points = (int)Math.Floor(basePoints * TierOf(lifetime).Mult);
            db.PointTransactions.Add(new PointTransaction
            {
                UserId = uid,
                Points = points,
                Reason = $"Hoàn thành đơn {b.Id.ToString("N")[..8].ToUpper()} ({b.Venue?.Name})",
                BookingId = b.Id,
                CreatedAt = BookingTime.EndOf(b)
            });
            lifetime += points;
        }

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); }   // hai yêu cầu cùng lúc: bỏ qua
    }

    private static async Task<LoyaltyDto> BuildAsync(AppDbContext db, Guid uid)
    {
        var tx = await db.PointTransactions.AsNoTracking()
            .Where(t => t.UserId == uid).OrderByDescending(t => t.CreatedAt).ToListAsync();

        var balance = tx.Sum(t => t.Points);
        var lifetime = tx.Where(t => t.Points > 0).Sum(t => t.Points);
        var tier = TierOf(lifetime);
        var from = lifetime >= 1000 ? 1000 : lifetime >= 300 ? 300 : 0;

        return new LoyaltyDto(balance, lifetime, tier.Name, tier.Next, from, tier.NextAt, tier.Mult,
            Rewards.Select(r => new RewardDto(r.Id, r.Title, r.Cost)).ToList(),
            tx.Take(30).Select(t => new PointTxDto(t.Points, t.Reason, t.CreatedAt)).ToList());
    }

    private static (string Name, decimal Mult, string? Next, int NextAt) TierOf(int lifetime) =>
        lifetime >= 1000 ? ("Vàng", 1.2m, null, 0) :
        lifetime >= 300 ? ("Bạc", 1.1m, "Vàng", 1000) :
        ("Đồng", 1.0m, "Bạc", 300);

    private static string RandomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}