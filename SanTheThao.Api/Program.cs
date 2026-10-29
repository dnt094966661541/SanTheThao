using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SanTheThao.Api.Data;
using SanTheThao.Api.Endpoints;
using SanTheThao.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static SanTheThao.Api.Models.Promotion;
using SanTheThao.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var id = ctx.Principal?.FindFirst("sub")?.Value;
                if (Guid.TryParse(id, out var uid) &&
                    await db.Users.AnyAsync(u => u.Id == uid && u.IsLocked))
                {
                    ctx.Fail("Tài khoản đã bị khóa.");
                }
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordHasher<AppUser>,
    PasswordHasher<AppUser>>();

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<ISmsSender, SimulatedSmsSender>();
builder.Services.AddScoped<OtpService>();

var app = builder.Build();

var uploadsDir = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

// ---------- Khởi tạo database và tài khoản mẫu ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    // Chỉ dùng EnsureDeleted() nếu thực sự muốn xóa database.
    // db.Database.EnsureDeleted();

    // Chạy các migration để cập nhật cấu trúc database.
    db.Database.Migrate();

    // Đưa ảnh cũ (một ảnh mỗi sân từ bước trước) vào bảng ảnh mới
    foreach (var v in db.Venues.Where(v => v.ImageUrl != null).ToList())
    {
        if (!db.VenueImages.Any(i => i.VenueId == v.Id && i.Url == v.ImageUrl))
            db.VenueImages.Add(new VenueImage { VenueId = v.Id, Url = v.ImageUrl! });
    }
    db.SaveChanges();

    var hasher = scope.ServiceProvider
        .GetRequiredService<IPasswordHasher<AppUser>>();

    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new AppUser
        {
            FullName = "Quản trị viên",
            Email = "admin@santhethao.vn",
            Role = "Admin"
        };

        admin.PasswordHash =
            hasher.HashPassword(admin, "Admin@123");

        db.Users.Add(admin);
    }

    var owner = db.Users.FirstOrDefault(u => u.Role == "Owner");

    if (owner is null)
    {
        owner = new AppUser
        {
            FullName = "Chủ sân mẫu",
            Email = "owner@santhethao.vn",
            Role = "Owner"
        };

        owner.PasswordHash =
            hasher.HashPassword(owner, "Owner@123");

        db.Users.Add(owner);
    }

    db.SaveChanges();

    // Gán các sân chưa có chủ cho chủ sân mẫu.
    foreach (var v in db.Venues.Where(v => v.OwnerId == null).ToList())
    {
        v.OwnerId = owner.Id;
    }

    db.SaveChanges();

    // Tài khoản tạo từ trước bước này (chưa có số điện thoại) coi như đã xác minh
    foreach (var u in db.Users.Where(u => !u.IsVerified && u.Phone == null).ToList())
        u.IsVerified = true;
    db.SaveChanges();

    // Dọn thông báo cũ trên 60 ngày
    db.Notifications.Where(n => n.CreatedAt < DateTime.Now.AddDays(-60)).ExecuteDelete();
}

app.UseAuthentication();
app.UseAuthorization();

// ---------- JWT ----------
(string Token, DateTime Expires) CreateToken(AppUser u)
{
    var expires = DateTime.UtcNow.AddDays(7);

    var claims = new[]
    {
        new Claim("sub", u.Id.ToString()),
        new Claim("name", u.FullName),
        new Claim("email", u.Email),
        new Claim("role", u.Role)
    };

    var creds = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        jwtIssuer,
        jwtAudience,
        claims,
        expires: expires,
        signingCredentials: creds);

    return (new JwtSecurityTokenHandler().WriteToken(token), expires);
}

AuthResponse MakeAuth(AppUser u)
{
    var (token, expires) = CreateToken(u);

    return new AuthResponse(
        token, u.FullName, u.Email, u.Role, expires);
}

Guid? UserIdOf(ClaimsPrincipal p) =>
    Guid.TryParse(p.FindFirst("sub")?.Value, out var id)
        ? id
        : null;

// ---------- Tài khoản ----------
app.MapPost("/api/auth/login", async (
    LoginRequest req,
    AppDbContext db,
    IPasswordHasher<AppUser> hasher) =>
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    if (user is null ||
        hasher.VerifyHashedPassword(
            user, user.PasswordHash, req.Password ?? "")
        == PasswordVerificationResult.Failed)
    {
        return Results.Json(
            "Email hoặc mật khẩu không đúng.",
            statusCode: 401);
    }

    if (user.IsLocked)
        return Results.Json("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.", statusCode: 403);

    if (!user.IsVerified)
        return Results.Json("Tài khoản chưa xác minh. Bạn hãy nhập mã xác minh để tiếp tục.", statusCode: 403);

    return Results.Ok(MakeAuth(user));
});

