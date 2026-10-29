using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly IVenueService _venueService;
    private readonly ICustomerService _customer;
    private readonly IAuthService _auth;
    private readonly INotificationService _notifications; // Chỗ 1: Thêm dịch vụ thông báo
    private List<Venue> _allVenues = new();

    // Chỗ 2: Cập nhật hàm tạo để tiêm INotificationService và đăng ký sự kiện
    public HomeViewModel(
        IVenueService venueService,
        ICustomerService customer,
        IAuthService auth,
        INotificationService notifications)
    {
        _venueService = venueService;
        _customer = customer;
        _auth = auth;
        _notifications = notifications;
        _notifications.UnreadChanged += () => MainThread.BeginInvokeOnMainThread(RefreshBadge);
    }

    public List<SportChip> Chips { get; } = new()
    {
        new() { Name = "Tất cả", Icon = "🏟️", IsSelected = true },
        new() { Name = "Bóng đá", Icon = "⚽" },
        new() { Name = "Cầu lông", Icon = "🏸" },
        new() { Name = "Pickleball", Icon = "🏓" },
        new() { Name = "Tennis", Icon = "🎾" },
        new() { Name = "Bóng rổ", Icon = "🏀" },
    };

    public List<string> SortOptions { get; } = new()
    {
        "Mặc định", "Giá thấp đến cao", "Đánh giá cao nhất"
    };

    public ObservableCollection<Venue> Venues { get; } = new();

    [ObservableProperty] private string greetingText = "Xin chào 👋";
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private string selectedSport = "Tất cả";
    [ObservableProperty] private string selectedSort = "Mặc định";
    [ObservableProperty] private bool onlyFavorites;
    [ObservableProperty] private bool isRefreshing;

    // Chỗ 3: Thêm các thuộc tính và phương thức xử lý Badge thông báo
    [ObservableProperty] private bool hasUnread;
    [ObservableProperty] private string badgeText = "";

    private void RefreshBadge()
    {
        var n = _notifications.UnreadCount;
        HasUnread = n > 0;
        BadgeText = n > 9 ? "9+" : n.ToString();
    }

    [RelayCommand]
    private Task OpenNotificationsAsync() => Shell.Current.GoToAsync("Notifications");

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedSortChanged(string value) => ApplyFilter();
    partial void OnOnlyFavoritesChanged(bool value) => ApplyFilter();

    partial void OnSelectedSportChanged(string value)
    {
        foreach (var c in Chips) c.IsSelected = c.Name == value;
        ApplyFilter();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Lời chào bằng tên (từ cuối của họ tên)
        var full = _auth.CurrentUser?.FullName ?? "";
        var last = full.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        GreetingText = string.IsNullOrEmpty(last) ? "Xin chào 👋" : $"Xin chào, {last} 👋";

        // Chỗ 4: Cập nhật gọi hàm làm mới badge và đồng bộ nhắc nhở ngay sau lời chào
        RefreshBadge();
        _ = _notifications.SyncRemindersAsync();

        try
        {
            _allVenues = await _venueService.GetVenuesAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không kết nối được máy chủ. Bạn hãy kiểm tra API đã chạy chưa.", "OK");
            return;
        }

        try
        {
            var favorites = (await _customer.GetFavoriteIdsAsync()).ToHashSet();
            foreach (var v in _allVenues)
                v.IsFavorite = favorites.Contains(v.Id);
        }
        catch (Exception)
        {
        }

        ApplyFilter();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private void SelectSport(string sport) => SelectedSport = sport;

    [RelayCommand]
    private async Task OpenVenueAsync(Venue venue)
    {
        await Shell.Current.GoToAsync($"VenueDetail?id={venue.Id}");
    }

    private void ApplyFilter()
    {
        var keyword = SearchText.Trim().ToLower();

        IEnumerable<Venue> result = _allVenues.Where(v =>
            (SelectedSport == "Tất cả" || v.Sport == SelectedSport) &&
            (keyword == "" ||
             v.Name.ToLower().Contains(keyword) ||
             v.District.ToLower().Contains(keyword)));

        if (OnlyFavorites)
            result = result.Where(v => v.IsFavorite);

        result = SelectedSort switch
        {
            "Giá thấp đến cao" => result.OrderBy(v => v.PricePerHour),
            "Đánh giá cao nhất" => result.OrderByDescending(v => v.Rating),
            _ => result.OrderBy(v => v.Id)
        };

        Venues.Clear();
        foreach (var v in result)
            Venues.Add(v);
    }
}