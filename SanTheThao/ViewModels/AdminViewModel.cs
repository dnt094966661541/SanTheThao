using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class AdminViewModel : ObservableObject
{
    private readonly IAdminService _admin;

    public AdminViewModel(IAdminService admin)
    {
        _admin = admin;
    }

    public ObservableCollection<AdminUser> Users { get; } = new();

    [ObservableProperty] private string customersText = "0";
    [ObservableProperty] private string ownersText = "0";
    [ObservableProperty] private string venuesText = "0";
    [ObservableProperty] private string bookingsText = "0";
    [ObservableProperty] private string revenueText = "0 đ";

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var stats = await _admin.GetStatsAsync();
            if (stats != null)
            {
                CustomersText = stats.Customers.ToString();
                OwnersText = stats.Owners.ToString();
                VenuesText = stats.Venues.ToString();
                BookingsText = stats.Bookings.ToString();
                RevenueText = $"{stats.Revenue:N0} đ";
            }

            var users = await _admin.GetUsersAsync();
            Users.Clear();
            foreach (var u in users) Users.Add(u);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không tải được dữ liệu quản trị. Bạn hãy kiểm tra API đã chạy chưa.", "OK");
        }
    }

    [RelayCommand]
    private async Task ToggleLockAsync(AdminUser user)
    {
        var action = user.IsLocked ? "mở khóa" : "khóa";
        var yes = await Shell.Current.DisplayAlertAsync("Xác nhận",
            $"Bạn muốn {action} tài khoản {user.FullName}?", "Đồng ý", "Không");
        if (!yes) return;

        var error = await _admin.ToggleLockAsync(user.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ChangeRoleAsync(AdminUser user)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"Vai trò của {user.FullName}", "Hủy", null, "Khách hàng", "Chủ sân");
        if (choice != "Khách hàng" && choice != "Chủ sân") return;

        var role = choice == "Chủ sân" ? "Owner" : "Customer";
        var error = await _admin.SetRoleAsync(user.Id, role);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa đổi được", error, "OK");
            return;
        }

        await Shell.Current.DisplayAlertAsync("Xong",
            "Đã đổi vai trò. Người này cần đăng xuất rồi đăng nhập lại để dùng quyền mới.", "OK");
        await LoadAsync();
    }

    // Admin xem tất cả đơn đặt bằng trang đơn đặt của chủ sân (Admin thấy mọi sân)
    [RelayCommand]
    private Task AllBookingsAsync() => Shell.Current.GoToAsync("OwnerBookings");

    [RelayCommand]
    private Task ApplicationsAsync() => Shell.Current.GoToAsync("AdminApplications");
}