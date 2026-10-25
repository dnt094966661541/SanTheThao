
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class OwnerHomeViewModel : ObservableObject
{
    private readonly IOwnerService _owner;

    public OwnerHomeViewModel(IOwnerService owner)
    {
        _owner = owner;
    }

    public ObservableCollection<Venue> Venues { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _owner.GetVenuesAsync();

            Venues.Clear();

            foreach (var venue in list)
            {
                Venues.Add(venue);
            }
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync(
                "Lỗi kết nối",
                "Không tải được danh sách sân. Bạn hãy kiểm tra API đã chạy chưa.",
                "OK");
        }
    }

    [RelayCommand]
    private Task AddAsync()
        => Shell.Current.GoToAsync("VenueEdit?id=0");

    [RelayCommand]
    private Task EditAsync(Venue venue)
        => Shell.Current.GoToAsync($"VenueEdit?id={venue.Id}");

    [RelayCommand]
    private async Task ToggleAsync(Venue venue)
    {
        await _owner.ToggleVenueAsync(venue.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private Task BookingsAsync()
        => Shell.Current.GoToAsync("OwnerBookings");

    [RelayCommand]
    private Task RevenueAsync()
        => Shell.Current.GoToAsync("Revenue");

    // Check-in bằng mã đơn
    [RelayCommand]
    private async Task CheckInAsync()
    {
        var code = await Shell.Current.DisplayPromptAsync(
            "Check-in khách",
            "Nhập mã đơn (8 ký tự khách đưa ra)",
            "Xác nhận",
            "Hủy",
            maxLength: 60);

        if (string.IsNullOrWhiteSpace(code))
            return;

        var (ok, message) = await _owner.CheckInAsync(code.Trim());

        await Shell.Current.DisplayAlertAsync(
            ok ? "✅ Check-in thành công" : "Không check-in được",
            message,
            "OK");
    }

    // Mở trang quét QR để check-in
    [RelayCommand]
    private Task ScanAsync()
        => Shell.Current.GoToAsync("ScanCheckIn");

    [RelayCommand]
    private Task PromosAsync()
        => Shell.Current.GoToAsync("OwnerPromos");
}
