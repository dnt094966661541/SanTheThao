using Android.App;
using Android.Content;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using SanTheThao.Services;

namespace SanTheThao;

// Quyền hiện thông báo trên Android 13 trở lên
public class PostNotificationsPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { ("android.permission.POST_NOTIFICATIONS", true) };
}

// Nhận tín hiệu từ báo thức và hiện thông báo
[BroadcastReceiver(Enabled = true, Exported = false)]
public class ReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        var id = intent.GetIntExtra("id", 0);
        var title = intent.GetStringExtra("title") ?? "SânThểThao";
        var body = intent.GetStringExtra("body") ?? "";

        AndroidReminderService.Show(context, id, title, body);
    }
}

public class AndroidReminderService : IReminderService
{
    private const string ChannelId = "santhethao_default";
    private static bool _asked;

    private static Context Ctx => Platform.AppContext;

    // Xin quyền hiện thông báo (chỉ hỏi một lần mỗi lần mở app)
    public async Task<bool> EnsurePermissionAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return true;

        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var status = await Permissions.CheckStatusAsync<PostNotificationsPermission>();
            if (status == PermissionStatus.Granted) return true;
            if (_asked) return false;

            _asked = true;
            status = await Permissions.RequestAsync<PostNotificationsPermission>();
            return status == PermissionStatus.Granted;
        });
    }

    // Hẹn giờ hiện thông báo. Cùng mã id thì thay lịch cũ.
    public void Schedule(int id, DateTime at, string title, string body)
    {
        var triggerAt = new DateTimeOffset(at).ToUnixTimeMilliseconds();
        if (triggerAt <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 2000) return;

        EnsureChannel(Ctx);

        var alarm = (AlarmManager?)Ctx.GetSystemService(Context.AlarmService);
        alarm?.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAt, AlarmIntent(id, title, body));
    }

    public void Cancel(int id)
    {
        var alarm = (AlarmManager?)Ctx.GetSystemService(Context.AlarmService);
        alarm?.Cancel(AlarmIntent(id, "", ""));
    }

    public void ShowNow(int id, string title, string body) => Show(Ctx, id, title, body);

    private static PendingIntent AlarmIntent(int id, string title, string body)
    {
        var intent = new Intent(Ctx, typeof(ReminderReceiver));
        intent.PutExtra("id", id);
        intent.PutExtra("title", title);
        intent.PutExtra("body", body);

        return PendingIntent.GetBroadcast(Ctx, id, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;
    }

    private static void EnsureChannel(Context ctx)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var manager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        if (manager == null || manager.GetNotificationChannel(ChannelId) != null) return;

        var channel = new NotificationChannel(ChannelId, "Thông báo SânThểThao", NotificationImportance.High)
        {
            Description = "Nhắc giờ chơi và thông báo từ ứng dụng"
        };
        manager.CreateNotificationChannel(channel);
    }

    public static void Show(Context ctx, int id, string title, string body)
    {
        EnsureChannel(ctx);

        // Bấm vào thông báo thì mở app
        PendingIntent? tap = null;
        var launch = ctx.PackageManager?.GetLaunchIntentForPackage(ctx.PackageName!);
        if (launch != null)
        {
            tap = PendingIntent.GetActivity(ctx, 0, launch,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        }

        Notification.Builder builder = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(ctx, ChannelId)
            : new Notification.Builder(ctx);

        builder.SetContentTitle(title)
               .SetContentText(body)
               .SetStyle(new Notification.BigTextStyle().BigText(body))
               .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
               .SetAutoCancel(true);

        if (tap != null) builder.SetContentIntent(tap);

        var manager = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
        manager?.Notify(id, builder.Build());
    }
}