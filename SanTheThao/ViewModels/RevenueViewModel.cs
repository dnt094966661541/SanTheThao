using Android.Content;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;
using System.Collections.ObjectModel;

namespace SanTheThao.ViewModels;

public partial class RevenueViewModel : ObservableObject
{
    private readonly IOwnerService _owner;
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public RevenueViewModel(IOwnerService owner)
    {
        _owner = owner;
    }

    public ObservableCollection<RevenueDay> Days { get; } = new();

    [ObservableProperty] private string monthText = "";
    [ObservableProperty] private string totalText = "0 đ";
    [ObservableProperty] private string countText = "0 đơn";

    [RelayCommand]
    private async Task LoadAsync()
    {
        MonthText = $"Tháng {_month:MM/yyyy}";

        var result = await _owner.GetRevenueAsync(_month);

        Days.Clear();
        if (result == null)
        {
            TotalText = "0 đ";
            CountText = "0 đơn";
            return;
        }

        TotalText = $"{result.Total:N0} đ";
        CountText = $"{result.BookingCount} đơn";

        var max = result.PerDay.Count == 0 ? 0 : result.PerDay.Max(d => d.Total);
        foreach (var d in result.PerDay)
        {
            d.Ratio = max > 0 ? (double)(d.Total / max) : 0;
            Days.Add(d);
        }
    }

    [RelayCommand]
    private async Task PrevAsync()
    {
        _month = _month.AddMonths(-1);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        _month = _month.AddMonths(1);
        await LoadAsync();
    }
}