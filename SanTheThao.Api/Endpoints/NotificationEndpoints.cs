using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/notifications").RequireAuthorization();

        // App hỏi định kỳ: số thông báo chưa đọc và vài thông báo mới nhất
        g.MapGet("/unread", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var query = db.Notifications.AsNoTracking().Where(n => n.UserId == uid && n.ReadAt == null);

            var count = await query.CountAsync();
            var latest = await query.OrderByDescending(n => n.Id).Take(5).ToListAsync();

            return new NotificationSummaryDto(count, latest.Select(ToDto).ToList());
        });

        // Hộp thông báo: 50 thông báo gần nhất
        g.MapGet("/", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var list = await db.Notifications.AsNoTracking()
                .Where(n => n.UserId == uid)
                .OrderByDescending(n => n.Id)
                .Take(50)
                .ToListAsync();

            return list.Select(ToDto).ToList();
        });

        g.MapPost("/{id:int}/read", async (int id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            var n = await db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (n is null) return Results.NotFound();

            n.ReadAt ??= DateTime.Now;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/read-all", async (ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me);
            await db.Notifications
                .Where(n => n.UserId == uid && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.Now));
            return Results.Ok();
        });
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Body, n.Route, n.CreatedAt, n.ReadAt != null);

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}