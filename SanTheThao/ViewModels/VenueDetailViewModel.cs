using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(VenueId), "id")]
public partial class VenueDetailViewModel : ObservableObject
{
    private readonly IVenueService _venueService;
    private readonly IBookingService _bookingService;
    private readonly ICustomerService _customer;
    private readonly ICommunityService _community;
    private PromoInfo? _promo;

    public VenueDetailViewModel(
        IVenueService venueService,
        IBookingService bookingService,
        ICustomerService customer,
        ICommunityService community)
    {
        _venueService = venueService;
        _bookingService = bookingService;
        _customer = customer;
        _community = community;
    }

    public string VenueId { get; set; } = "";

    public DateTime MinDate => DateTime.Today;
    public DateTime MaxDate => DateTime.Today.AddDays(30);

    public ObservableCollection<TimeSlot> Slots { get; } = new();
    public ObservableCollection<Review> Reviews { get; } = new();
    public ObservableCollection<VenueImage> Images { get; } = new();

    public List<string> WeekOptions { get; } = new() { "4 tuần", "8 tuần", "12 tuần" };

    [ObservableProperty] private Venue? venue;
    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private string totalText = "Chưa chọn giờ nào";
    [ObservableProperty] private string promoCode = "";
    [ObservableProperty] private string promoMessage = "";
    [ObservableProperty] private bool hasNoReviews = true;
    [ObservableProperty] private bool hasImages;
    [ObservableProperty] private bool repeatWeekly;
    [ObservableProperty] private int weekIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteText))]
    private bool isFavorite;

    public string FavoriteText => IsFavorite ? "❤️ Đã yêu thích" : "🤍 Yêu thích";

    partial void OnSelectedDateChanged(DateTime value) => _ = LoadSlotsAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!int.TryParse(VenueId, out var id)) return;
        Venue = await _venueService.GetVenueAsync(id);
        await LoadSlotsAsync();

        try
        {
            var imgs = await _customer.GetVenueImagesAsync(id);
            Images.Clear();
            foreach (var i in imgs) Images.Add(i);
            HasImages = Images.Count > 0;
        }
        catch (Exception)
        {
            // Không tải được ảnh thì vẫn hiện ảnh nền mặc định
        }

        try
        {
            IsFavorite = (await _customer.GetFavoriteIdsAsync()).Contains(id);
        }
        catch (Exception)
        {
        }

        try
        {
            var list = await _customer.GetReviewsAsync(id);
            Reviews.Clear();
            foreach (var r in list) Reviews.Add(r);
            HasNoReviews = Reviews.Count == 0;
        }
        catch (Exception)
        {
            // Không tải được đánh giá thì bỏ qua, vẫn cho đặt sân bình thường
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Venue == null) return;
        try
        {
            IsFavorite = await _customer.ToggleFavoriteAsync(Venue.Id);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi", "Không cập nhật được sân yêu thích.", "OK");
        }
    }

    private async Task LoadSlotsAsync()
    {
        if (Venue == null) return;

        var booked = await _bookingService.GetBookedHoursAsync(Venue.Id, SelectedDate);

        Slots.Clear();
        for (int hour = 6; hour < 22; hour++)
        {
            var isPast = SelectedDate.Date.AddHours(hour + 1) <= DateTime.Now;
            Slots.Add(new TimeSlot
            {
                Hour = hour,
                Price = Venue.PriceAt(SelectedDate, hour),
                IsBooked = booked.Contains(hour) || isPast
            });
        }

        UpdateTotal();
    }

    [RelayCommand]
    private void ToggleSlot(TimeSlot slot)
    {
        if (slot.IsBooked) return;
        slot.IsSelected = !slot.IsSelected;
        UpdateTotal();
    }

    [RelayCommand]
    private async Task ApplyPromoAsync()
    {
        if (Venue == null) return;

        var code = PromoCode.Trim();
        if (code == "")
        {
            _promo = null;
            PromoMessage = "";
            UpdateTotal();
            return;
        }

        var (info, error) = await _customer.CheckPromoAsync(code, Venue.Id);
        _promo = info;
        PromoMessage = error ?? "";
        UpdateTotal();
    }

    [RelayCommand]
    private Task GoBookAsync()
    {
        if (Venue == null) return Task.CompletedTask;
        return Shell.Current.GoToAsync($"BookSlot?id={Venue.Id}");
    }

    // Cùng cách tính với server. Server mới là nơi tính tiền chính thức.
    private decimal DiscountFor(decimal subtotal)
    {
        if (_promo == null || subtotal < _promo.MinTotal) return 0;
        var d = Math.Floor(subtotal * _promo.PercentOff / 100m);
        if (_promo.MaxDiscount > 0) d = Math.Min(d, _promo.MaxDiscount);
        return d;
    }

    private decimal Subtotal() =>
        Venue == null ? 0 : Slots.Where(s => s.IsSelected).Sum(s => Venue.PriceAt(SelectedDate, s.Hour));

    private void UpdateTotal()
    {
        var count = Slots.Count(s => s.IsSelected);
        if (count == 0 || Venue == null)
        {
            TotalText = "Chưa chọn giờ nào";
            return;
        }

        var subtotal = Subtotal();
        var discount = DiscountFor(subtotal);

        if (_promo != null)
        {
            PromoMessage = discount > 0
                ? $"✅ Mã {_promo.Code}: {_promo.Description}"
                : $"Mã {_promo.Code} cần đơn từ {_promo.MinTotal:N0} đ.";
        }

        TotalText = discount > 0
            ? $"{count} giờ · {(subtotal - discount):N0} đ (đã giảm {discount:N0} đ)"
            : $"{count} giờ · {subtotal:N0} đ";
    }

    [RelayCommand]
    private async Task BookAsync()
    {
        var chosen = Slots.Where(s => s.IsSelected).ToList();
        if (chosen.Count == 0 || Venue == null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa chọn giờ", "Bạn hãy chọn ít nhất một khung giờ.", "OK");
            return;
        }

        if (RepeatWeekly)
        {
            var weeks = new[] { 4, 8, 12 }[Math.Clamp(WeekIndex, 0, 2)];
            var (err, res) = await _community.CreateRecurringAsync(
                Venue.Id, SelectedDate.Date, chosen.Select(s => s.Hour).ToList(), weeks);
            if (err != null)
            {
                await Shell.Current.DisplayAlertAsync("Không đặt được", err, "OK");
                await LoadSlotsAsync();
                return;
            }
            var skippedText = res!.Skipped.Count == 0 ? "" : "\nBỏ qua: " + string.Join(", ", res.Skipped);
            await Shell.Current.DisplayAlertAsync("Đặt sân cố định thành công",
                $"{Venue.Name}\n{res.Created} buổi, tổng {res.Total:N0} đ{skippedText}\n" +
                "Mã giảm giá không áp dụng cho đặt cố định.", "OK");
            await Shell.Current.GoToAsync("//HistoryPage");
            return;
        }

        var subtotal = Subtotal();
        var discount = DiscountFor(subtotal);

        var booking = new Booking
        {
            VenueId = Venue.Id,
            VenueName = Venue.Name,
            Icon = Venue.Icon,
            Date = SelectedDate.Date,
            Hours = chosen.Select(s => s.Hour).ToList(),
            TotalPrice = subtotal - discount,
            PromoCode = discount > 0 ? _promo?.Code : null
        };

        var error = await _bookingService.CreateAsync(booking);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Không đặt được", error, "OK");
            await LoadSlotsAsync();
            return;
        }

        await Shell.Current.DisplayAlertAsync("Đặt sân thành công",
            $"Mã đơn: {booking.Code}\n{Venue.Name}\nNgày {booking.Date:dd/MM/yyyy}\n{booking.HoursText}\nTổng: {booking.TotalPrice:N0} đ",
            "OK");

        await Shell.Current.GoToAsync("//HistoryPage");
    }

    // Mở Google Maps tìm đúng địa chỉ của sân (không cần khóa bản đồ)
    [RelayCommand]
    private async Task OpenDirectionsAsync()
    {
        if (Venue == null) return;
        var query = Uri.EscapeDataString($"{Venue.Name}, {Venue.Address}, {Venue.District}");
        try
        {
            await Launcher.Default.OpenAsync(new Uri($"https://www.google.com/maps/search/?api=1&query={query}"));
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Không mở được bản đồ",
                "Máy chưa có ứng dụng bản đồ hoặc trình duyệt.", "OK");
        }
    }
}