// ---------- Sân công khai ----------
app.MapGet("/api/venues", async (AppDbContext db) =>
    await db.Venues
        .AsNoTracking()
        .Where(v => v.IsActive)
        .ToListAsync());

app.MapGet("/api/venues/{id:int}", async (
    int id,
    AppDbContext db) =>
    await db.Venues
        .AsNoTracking()
        .FirstOrDefaultAsync(v => v.Id == id && v.IsActive) is Venue v
            ? Results.Ok(v)
            : Results.NotFound());

app.MapGet("/api/venues/{id:int}/booked-hours", async (
    int id,
    DateTime date,
    AppDbContext db) =>
    await db.BookingSlots
        .Where(s => s.VenueId == id && s.Date == date.Date)
        .Select(s => s.Hour)
        .ToListAsync());

// ---------- Đơn đặt sân ----------
app.MapGet("/api/bookings", async (
    ClaimsPrincipal me,
    AppDbContext db) =>
{
    var query = db.Bookings
        .Include(b => b.Venue)
        .AsQueryable();

    if (!me.IsInRole("Admin"))
    {
        var uid = UserIdOf(me);
        query = query.Where(b => b.UserId == uid);
    }

    var list = await query
        .OrderByDescending(b => b.CreatedAt)
        .ToListAsync();

    return list.Select(BookingDto.From);
}).RequireAuthorization();

// Tạo đơn đặt sân có hỗ trợ mã giảm giá.
app.MapPost("/api/bookings", async (
    CreateBookingRequest req,
    ClaimsPrincipal me,
    AppDbContext db) =>
{
    var uid = UserIdOf(me);

    if (uid is null)
        return Results.Unauthorized();

    var hours = req.Hours
        .Distinct()
        .OrderBy(h => h)
        .ToList();

    if (hours.Count == 0 || hours.Any(h => h < 6 || h > 21))
        return Results.BadRequest("Khung giờ không hợp lệ.");

    // Không cho đặt khung giờ đã kết thúc (giờ đang diễn ra thì vẫn đặt được)
    if (hours.Any(h => req.Date.Date.AddHours(h + 1) <= DateTime.Now))
        return Results.BadRequest("Có khung giờ đã qua, bạn hãy chọn giờ khác.");

    var venue = await db.Venues.FindAsync(req.VenueId);

    if (venue is null || !venue.IsActive)
        return Results.NotFound("Không tìm thấy sân.");

    // Server tự tính tiền, không tin số tiền từ app gửi lên.
    var subtotal = hours.Sum(h =>
        Pricing.HourPrice(venue, req.Date, h));

    Promotion? promo = null;
    decimal discount = 0;

    var code = (req.PromoCode ?? "").Trim();

    if (code != "")
    {
        promo = await db.Promotions
            .FirstOrDefaultAsync(p => p.Code == code);

        var error = PromoRules.Validate(promo, venue, DateTime.Now);

        if (error != null)
            return Results.BadRequest(error);

        discount = PromoRules.Discount(promo!, subtotal);

        if (discount == 0)
        {
            return Results.BadRequest(
                $"Đơn cần từ {promo!.MinTotal:N0} đ để dùng mã này.");
        }
    }

    var booking = new Booking
    {
        UserId = uid.Value,
        VenueId = venue.Id,
        Date = req.Date.Date,
        HoursCsv = string.Join(",", hours),
        TotalPrice = subtotal - discount,
        DiscountAmount = discount,
        PromoCode = promo?.Code,
        Slots = hours.Select(h => new BookingSlot
        {
            VenueId = venue.Id,
            Date = req.Date.Date,
            Hour = h
        }).ToList()
    };

    // Lưu lượt dùng mã và đơn đặt sân trong cùng giao dịch.
    await using var tx = await db.Database.BeginTransactionAsync();

    if (promo != null)
    {
        var updated = await db.Promotions
            .Where(p =>
                p.Id == promo.Id &&
                (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit))
            .ExecuteUpdateAsync(s =>
                s.SetProperty(
                    p => p.UsedCount,
                    p => p.UsedCount + 1));

        if (updated == 0)
            return Results.BadRequest("Mã giảm giá đã hết lượt dùng.");
    }

    db.Bookings.Add(booking);

    if (venue.OwnerId != null)
    {
        var who = me.FindFirst("name")?.Value ?? "Một khách";
        var hoursText = string.Join(", ", hours.Select(h => $"{h:00}h-{h + 1:00}h"));
        Notify.Add(db, venue.OwnerId.Value, "booking", "Có đơn đặt mới",
            $"{who} đặt {venue.Name} ngày {booking.Date:dd/MM/yyyy}, {hoursText}.");
    }

    try
    {
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
    catch (DbUpdateException)
    {
        return Results.Conflict(
            "Có khung giờ vừa bị người khác đặt.");
    }

    booking.Venue = venue;

    return Results.Ok(BookingDto.From(booking));
}).RequireAuthorization();

// ---------- Hủy đơn đặt sân ----------
app.MapPost("/api/bookings/{id:guid}/cancel", async (
    Guid id,
    ClaimsPrincipal me,
    AppDbContext db) =>
{
    var b = await db.Bookings
        .Include(x => x.Slots)
        .Include(x => x.Venue)
        .FirstOrDefaultAsync(x => x.Id == id);

    if (b is null)
        return Results.NotFound();

    var uid = UserIdOf(me);

    // Admin, người đặt hoặc chủ sân được phép hủy.
    var allowed =
        me.IsInRole("Admin") ||
        b.UserId == uid ||
        b.Venue?.OwnerId == uid;

    if (!allowed)
        return Results.StatusCode(403);

    if (b.Status != "Đã đặt")
        return Results.BadRequest("Đơn này không hủy được.");

    b.Status = "Đã hủy";

    var code = b.Id.ToString("N")[..8].ToUpper();
    if (b.UserId != uid)
    {
        // Chủ sân hoặc quản trị viên hủy: báo cho khách
        Notify.Add(db, b.UserId, "booking", "Đơn đặt sân đã bị hủy",
            $"Đơn {code} tại {b.Venue?.Name} ngày {b.Date:dd/MM/yyyy} đã được chủ sân hoặc quản trị viên hủy.");
    }
    else if (b.Venue?.OwnerId != null && b.Venue.OwnerId != uid)
    {
        // Khách hủy: báo cho chủ sân
        Notify.Add(db, b.Venue.OwnerId.Value, "booking", "Khách đã hủy đơn",
            $"Đơn {code} tại {b.Venue.Name} ngày {b.Date:dd/MM/yyyy} đã bị khách hủy.");
    }

    if (b.CheckedInAt != null || BookingTime.EndOf(b) <= DateTime.Now)
        return Results.BadRequest("Đơn đã qua giờ chơi hoặc đã check-in nên không hủy được.");

    db.BookingSlots.RemoveRange(b.Slots);

    await db.SaveChangesAsync();

    return Results.Ok();
}).RequireAuthorization();

app.MapPut("/api/auth/profile", async (UpdateProfileRequest req, ClaimsPrincipal me, AppDbContext db) =>
{
    var uid = UserIdOf(me);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid);
    if (user is null) return Results.Unauthorized();

    var name = (req.FullName ?? "").Trim();
    if (name == "") return Results.BadRequest("Bạn chưa nhập họ tên.");

    user.FullName = name;
    await db.SaveChangesAsync();

    // Cấp lại token vì tên nằm trong token
    return Results.Ok(MakeAuth(user));
}).RequireAuthorization();

