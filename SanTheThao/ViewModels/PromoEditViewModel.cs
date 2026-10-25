using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class PromoEditViewModel : ObservableObject
{
    private readonly IOwnerService _owner;
    private List<Venue> _venues = new();
    private bool _loaded;

    public PromoEditViewModel(IOwnerService owner)
    {
        _owner = owner;
    }

    public ObservableCollection<string> VenueNames { get; } = new();

    [ObservableProperty] private string code = "";
    [ObservableProperty] private string percentText = "10";
    [ObservableProperty] private string maxDiscountText = "0";
    [ObservableProperty] private string minTotalText = "0";
    [ObservableProperty] private string usageLimitText = "0";
    [ObservableProperty] private DateTime validFrom = DateTime.Today;
    [ObservableProperty] private DateTime validTo = DateTime.Today.AddMonths(1);
    [ObservableProperty] private int venueIndex;
    [ObservableProperty] private string errorText = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try { _venues = await _owner.GetVenuesAsync(); }
        catch (Exception) { _venues = new(); }

        VenueNames.Clear();
        VenueNames.Add("Tất cả sân của tôi");
        foreach (var v in _venues) VenueNames.Add(v.Name);
        VenueIndex = 0;
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

        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorText = "Bạn chưa nhập mã.";
            return;
        }
        if (!TryNumber(PercentText, out var percent) || percent < 1 || percent > 100)
        {
            ErrorText = "Phần trăm giảm phải từ 1 đến 100.";
            return;
        }
        if (!TryNumber(MaxDiscountText, out var maxDiscount) ||
            !TryNumber(MinTotalText, out var minTotal) ||
            !TryNumber(UsageLimitText, out var usageLimit))
        {
            ErrorText = "Các ô số tiền và số lượt chỉ được nhập số.";
            return;
        }

        int? venueId = null;
        if (VenueIndex > 0 && VenueIndex <= _venues.Count)
            venueId = _venues[VenueIndex - 1].Id;

        var promo = new Promotion
        {
            Code = Code.Trim(),
            PercentOff = (int)percent,
            MaxDiscount = maxDiscount,
            MinTotal = minTotal,
            UsageLimit = (int)usageLimit,
            ValidFrom = ValidFrom.Date,
            ValidTo = ValidTo.Date,
            VenueId = venueId
        };

        var error = await _owner.CreatePromotionAsync(promo);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        await Shell.Current.GoToAsync("..");
    }
}