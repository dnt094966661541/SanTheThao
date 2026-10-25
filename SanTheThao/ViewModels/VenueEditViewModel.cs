using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(VenueId), "id")]
public partial class VenueEditViewModel : ObservableObject
{
    private readonly IOwnerService _owner;
    private int _id;
    private bool _loaded;
    private FileResult? _picked;

    private static readonly Dictionary<string, string> Icons = new()
    {
        ["Bóng đá"] = "⚽",
        ["Cầu lông"] = "🏸",
        ["Pickleball"] = "🏓",
        ["Tennis"] = "🎾",
        ["Bóng rổ"] = "🏀"
    };

    public VenueEditViewModel(IOwnerService owner)
    {
        _owner = owner;
    }

    public string VenueId { get; set; } = "0";
    public List<string> Sports { get; } = Icons.Keys.ToList();

    public bool HasPhoto => PhotoSource != null;
    public bool HasNoPhoto => PhotoSource == null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyPropertyChangedFor(nameof(HasNoPhoto))]
    private ImageSource? photoSource;

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

        int.TryParse(VenueId, out _id);
        if (_id == 0)
        {
            PageTitle = "Thêm sân";
            return;
        }

        PageTitle = "Sửa sân";
        var v = (await _owner.GetVenuesAsync()).FirstOrDefault(x => x.Id == _id);
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

        if (v.FullImageUrl != null)
            PhotoSource = ImageSource.FromUri(new Uri(v.FullImageUrl));
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Chọn ảnh sân" });
            if (file == null) return;

            _picked = file;
            PhotoSource = ImageSource.FromFile(file.FullPath);
            ErrorText = "";
        }
        catch (Exception)
        {
            ErrorText = "Không mở được thư viện ảnh.";
        }
    }

    // Chấp nhận cả "80000", "80.000" và "80,000"
    private static bool TryNumber(string? text, out decimal number)
    {
        var digits = new string((text ?? "").Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, out number);
    }

    private async Task<string?> UploadPhotoAsync(int venueId)
    {
        await using var stream = await _picked!.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        if (ms.Length > 10 * 1024 * 1024)
            return "Ảnh quá lớn, tối đa 10 MB.";

        var type = Path.GetExtension(_picked.FileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return await _owner.UploadVenueImageAsync(venueId, ms.ToArray(), type);
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
            Id = _id,
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

        var (error, savedId) = await _owner.SaveVenueAsync(venue);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        if (_picked != null)
        {
            var uploadError = await UploadPhotoAsync(savedId);
            if (uploadError != null)
            {
                await Shell.Current.DisplayAlertAsync("Đã lưu sân nhưng chưa tải được ảnh",
                    uploadError + "\nBạn mở lại mục Sửa sân để chọn ảnh lần nữa.", "OK");
            }
        }

        await Shell.Current.GoToAsync("..");
    }
}