using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SanTheThao.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> log) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var host = config["Email:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(config["Email:Port"], out var p) ? p : 587;
        var user = config["Email:User"];
        var pass = config["Email:Password"];

        // Chưa cấu hình Gmail: không gửi thật, in nội dung thư ra cửa sổ API để thử
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            log.LogWarning("EMAIL CHƯA CẤU HÌNH, thư không gửi thật. Gửi tới {To} | {Subject}\n{Body}",
                to, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config["Email:FromName"] ?? "SânThểThao", user));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

public interface ISmsSender
{
    bool IsSimulated { get; }
    Task SendAsync(string phone, string message);
}

// Giả lập: không gửi tin thật, chỉ in ra cửa sổ API.
// Khi dùng nhà cung cấp tin nhắn thật, viết một lớp mới theo ISmsSender và đăng ký thay cho lớp này.
public class SimulatedSmsSender(ILogger<SimulatedSmsSender> log) : ISmsSender
{
    public bool IsSimulated => true;

    public Task SendAsync(string phone, string message)
    {
        log.LogWarning("TIN NHẮN GIẢ LẬP gửi tới {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}

public static class EmailTemplates
{
    private static string Wrap(string inner) => $@"
<div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px;border:1px solid #E5E7EB;border-radius:12px'>
  <h2 style='color:#1B8A3A;margin-top:0'>SânThểThao</h2>
  {inner}
  <p style='color:#6B7280;font-size:12px;margin-bottom:0'>Đây là thư tự động, bạn không cần trả lời.</p>
</div>";

    public static string Otp(string name, string code, int minutes) => Wrap($@"
  <p>Xin chào {WebUtility.HtmlEncode(name)},</p>
  <p>Mã xác thực của bạn là:</p>
  <p style='font-size:34px;font-weight:bold;letter-spacing:8px;color:#1B8A3A;margin:16px 0'>{code}</p>
  <p>Mã có hiệu lực trong {minutes} phút. Bạn đừng cho ai biết mã này.</p>
  <p>Nếu bạn không yêu cầu mã, hãy bỏ qua thư này.</p>");

    public static string Welcome(string name) => Wrap($@"
  <p>Xin chào {WebUtility.HtmlEncode(name)},</p>
  <p>Chúc mừng bạn đã tạo tài khoản <b>SânThểThao</b> thành công.</p>
  <p>Từ bây giờ bạn có thể tìm sân, xem giá theo từng khung giờ và đặt sân ngay trên app.</p>
  <p>Chúc bạn chơi thể thao vui vẻ!</p>");
}