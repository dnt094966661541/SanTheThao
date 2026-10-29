using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IBookingService _bookingService;
    private readonly ICustomerService _customer;
    private List<Booking> _all = new();

    public HistoryViewModel(IBookingService bookingService, ICustomerService customer)
    {
        _bookingService = bookingService;
        _customer = customer;
    }

    public List<SportChip> Filters { get; } = new()
    {
        new() { Name = "Tất cả", IsSelected = true },
        new() { Name = "Sắp tới" },
        new() { Name = "Hoàn thành" },
        new() { Name = "Đã hủy" },
    };

    public ObservableCollection<Booking> Bookings { get; } = new();

    [ObservableProperty] private string selectedFilter = "Tất cả";
    [ObservableProperty] private bool isRefreshing;

    partial void OnSelectedFilterChanged(string value)
    {
        foreach (var f in Filters) f.IsSelected = f.Name == value;
        ApplyFilter();
    }

    [RelayCommand]
    private void SelectFilter(string name) => SelectedFilter = name;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _bookingService.GetAllAsync();
            _all = list.OrderByDescending(b => b.CreatedAt).ToList();
            ApplyFilter();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không tải được lịch sử đặt sân. Bạn hãy kiểm tra API đã chạy chưa.", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    private void ApplyFilter()
    {
        IEnumerable<Booking> r = _all;
        r = SelectedFilter switch
        {
            "Sắp tới" => r.Where(b => !b.IsCancelled && !b.IsDone),
            "Hoàn thành" => r.Where(b => b.IsDone),
            "Đã hủy" => r.Where(b => b.IsCancelled),
            _ => r
        };

        Bookings.Clear();
        foreach (var b in r) Bookings.Add(b);
    }

    [RelayCommand]
    private async Task CancelAsync(Booking booking)
    {
        if (!booking.CanCancel) return;

        var yes = await Shell.Current.DisplayAlertAsync("Hủy đơn",
            $"Bạn muốn hủy đơn {booking.Code}?", "Hủy đơn", "Không");
        if (!yes) return;

        await _bookingService.CancelAsync(booking.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private Task ShowQrAsync(Booking booking) =>
        Shell.Current.GoToAsync("BookingQr", new Dictionary<string, object> { ["booking"] = booking });

    [RelayCommand]
    private async Task ReviewAsync(Booking booking)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(
            "Bạn chấm sân này mấy sao?", "Để sau", null,
            "5 sao", "4 sao", "3 sao", "2 sao", "1 sao");

        if (string.IsNullOrEmpty(choice) || choice == "Để sau") return;
        var stars = int.Parse(choice[..1]);

        var comment = await Shell.Current.DisplayPromptAsync(
            "Nhận xét", "Bạn thấy sân thế nào? (có thể bỏ trống)", "Gửi", "Bỏ qua", maxLength: 300) ?? "";

        var error = await _customer.SubmitReviewAsync(booking.Id, stars, comment);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa gửi được", error, "OK");
            return;
        }

        await Shell.Current.DisplayAlertAsync("Cảm ơn bạn", "Đánh giá của bạn đã được gửi.", "OK");
        await LoadAsync();
    }
    [RelayCommand]
    private Task SplitAsync(Booking booking) =>
    Shell.Current.GoToAsync("Split", new Dictionary<string, object> { ["booking"] = booking });
}