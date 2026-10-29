using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Data;
using SanTheThao.Api.Models;
using System.Security.Cryptography;
using System.Text;
using static SanTheThao.Api.Models.Promotion;

namespace SanTheThao.Api.Services;

public class OtpService(AppDbContext db, IConfiguration config)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 5;
    private const int MaxSendsPerHour = 5;

    // Mã được băm bằng khóa bí mật của server, trong cơ sở dữ liệu không có mã gốc
    private string Hash(Guid userId, string code)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);
        using var h = new HMACSHA256(key);
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));
    }

    // Tạo mã mới. Trả về (lỗi, mã). Có lỗi thì mã là null.
    public async Task<(string? Error, string? Code)> CreateAsync(AppUser user)
    {
        var now = DateTime.UtcNow;

        var last = await db.OtpCodes
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (last != null && now - last.CreatedAt < Cooldown)
        {
            var wait = (int)Math.Ceiling((Cooldown - (now - last.CreatedAt)).TotalSeconds);
            return ($"Bạn vừa yêu cầu mã. Hãy chờ {wait} giây rồi thử lại.", null);
        }

        var count = await db.OtpCodes.CountAsync(o => o.UserId == user.Id && o.CreatedAt > now.AddHours(-1));
        if (count >= MaxSendsPerHour)
            return ("Bạn đã yêu cầu mã quá nhiều lần. Hãy thử lại sau ít phút.", null);

        // Mã cũ chưa dùng thì vô hiệu hóa
        var olds = await db.OtpCodes.Where(o => o.UserId == user.Id && o.UsedAt == null).ToListAsync();
        foreach (var o in olds) o.UsedAt = now;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.OtpCodes.Add(new OtpCode
        {
            UserId = user.Id,
            CodeHash = Hash(user.Id, code),
            CreatedAt = now,
            ExpiresAt = now + Lifetime
        });
        await db.SaveChangesAsync();

        return (null, code);
    }

    // Kiểm tra mã. Trả về null nếu đúng, ngược lại trả về nội dung lỗi.
    public async Task<string?> VerifyAsync(AppUser user, string code)
    {
        var now = DateTime.UtcNow;

        var otp = await db.OtpCodes
            .Where(o => o.UserId == user.Id && o.UsedAt == null)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp is null || otp.ExpiresAt < now)
            return "Mã không đúng hoặc đã hết hạn. Hãy bấm Gửi lại mã.";

        if (otp.Attempts >= MaxAttempts)
        {
            otp.UsedAt = now;
            await db.SaveChangesAsync();
            return "Bạn đã nhập sai quá nhiều lần. Hãy yêu cầu mã mới.";
        }

        otp.Attempts++;

        var expected = Convert.FromHexString(otp.CodeHash);
        var actual = Convert.FromHexString(Hash(user.Id, code));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            await db.SaveChangesAsync();
            return "Mã không đúng.";
        }

        otp.UsedAt = now;   // mỗi mã chỉ dùng được một lần
        await db.SaveChangesAsync();
        return null;
    }
}