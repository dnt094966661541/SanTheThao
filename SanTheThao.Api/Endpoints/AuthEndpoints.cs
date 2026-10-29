using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;
using SanTheThao.Api.Services;

namespace SanTheThao.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app, Func<AppUser, AuthResponse> makeAuth)
    {
        // ----- Đăng ký: tạo tài khoản chưa xác minh và gửi mã -----
        app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db,
            IPasswordHasher<AppUser> hasher, OtpService otp, IEmailSender email, ISmsSender sms,
            IConfiguration config, ILoggerFactory lf) =>
        {
            var name = (req.FullName ?? "").Trim();
            var mail = (req.Email ?? "").Trim().ToLowerInvariant();
            var phone = NormalizePhone(req.Phone);
            var password = req.Password ?? "";
            var channel = NormalizeChannel(req.Channel);

            if (name == "" || name.Length > 100)
                return Results.BadRequest("Bạn chưa nhập họ tên (tối đa 100 ký tự).");
            if (mail.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(mail, out _))
                return Results.BadRequest("Email chưa đúng.");
            if (phone is null)
                return Results.BadRequest("Số điện thoại chưa đúng (ví dụ 0912345678).");
            if (password.Length < 6)
                return Results.BadRequest("Mật khẩu cần ít nhất 6 ký tự.");

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == mail);
            if (user != null && user.IsVerified)
                return Results.Conflict("Email này đã được đăng ký.");

            var phoneOwner = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (phoneOwner != null && phoneOwner.Id != user?.Id)
                return Results.Conflict("Số điện thoại này đã được đăng ký.");

            // Đăng ký dở dang lần trước (chưa xác minh): cho đăng ký lại, cập nhật thông tin mới
            if (user is null)
            {
                user = new AppUser { Email = mail, Role = "Customer" };
                db.Users.Add(user);
            }
            user.FullName = name;
            user.Phone = phone;
            user.PasswordHash = hasher.HashPassword(user, password);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict("Email hoặc số điện thoại này đã được đăng ký.");
            }

            var (error, sent) = await DeliverOtpAsync(user, channel, otp, email, sms, config, lf.CreateLogger("Auth"));
            return error != null ? Results.BadRequest(error) : Results.Ok(sent);
        });

        // ----- Gửi (hoặc gửi lại) mã OTP cho email hoặc số điện thoại -----
        app.MapPost("/api/auth/otp/send", async (OtpSendRequest req, AppDbContext db, OtpService otp,
            IEmailSender email, ISmsSender sms, IConfiguration config, ILoggerFactory lf) =>
        {
            var channel = NormalizeChannel(req.Channel);
            var identifier = (req.Identifier ?? "").Trim();
            if (identifier == "")
                return Results.BadRequest("Bạn chưa nhập email hoặc số điện thoại.");

            var user = await FindUserAsync(db, identifier);

            // Không cho biết tài khoản có tồn tại hay không: vẫn trả lời như đã gửi
            if (user is null || user.IsLocked)
            {
                var fake = new OtpSentResponse(identifier, channel,
                    identifier.Contains('@') ? MaskEmail(identifier) : MaskPhone(identifier),
                    null, (int)OtpService.Lifetime.TotalSeconds);
                return Results.Ok(fake);
            }

            var (error, sent) = await DeliverOtpAsync(user, channel, otp, email, sms, config, lf.CreateLogger("Auth"));
            return error != null ? Results.BadRequest(error) : Results.Ok(sent);
        });

        // ----- Kiểm tra mã OTP: đúng thì đăng nhập (tài khoản mới được xác minh luôn) -----
        app.MapPost("/api/auth/otp/verify", async (OtpVerifyRequest req, AppDbContext db, OtpService otp,
            IEmailSender email, ILoggerFactory lf) =>
        {
            var code = new string((req.Code ?? "").Where(char.IsDigit).ToArray());
            if (code.Length != 6)
                return Results.BadRequest("Mã gồm 6 số.");

            var user = await FindUserAsync(db, req.Identifier);
            if (user is null)
                return Results.BadRequest("Mã không đúng hoặc đã hết hạn.");

            if (user.IsLocked)
                return Results.Json("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.", statusCode: 403);

            var error = await otp.VerifyAsync(user, code);
            if (error != null)
                return Results.BadRequest(error);

            if (!user.IsVerified)
            {
                user.IsVerified = true;
                await db.SaveChangesAsync();

                // Gửi email chào mừng. Gửi lỗi thì bỏ qua, không làm hỏng việc xác minh.
                try
                {
                    await email.SendAsync(user.Email, "Chào mừng bạn đến với SânThểThao",
                        EmailTemplates.Welcome(user.FullName));
                }
                catch (Exception ex)
                {
                    lf.CreateLogger("Auth").LogError(ex, "Không gửi được email chào mừng tới {Email}", user.Email);
                }
            }

            return Results.Ok(makeAuth(user));
        });
    }

    // Gửi mã qua email hoặc tin nhắn
    private static async Task<(string? Error, OtpSentResponse? Sent)> DeliverOtpAsync(
        AppUser user, string channel, OtpService otp, IEmailSender email, ISmsSender sms,
        IConfiguration config, ILogger log)
    {
        if (channel == "sms" && string.IsNullOrEmpty(user.Phone))
            return ("Tài khoản này chưa có số điện thoại. Bạn hãy chọn nhận mã qua email.", null);

        var (error, code) = await otp.CreateAsync(user);
        if (error != null) return (error, null);

        var minutes = (int)OtpService.Lifetime.TotalMinutes;
        string destination;
        string? dev = null;

        try
        {
            if (channel == "sms")
            {
                var text = $"SanTheThao: Ma xac thuc cua ban la {code}. Ma co hieu luc {minutes} phut. Khong chia se ma nay cho ai.";
                await sms.SendAsync(user.Phone!, text);
                destination = MaskPhone(user.Phone!);

                // Chế độ giả lập: trả nội dung tin nhắn về app để hiện lên màn hình
                if (sms.IsSimulated && config["Sms:Mode"] == "Simulated")
                    dev = text;
            }
            else
            {
                await email.SendAsync(user.Email, "Mã xác thực SânThểThao",
                    EmailTemplates.Otp(user.FullName, code!, minutes));
                destination = MaskEmail(user.Email);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Không gửi được mã OTP qua {Channel} cho {UserId}", channel, user.Id);
            return ("Chưa gửi được mã. Bạn hãy thử lại sau ít phút hoặc chọn cách nhận mã khác.", null);
        }

        var identifier = channel == "sms" ? user.Phone! : user.Email;
        return (null, new OtpSentResponse(identifier, channel, destination, dev, (int)OtpService.Lifetime.TotalSeconds));
    }

    private static async Task<AppUser?> FindUserAsync(AppDbContext db, string? identifier)
    {
        var id = (identifier ?? "").Trim();
        if (id.Contains('@'))
        {
            var mail = id.ToLowerInvariant();
            return await db.Users.FirstOrDefaultAsync(u => u.Email == mail);
        }

        var phone = NormalizePhone(id);
        return phone is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
    }

    private static string NormalizeChannel(string? c) =>
        (c ?? "").Trim().ToLowerInvariant() == "sms" ? "sms" : "email";

    // Nhận 0912345678, +84912345678 hoặc 84912345678, đổi về dạng 0912345678
    private static string? NormalizePhone(string? input)
    {
        var digits = new string((input ?? "").Where(char.IsDigit).ToArray());
        if (digits.StartsWith("84") && digits.Length == 11) digits = "0" + digits[2..];
        return Regex.IsMatch(digits, @"^0[35789]\d{8}$") ? digits : null;
    }

    private static string MaskEmail(string e)
    {
        var at = e.IndexOf('@');
        if (at < 1) return "***";
        var local = e[..at];
        var shown = local.Length <= 2 ? local[..1] : local[..2];
        return shown + "***" + e[at..];
    }

    private static string MaskPhone(string p) =>
        p.Length < 6 ? "***" : p[..2] + new string('*', p.Length - 5) + p[^3..];
}