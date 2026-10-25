using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;

namespace SanTheThao.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // Chỉ Admin mới gọi được
        var g = app.MapGroup("/api/admin")
                   .RequireAuthorization(p => p.RequireRole("Admin"));

        g.MapGet("/stats", async (AppDbContext db) =>
        {
            var customers = await db.Users.CountAsync(u => u.Role == "Customer");
            var owners = await db.Users.CountAsync(u => u.Role == "Owner");
            var venues = await db.Venues.CountAsync();
            var bookings = await db.Bookings.CountAsync(b => b.Status == "Đã đặt");
            var revenue = await db.Bookings
                .Where(b => b.Status == "Đã đặt")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            return new AdminStatsResponse(customers, owners, venues, bookings, revenue);
        });

        g.MapGet("/users", async (AppDbContext db) =>
            await db.Users.AsNoTracking()
                .OrderBy(u => u.Role)
                .ThenByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Role, u.IsLocked, u.CreatedAt))
                .ToListAsync());

        // Bấm lần đầu thì khóa, bấm lần nữa thì mở khóa
        g.MapPost("/users/{id:guid}/lock", async (Guid id, AppDbContext db) =>
        {
            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return Results.NotFound();
            if (u.Role == "Admin") return Results.BadRequest("Không khóa được tài khoản quản trị viên.");

            u.IsLocked = !u.IsLocked;
            await db.SaveChangesAsync();
            return Results.Ok(u.IsLocked);
        });

        g.MapPost("/users/{id:guid}/role", async (Guid id, SetRoleRequest req, AppDbContext db) =>
        {
            if (req.Role is not ("Customer" or "Owner"))
                return Results.BadRequest("Vai trò không hợp lệ.");

            var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
            if (u is null) return Results.NotFound();
            if (u.Role == "Admin") return Results.BadRequest("Không đổi được vai trò của quản trị viên.");

            u.Role = req.Role;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // ----- Duyệt hồ sơ chủ sân -----
        g.MapGet("/applications", async (AppDbContext db) =>
        {
            var query =
                from a in db.OwnerApplications.AsNoTracking()
                join u in db.Users.AsNoTracking() on a.UserId equals u.Id
                orderby a.Status == "Chờ duyệt" descending, a.CreatedAt descending
                select new OwnerApplicationDto(
                    a.Id, a.BusinessName, a.Phone, a.Address, a.Note,
                    a.Status, a.AdminNote, a.CreatedAt, u.FullName, u.Email);
            return await query.ToListAsync();
        });

        g.MapPost("/applications/{id:int}/approve", async (int id, AppDbContext db) =>
        {
            var a = await db.OwnerApplications.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return Results.NotFound();
            if (a.Status != "Chờ duyệt") return Results.BadRequest("Hồ sơ này đã được xử lý rồi.");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == a.UserId);
            if (user is null) return Results.NotFound();

            if (user.Role == "Customer") user.Role = "Owner";

            a.Status = "Đã duyệt";
            a.AdminNote = null;
            a.ReviewedAt = DateTime.Now;

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        g.MapPost("/applications/{id:int}/reject", async (int id, RejectApplicationRequest req, AppDbContext db) =>
        {
            var a = await db.OwnerApplications.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return Results.NotFound();
            if (a.Status != "Chờ duyệt") return Results.BadRequest("Hồ sơ này đã được xử lý rồi.");

            var note = (req.Note ?? "").Trim();
            if (note == "") return Results.BadRequest("Bạn hãy nhập lý do từ chối.");

            a.Status = "Từ chối";
            a.AdminNote = note;
            a.ReviewedAt = DateTime.Now;

            await db.SaveChangesAsync();
            return Results.Ok();
        });
    }
}