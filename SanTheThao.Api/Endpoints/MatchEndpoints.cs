using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class MatchEndpoints
{
    private static readonly string[] Sports = { "Bóng đá", "Cầu lông", "Pickleball", "Tennis", "Bóng rổ" };
    private static readonly string[] Levels = { "Mới chơi", "Trung bình", "Khá", "Giỏi" };

    public static void MapMatchEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/matches").RequireAuthorization();

        // Danh sách kèo đang mở (mine=true thì chỉ lấy kèo tôi đăng hoặc đã xin tham gia)
        g.MapGet("/", async (string? sport, bool? mine, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var today = DateTime.Today;
            IQueryable<MatchPost> q = db.MatchPosts.AsNoTracking();

            if (mine == true)
            {
                var joined = db.MatchJoins.Where(j => j.UserId == uid).Select(j => j.MatchId);
                q = q.Where(m => m.HostId == uid || joined.Contains(m.Id))
                     .OrderByDescending(m => m.Date).ThenByDescending(m => m.StartHour);
            }
            else
            {
                q = q.Where(m => m.Date >= today && m.Status != "Đã đóng");
                if (!string.IsNullOrWhiteSpace(sport) && sport != "Tất cả")
                    q = q.Where(m => m.Sport == sport);
                q = q.OrderBy(m => m.Date).ThenBy(m => m.StartHour);
            }

            var list = await q.Take(100).ToListAsync();
            if (mine != true)
                list = list.Where(m => m.Date.AddHours(m.EndHour) > DateTime.Now).ToList();

            return Results.Ok(await ToDtosAsync(db, list, uid));
        });

        g.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var m = await db.MatchPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (m is null) return Results.NotFound();

            var dto = (await ToDtosAsync(db, new List<MatchPost> { m }, uid))[0];
            var isHost = m.HostId == uid;

            var joins = await db.MatchJoins.AsNoTracking()
                .Where(j => j.MatchId == id).OrderBy(j => j.CreatedAt).ToListAsync();
            var userIds = joins.Select(j => j.UserId).Append(m.HostId).Distinct().ToList();
            var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            var myJoin = joins.FirstOrDefault(j => j.UserId == uid);

            // Số điện thoại chỉ lộ cho người có liên quan: chủ kèo, và người đã được nhận
            string? hostPhone = (isHost || myJoin?.Status == "Được nhận")
                ? users.GetValueOrDefault(m.HostId)?.Phone : null;

            var list = joins
                .Where(j => isHost || j.Status == "Được nhận" || j.UserId == uid)
                .Select(j => new MatchJoinDto(
                    j.Id,
                    users.GetValueOrDefault(j.UserId)?.FullName ?? "Người dùng",
                    j.Message,
                    j.Status,
                    isHost && j.Status == "Được nhận" ? users.GetValueOrDefault(j.UserId)?.Phone : null,
                    j.UserId == uid))
                .ToList();

            return Results.Ok(new MatchDetailDto(dto, hostPhone, list));
        });

        g.MapPost("/", async (CreateMatchRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;

            if (!Sports.Contains(req.Sport)) return Results.BadRequest("Môn thể thao không hợp lệ.");
            if (!Levels.Contains(req.Level)) return Results.BadRequest("Trình độ không hợp lệ.");
            if (req.PlayersNeeded < 1 || req.PlayersNeeded > 20) return Results.BadRequest("Số người cần thêm từ 1 đến 20.");
            if (req.StartHour < 5 || req.EndHour > 24 || req.StartHour >= req.EndHour)
                return Results.BadRequest("Giờ chơi chưa đúng.");
            if (req.CostPerPerson < 0 || req.CostPerPerson > 10_000_000)
                return Results.BadRequest("Chi phí mỗi người chưa đúng.");

            var date = req.Date.Date;
            if (date.AddHours(req.EndHour) <= DateTime.Now)
                return Results.BadRequest("Giờ chơi đã qua.");

            if (req.VenueId != null &&
                !await db.Venues.AnyAsync(v => v.Id == req.VenueId && v.IsActive))
                return Results.BadRequest("Không tìm thấy sân.");

            var openCount = await db.MatchPosts.CountAsync(m => m.HostId == uid && m.Status != "Đã đóng" && m.Date >= DateTime.Today);
            if (openCount >= 10)
                return Results.BadRequest("Bạn đang có quá nhiều kèo mở. Hãy đóng bớt kèo cũ.");

            var note = (req.Note ?? "").Trim();
            db.MatchPosts.Add(new MatchPost
            {
                HostId = uid,
                Sport = req.Sport,
                VenueId = req.VenueId,
                Date = date,
                StartHour = req.StartHour,
                EndHour = req.EndHour,
                PlayersNeeded = req.PlayersNeeded,
                Level = req.Level,
                CostPerPerson = req.CostPerPerson,
                Note = note[..Math.Min(note.Length, 300)]
            });
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{id:guid}/join", async (Guid id, JoinMatchRequest req, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var m = await db.MatchPosts.FirstOrDefaultAsync(x => x.Id == id);
            if (m is null) return Results.NotFound();

            if (m.HostId == uid) return Results.BadRequest("Bạn là chủ kèo này.");
            if (m.Status == "Đã đóng") return Results.BadRequest("Kèo này đã đóng.");
            if (m.Status == "Đủ người") return Results.BadRequest("Kèo này đã đủ người.");
            if (m.Date.AddHours(m.EndHour) <= DateTime.Now) return Results.BadRequest("Kèo này đã kết thúc.");
            if (await db.MatchJoins.AnyAsync(j => j.MatchId == id && j.UserId == uid))
                return Results.BadRequest("Bạn đã xin tham gia kèo này rồi.");

            var msg = (req.Message ?? "").Trim();
            db.MatchJoins.Add(new MatchJoin
            {
                MatchId = id,
                UserId = uid,
                Message = msg[..Math.Min(msg.Length, 200)]
            });

            // Gửi thông báo cho Chủ kèo khi có người xin tham gia
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == uid);
            var userName = user?.FullName ?? "Người dùng";
            Notify.Add(db, m.HostId, "match", "Có người xin tham gia kèo", $"{userName} muốn tham gia kèo {m.Sport} của bạn.");

            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { return Results.BadRequest("Bạn đã xin tham gia kèo này rồi."); }
            return Results.Ok();
        });

        g.MapPost("/{id:guid}/leave", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var j = await db.MatchJoins.FirstOrDefaultAsync(x => x.MatchId == id && x.UserId == uid);
            if (j is null) return Results.NotFound();

            var wasAccepted = j.Status == "Được nhận";
            db.MatchJoins.Remove(j);

            var m = await db.MatchPosts.FirstOrDefaultAsync(x => x.Id == id);
            if (m != null && wasAccepted && m.Status == "Đủ người") m.Status = "Đang mở";

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{id:guid}/joins/{joinId:guid}/approve", async (Guid id, Guid joinId, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var m = await db.MatchPosts.FirstOrDefaultAsync(x => x.Id == id && x.HostId == uid);
            if (m is null) return Results.NotFound();

            var j = await db.MatchJoins.FirstOrDefaultAsync(x => x.Id == joinId && x.MatchId == id);
            if (j is null) return Results.NotFound();
            if (j.Status != "Chờ duyệt") return Results.BadRequest("Yêu cầu này đã được xử lý rồi.");

            var accepted = await db.MatchJoins.CountAsync(x => x.MatchId == id && x.Status == "Được nhận");
            if (accepted >= m.PlayersNeeded) return Results.BadRequest("Kèo đã đủ người.");

            j.Status = "Được nhận";
            if (accepted + 1 >= m.PlayersNeeded) m.Status = "Đủ người";

            // Gửi thông báo cho người chơi khi được duyệt tham gia
            Notify.Add(db, j.UserId, "match", "Đã duyệt tham gia kèo", $"Chủ kèo đã chấp nhận yêu cầu tham gia kèo {m.Sport} của bạn.");

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{id:guid}/joins/{joinId:guid}/reject", async (Guid id, Guid joinId, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var m = await db.MatchPosts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.HostId == uid);
            if (m is null) return Results.NotFound();

            var j = await db.MatchJoins.FirstOrDefaultAsync(x => x.Id == joinId && x.MatchId == id);
            if (j is null) return Results.NotFound();
            if (j.Status != "Chờ duyệt") return Results.BadRequest("Yêu cầu này đã được xử lý rồi.");

            j.Status = "Từ chối";

            // Gửi thông báo cho người chơi khi bị từ chối
            Notify.Add(db, j.UserId, "match", "Từ chối tham gia kèo", $"Chủ kèo đã từ chối yêu cầu tham gia kèo {m.Sport} của bạn.");

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/{id:guid}/close", async (Guid id, ClaimsPrincipal me, AppDbContext db) =>
        {
            var uid = UserIdOf(me)!.Value;
            var m = await db.MatchPosts.FirstOrDefaultAsync(x => x.Id == id && x.HostId == uid);
            if (m is null) return Results.NotFound();

            m.Status = "Đã đóng";
            await db.SaveChangesAsync();
            return Results.Ok();
        });
    }

    private static async Task<List<MatchDto>> ToDtosAsync(AppDbContext db, List<MatchPost> list, Guid uid)
    {
        var ids = list.Select(m => m.Id).ToList();
        var hostIds = list.Select(m => m.HostId).Distinct().ToList();
        var venueIds = list.Where(m => m.VenueId != null).Select(m => m.VenueId!.Value).Distinct().ToList();

        var hosts = await db.Users.AsNoTracking().Where(u => hostIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);
        var venues = await db.Venues.AsNoTracking().Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.Name);
        var joins = await db.MatchJoins.AsNoTracking().Where(j => ids.Contains(j.MatchId))
            .Select(j => new { j.MatchId, j.UserId, j.Status }).ToListAsync();

        return list.Select(m => new MatchDto(
            m.Id,
            hosts.GetValueOrDefault(m.HostId, "Người dùng"),
            m.Sport,
            m.VenueId,
            m.VenueId != null ? venues.GetValueOrDefault(m.VenueId.Value, "") : "",
            m.Date,
            m.StartHour,
            m.EndHour,
            m.PlayersNeeded,
            joins.Count(j => j.MatchId == m.Id && j.Status == "Được nhận"),
            m.Level,
            m.CostPerPerson,
            m.Note,
            m.Status,
            m.HostId == uid,
            joins.FirstOrDefault(j => j.MatchId == m.Id && j.UserId == uid)?.Status)).ToList();
    }

    private static Guid? UserIdOf(ClaimsPrincipal p) =>
        Guid.TryParse(p.FindFirst("sub")?.Value, out var id) ? id : null;
}