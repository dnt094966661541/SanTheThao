using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class ChatEndpoints
{
    private const string Forbidden = "Chỉ chủ kèo và người đã được nhận mới vào được phòng trò chuyện.";

    public static void MapChatEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/matches").RequireAuthorization();

        // Lấy tin nhắn. after = 0 thì lấy 100 tin mới nhất, after > 0 thì chỉ lấy các tin có mã lớn hơn.
        g.MapGet("/{id:guid}/chat", async (Guid id, int? after, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var (m, allowed) = await AccessAsync(db, id, uid);
            if (m is null) return Results.NotFound();
            if (!allowed) return Results.Json(Forbidden, statusCode: 403);

            var afterId = after ?? 0;
            List<MatchMessage> list;
            if (afterId <= 0)
            {
                list = await db.MatchMessages.AsNoTracking()
                    .Where(x => x.MatchId == id)
                    .OrderByDescending(x => x.Id).Take(100).ToListAsync();
                list.Reverse();
            }
            else
            {
                list = await db.MatchMessages.AsNoTracking()
                    .Where(x => x.MatchId == id && x.Id > afterId)
                    .OrderBy(x => x.Id).Take(100).ToListAsync();
            }

            var userIds = list.Select(x => x.UserId).Distinct().ToList();
            var names = await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            // Đang xem phòng chat thì coi như đã đọc thông báo tin nhắn của kèo này
            var route = $"MatchChat?id={id}";
            await db.Notifications
                .Where(n => n.UserId == uid && n.Type == "chat" && n.Route == route && n.ReadAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.Now));

            var count = 1 + await db.MatchJoins.CountAsync(j => j.MatchId == id && j.Status == "Được nhận");

            return Results.Ok(new ChatResponse(
                list.Select(x => ToDto(x, names, uid)).ToList(),
                CanSend(m),
                count,
                $"{m.Sport} · {m.Date:dd/MM} · {m.StartHour:00}h-{m.EndHour:00}h"));
        });

        // Gửi một tin nhắn
        g.MapPost("/{id:guid}/chat", async (Guid id, SendChatRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var (m, allowed) = await AccessAsync(db, id, uid);
            if (m is null) return Results.NotFound();
            if (!allowed) return Results.Json(Forbidden, statusCode: 403);
            if (!CanSend(m)) return Results.BadRequest("Kèo đã đóng hoặc đã kết thúc nên không nhắn thêm được.");

            var text = (req.Text ?? "").Trim();
            if (text == "") return Results.BadRequest("Bạn chưa nhập nội dung.");
            if (text.Length > 1000) return Results.BadRequest("Tin nhắn tối đa 1000 ký tự.");

            var since = DateTime.Now.AddMinutes(-1);
            if (await db.MatchMessages.CountAsync(x => x.UserId == uid && x.CreatedAt > since) >= 20)
                return Results.BadRequest("Bạn gửi quá nhanh, hãy chờ một chút.");

            var msg = new MatchMessage { MatchId = id, UserId = uid, Text = text };
            db.MatchMessages.Add(msg);

            var senderName = await db.Users.AsNoTracking()
                .Where(u => u.Id == uid).Select(u => u.FullName).FirstOrDefaultAsync() ?? "Người dùng";

            // Báo cho những người còn lại trong nhóm. Mỗi người chỉ giữ một thông báo chưa đọc cho mỗi kèo.
            var recipients = await db.MatchJoins
                .Where(j => j.MatchId == id && j.Status == "Được nhận")
                .Select(j => j.UserId).ToListAsync();
            recipients.Add(m.HostId);
            recipients = recipients.Where(r => r != uid).Distinct().ToList();

            var route = $"MatchChat?id={id}";
            var olds = await db.Notifications
                .Where(n => recipients.Contains(n.UserId) && n.Type == "chat" && n.Route == route && n.ReadAt == null)
                .ToListAsync();
            db.Notifications.RemoveRange(olds);

            var preview = text.Length > 60 ? text[..60] + "…" : text;
            foreach (var r in recipients)
                Notify.Add(db, r, "chat", $"Tin nhắn mới - kèo {m.Sport} {m.Date:dd/MM}",
                    $"{senderName}: {preview}", route);

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(msg, new Dictionary<Guid, string> { [uid] = senderName }, uid));
        });
    }

    // Chủ kèo và người đã được nhận mới vào được phòng chat
    private static async Task<(MatchPost? Match, bool Allowed)> AccessAsync(AppDbContext db, Guid matchId, Guid uid)
    {
        var m = await db.MatchPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == matchId);
        if (m is null) return (null, false);
        if (m.HostId == uid) return (m, true);

        var ok = await db.MatchJoins.AnyAsync(j => j.MatchId == matchId && j.UserId == uid && j.Status == "Được nhận");
        return (m, ok);
    }

    // Kèo chưa đóng và chưa quá 24 giờ sau khi kết thúc thì còn nhắn được
    private static bool CanSend(MatchPost m) =>
        m.Status != "Đã đóng" && DateTime.Now < m.Date.AddHours(m.EndHour + 24);

    private static ChatMessageDto ToDto(MatchMessage x, Dictionary<Guid, string> names, Guid uid) =>
        new(x.Id, names.GetValueOrDefault(x.UserId, "Người dùng"), x.Text, x.CreatedAt, x.UserId == uid);

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}