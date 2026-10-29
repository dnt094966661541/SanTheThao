using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private readonly ICustomerService _customer;
    private readonly INotificationService _notifications; // Chỗ 1: Thêm dịch vụ thông báo

    // Chỗ 2: Cập nhật hàm tạo để tiêm INotificationService
    public ProfileViewModel(IAuthService auth, ICustomerService customer, INotificationService notifications)
    {
        _auth = auth;
        _customer = customer;
        _notifications = notifications;
    }

    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string roleText = "";
    [ObservableProperty] private string initial = "?";

    [ObservableProperty] private bool showOwnerSection;
    [ObservableProperty] private string applicationText = "";
    [ObservableProperty] private bool showApply;
    [ObservableProperty] private bool showRefresh;

    // Chỗ 3: Thêm thuộc tính lưu trạng thái công tắc nhắc giờ và xử lý sự kiện thay đổi
    [ObservableProperty]
    private bool remindersEnabled = Preferences.Default.Get("reminders_enabled", true);

    partial void OnRemindersEnabledChanged(bool value)
    {
        Preferences.Default.Set("reminders_enabled", value);
        _ = _notifications.SyncRemindersAsync();   // bật thì đặt lại lịch nhắc, tắt thì hủy
    }

    public void Refresh()
    {
        var u = _auth.CurrentUser;
        FullName = u?.FullName ?? "";
        Email = u?.Email ?? "";
        RoleText = _auth.RoleText;
        var last = (u?.FullName ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        Initial = string.IsNullOrEmpty(last) ? "?" : last[..1].ToUpper();
        ShowOwnerSection = u?.Role == "Customer";
    }

    // Chỉ khách hàng mới thấy mục "Bạn có sân thể thao?"
    [RelayCommand]
    private async Task LoadApplicationAsync()
    {
        ShowApply = false;
        ShowRefresh = false;
        ApplicationText = "";
        if (!ShowOwnerSection) return;

        try
        {
            var app = await _customer.GetMyApplicationAsync();
            switch (app?.Status)
            {
                case "Chờ duyệt":
                    ApplicationText = "Hồ sơ của bạn đang chờ quản trị viên duyệt.";
                    break;
                case "Đã duyệt":
                    ApplicationText = "Hồ sơ đã được duyệt! Bấm nút bên dưới để dùng quyền chủ sân.";
                    ShowRefresh = true;
                    break;
                case "Từ chối":
                    ApplicationText = $"Hồ sơ chưa được duyệt. Lý do: {app.AdminNote}";
                    ShowApply = true;
                    break;
                default:
                    ApplicationText = "Đăng ký để đưa sân của bạn lên app và nhận khách đặt.";
                    ShowApply = true;
                    break;
            }
        }
        catch (Exception)
        {
            // Không tải được thì ẩn mục này, các chức năng khác vẫn dùng bình thường
        }
    }

    [RelayCommand]
    private Task ApplyAsync() => Shell.Current.GoToAsync("OwnerApply");

    [RelayCommand]
    private async Task RefreshRoleAsync()
    {
        var error = await _auth.RefreshAsync();
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa cập nhật được", error, "OK");
            return;
        }

        (Shell.Current as AppShell)?.ApplyRole(_auth.CurrentUser?.Role);
        Refresh();
        await LoadApplicationAsync();
        await Shell.Current.DisplayAlertAsync("Xong", "Bạn đã là chủ sân. Tab Quản lý đã sẵn sàng.", "OK");
    }

    [RelayCommand]
    private async Task EditNameAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync(
            "Đổi họ tên", "Nhập họ tên mới", "Lưu", "Hủy", initialValue: FullName, maxLength: 100);
        if (string.IsNullOrWhiteSpace(name)) return;

        var error = await _auth.UpdateProfileAsync(name);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa đổi được", error, "OK");
            return;
        }
        Refresh();
    }

    [RelayCommand]
    private Task ChangePasswordAsync() => Shell.Current.GoToAsync("ChangePassword");

    [RelayCommand]
    private Task GoHistoryAsync() => Shell.Current.GoToAsync("//HistoryPage");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Logout();
        (Shell.Current as AppShell)?.ApplyRole(null);
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private Task GoLoyaltyAsync() => Shell.Current.GoToAsync("Loyalty");

    [RelayCommand]
    private Task GoRecurringAsync() => Shell.Current.GoToAsync("Recurring");
}