using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class NotificationService(HttpClient http, IBookingService bookings,
                                 IReminderService reminder, IAuthService auth) : INotificationService
{
    // Nhắc trước giờ chơi bao nhiêu phút. Muốn thử nhanh thì tạm đổi số này (xem phần Chạy thử).
    public static int ReminderLeadMinutes = 60;

    private IDispatcherTimer? _timer;
    private int _lastShown;
    private bool _primed;
    private bool _polling;

    public int UnreadCount { get; private set; }
    public event Action? UnreadChanged;

    private void SetUnread(int count)
    {
        if (count == UnreadCount) return;
        UnreadCount = count;
        UnreadChanged?.Invoke();
    }

    // ----- Hỏi máy chủ định kỳ -----
    public void StartPolling()
    {
        StopPolling();

        var timer = Application.Current?.Dispatcher.CreateTimer();
        if (timer == null) return;

        timer.Interval = TimeSpan.FromSeconds(30);
        timer.Tick += async (_, _) => await PollAsync();
        timer.Start();
        _timer = timer;

        _ = PollAsync();
    }

    public void StopPolling()
    {
        _timer?.Stop();
        _timer = null;
        _primed = false;
        _lastShown = 0;
        SetUnread(0);
    }

    private async Task PollAsync()
    {
        if (_polling || auth.CurrentUser == null) return;
        _polling = true;
        try
        {
            var summary = await http.GetFromJsonAsync<NotificationSummary>("api/notifications/unread");
            if (summary == null) return;

            SetUnread(summary.Unread);

            // Lần hỏi đầu tiên chỉ ghi nhận, không hiện lại thông báo cũ
            var fresh = summary.Latest.Where(n => n.Id > _lastShown).OrderBy(n => n.Id).ToList();
            if (_primed && fresh.Count > 0 && await reminder.EnsurePermissionAsync())
            {
                foreach (var n in fresh)
                    reminder.ShowNow(900_000_000 + n.Id, n.Title, n.Body);
            }

            if (summary.Latest.Count > 0)
                _lastShown = Math.Max(_lastShown, summary.Latest.Max(n => n.Id));
            _primed = true;
        }
        catch (Exception)
        {
            // Mạng chập chờn thì lần sau hỏi lại
        }
        finally
        {
            _polling = false;
        }
    }

    // ----- Hộp thông báo -----
    public async Task<List<NotificationItem>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<NotificationItem>>("api/notifications") ?? new();

    public async Task MarkReadAsync(int id)
    {
        try
        {
            var res = await http.PostAsync($"api/notifications/{id}/read", null);
            if (res.IsSuccessStatusCode && UnreadCount > 0) SetUnread(UnreadCount - 1);
        }
        catch (Exception)
        {
        }
    }

    public async Task MarkAllReadAsync()
    {
        try
        {
            var res = await http.PostAsync("api/notifications/read-all", null);
            if (res.IsSuccessStatusCode) SetUnread(0);
        }
        catch (Exception)
        {
        }
    }

    // ----- Nhắc giờ chơi -----
    public async Task SyncRemindersAsync()
    {
        var user = auth.CurrentUser;
        if (user == null || user.Role == "Admin") return;   // quản trị viên thấy đơn của mọi người nên không đặt nhắc

        try
        {
            var list = await bookings.GetAllAsync();
            var enabled = Preferences.Default.Get("reminders_enabled", true);
            var now = DateTime.Now;

            foreach (var b in list.Where(b => !enabled || b.IsCancelled))
                reminder.Cancel(b.ReminderId);

            if (!enabled) return;

            var upcoming = list
                .Where(b => !b.IsCancelled && b.StartTime.AddMinutes(-ReminderLeadMinutes) > now.AddSeconds(5))
                .ToList();
            if (upcoming.Count == 0) return;

            if (!await reminder.EnsurePermissionAsync()) return;

            foreach (var b in upcoming)
            {
                reminder.Schedule(
                    b.ReminderId,
                    b.StartTime.AddMinutes(-ReminderLeadMinutes),
                    "Sắp đến giờ chơi!",
                    $"{b.VenueName} lúc {b.StartTime:HH:mm} ngày {b.StartTime:dd/MM}. Nhớ mang mã QR để check-in.");
            }
        }
        catch (Exception)
        {
            // Không đặt được nhắc thì thôi, không làm ảnh hưởng việc khác
        }
    }
}