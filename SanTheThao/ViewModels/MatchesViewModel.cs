using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class MatchesViewModel : ObservableObject
{
    private readonly ICommunityService _svc;

    public MatchesViewModel(ICommunityService svc)
    {
        _svc = svc;
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

    public ObservableCollection<MatchItem> Matches { get; } = new();

    [ObservableProperty] private string selectedSport = "Tất cả";
    [ObservableProperty] private bool onlyMine;
    [ObservableProperty] private bool isRefreshing;

    partial void OnOnlyMineChanged(bool value) => _ = LoadAsync();

    partial void OnSelectedSportChanged(string value)
    {
        foreach (var c in Chips) c.IsSelected = c.Name == value;
        _ = LoadAsync();
    }

    [RelayCommand]
    private void SelectSport(string sport) => SelectedSport = sport;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _svc.GetMatchesAsync(SelectedSport, OnlyMine);
            Matches.Clear();
            foreach (var m in list) Matches.Add(m);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không tải được danh sách kèo. Bạn hãy kiểm tra API đã chạy chưa.", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private Task CreateAsync() => Shell.Current.GoToAsync("MatchCreate");

    [RelayCommand]
    private Task OpenAsync(MatchItem match) => Shell.Current.GoToAsync($"MatchDetail?id={match.Id}");
}