using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(VenueId), "id")]
public partial class VenueEditViewModel : ObservableObject
{
    private readonly IOwnerService _owner;
    private readonly ICustomerService _customer;
    private bool _loaded;

    private static readonly Dictionary<string, string> Icons = new()
    {
        ["Bóng đá"] = "⚽",
        ["Cầu lông"] = "🏸",
        ["Pickleball"] = "🏓",
        ["Tennis"] = "🎾",
        ["Bóng rổ"] = "🏀"
    };

    public VenueEditViewModel(IOwnerService owner, ICustomerService customer)
    {
        _owner = owner;
        _customer = customer;
    }

    public string VenueId { get; set; } = "0";
    public List<string> Sports { get; } = Icons.Keys.ToList();
    public ObservableCollection<VenueImage> Images { get; } = new();

    // Mã sân trong cơ sở dữ liệu. Bằng 0 nghĩa là sân mới, chưa lưu.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGallery))]
    [NotifyPropertyChangedFor(nameof(ShowGalleryHint))]
    private int venueDbId;

    public bool HasGallery => VenueDbId > 0;
    public bool ShowGalleryHint => VenueDbId == 0;

    [ObservableProperty] private string photoCountText = "";
    [ObservableProperty] private string pageTitle = "Thêm sân";
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string sport = "Cầu lông";
    [ObservableProperty] private string address = "";
    [ObservableProperty] private string district = "";
    [ObservableProperty] private string priceText = "";
    [ObservableProperty] private string peakPriceText = "0";
    [ObservableProperty] private string peakStartText = "17";
    [ObservableProperty] private string peakEndText = "22";
    [ObservableProperty] private string weekendPriceText = "0";
    [ObservableProperty] private string errorText = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        int.TryParse(VenueId, out var id);
        VenueDbId = id;

        if (id == 0)
        {
            PageTitle = "Thêm sân";
            return;
        }

        PageTitle = "Sửa sân";
        var v = (await _owner.GetVenuesAsync()).FirstOrDefault(x => x.Id == id);
        if (v == null) return;

        Name = v.Name;
        Sport = v.Sport;
        Address = v.Address;
        District = v.District;
        PriceText = v.PricePerHour.ToString("0");
        PeakPriceText = v.PeakPricePerHour.ToString("0");
        PeakStartText = v.PeakStartHour.ToString();
        PeakEndText = v.PeakEndHour.ToString();
        WeekendPriceText = v.WeekendPricePerHour.ToString("0");

        await LoadImagesAsync();
    }

    private async Task LoadImagesAsync()
    {
        try
        {
            var list = await _customer.GetVenueImagesAsync(VenueDbId);
            Images.Clear();
            foreach (var i in list) Images.Add(i);
        }
        catch (Exception)
        {
            // Không tải được ảnh thì để trống, các phần khác vẫn dùng được
        }
        PhotoCountText = $"Đã có {Images.Count}/10 ảnh";
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Chọn ảnh sân" });
            if (file == null) return;

            await using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            if (ms.Length > 10 * 1024 * 1024)
            {
                await Shell.Current.DisplayAlertAsync("Ảnh quá lớn", "Mỗi ảnh tối đa 10 MB.", "OK");
                return;
            }

            var type = Path.GetExtension(file.FileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            var error = await _owner.UploadVenueImageAsync(VenueDbId, ms.ToArray(), type);
            if (error != null)
            {
                await Shell.Current.DisplayAlertAsync("Chưa tải được ảnh", error, "OK");
                return;
            }

            await LoadImagesAsync();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi", "Không mở được thư viện ảnh.", "OK");
        }
    }

    [RelayCommand]
    private async Task SetMainAsync(VenueImage image)
    {
        var error = await _owner.SetMainVenueImageAsync(VenueDbId, image.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa đổi được", error, "OK");
            return;
        }
        await LoadImagesAsync();
    }

    [RelayCommand]
    private async Task RemoveImageAsync(VenueImage image)
    {
        var yes = await Shell.Current.DisplayAlertAsync("Xóa ảnh", "Bạn muốn xóa ảnh này?", "Xóa", "Không");
        if (!yes) return;

        var error = await _owner.DeleteVenueImageAsync(VenueDbId, image.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa xóa được", error, "OK");
            return;
        }
        await LoadImagesAsync();
    }

    // Chấp nhận cả "80000", "80.000" và "80,000"
    private static bool TryNumber(string? text, out decimal number)
    {
        var digits = new string((text ?? "").Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, out number);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorText = "";

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorText = "Bạn chưa nhập tên sân.";
            return;
        }
        if (!TryNumber(PriceText, out var price) || price <= 0)
        {
            ErrorText = "Bạn hãy nhập giá thường (lớn hơn 0).";
            return;
        }
        if (!TryNumber(PeakPriceText, out var peakPrice) ||
            !TryNumber(WeekendPriceText, out var weekendPrice) ||
            !TryNumber(PeakStartText, out var peakStart) ||
            !TryNumber(PeakEndText, out var peakEnd))
        {
            ErrorText = "Giá và giờ cao điểm chỉ được nhập số.";
            return;
        }

        var venue = new Venue
        {
            Id = VenueDbId,
            Name = Name.Trim(),
            Sport = Sport,
            Icon = Icons.GetValueOrDefault(Sport, "🏟️"),
            Address = Address,
            District = District,
            PricePerHour = price,
            PeakPricePerHour = peakPrice,
            PeakStartHour = (int)peakStart,
            PeakEndHour = (int)peakEnd,
            WeekendPricePerHour = weekendPrice
        };

        var isNew = VenueDbId == 0;
        var (error, savedId) = await _owner.SaveVenueAsync(venue);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        if (isNew)
        {
            // Sân mới: ở lại trang này để chủ sân thêm ảnh
            VenueDbId = savedId;
            PageTitle = "Sửa sân";
            await LoadImagesAsync();
            await Shell.Current.DisplayAlertAsync("Đã tạo sân",
                "Bạn có thể thêm ảnh cho sân ở phần Ảnh sân bên dưới. Xong thì bấm nút quay lại.", "OK");
            return;
        }

        await Shell.Current.GoToAsync("..");
    }
}