app.MapPost("/api/auth/change-password", async (ChangePasswordRequest req, ClaimsPrincipal me, AppDbContext db, IPasswordHasher<AppUser> hasher) =>
{
    var uid = UserIdOf(me);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid);
    if (user is null) return Results.Unauthorized();

    if (hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword ?? "") == PasswordVerificationResult.Failed)
        return Results.BadRequest("Mật khẩu hiện tại không đúng.");

    if ((req.NewPassword ?? "").Length < 6)
        return Results.BadRequest("Mật khẩu mới cần ít nhất 6 ký tự.");

    user.PasswordHash = hasher.HashPassword(user, req.NewPassword!);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// Lấy lại token mới, dùng khi quyền của tài khoản vừa thay đổi (ví dụ được duyệt làm chủ sân)
app.MapPost("/api/auth/refresh", async (ClaimsPrincipal me, AppDbContext db) =>
{
    var uid = UserIdOf(me);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid);
    if (user is null) return Results.Unauthorized();
    return Results.Ok(MakeAuth(user));
}).RequireAuthorization();

// ---------- API dành cho Admin ----------
app.MapAdminEndpoints();

// ---------- API dành cho chủ sân ----------
app.MapOwnerEndpoints();
app.MapNotificationEndpoints();

// ---------- API dành cho khách hàng ----------
app.MapCustomerEndpoints();

app.MapAuthEndpoints(MakeAuth);
app.MapLoyaltyEndpoints();
app.MapRecurringEndpoints();
app.MapSplitEndpoints();
app.MapMatchEndpoints();

app.Run();