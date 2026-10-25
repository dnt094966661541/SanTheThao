using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class OwnerPromosViewModel : ObservableObject
{
    private readonly IOwnerService _owner;

    public OwnerPromosViewModel(IOwnerService owner)
    {
        _owner = owner;
    }

    public ObservableCollection<Promotion> Promos { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _owner.GetPromotionsAsync();
            Promos.Clear();
            foreach (var p in list) Promos.Add(p);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không tải được danh sách mã giảm giá.", "OK");
        }
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync("PromoEdit");

    [RelayCommand]
    private async Task ToggleAsync(Promotion promo)
    {
        await _owner.TogglePromotionAsync(promo.Id);
        await LoadAsync();
    }
}