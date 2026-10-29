using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class RecurringViewModel : ObservableObject
{
    private readonly ICommunityService _svc;

    public RecurringViewModel(ICommunityService svc)
    {
        _svc = svc;
    }

    public ObservableCollection<RecurringItem> Items { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _svc.GetRecurringAsync();
            Items.Clear();
            foreach (var i in list) Items.Add(i);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không tải được lịch cố định.", "OK");
        }
    }

    [RelayCommand]
    private async Task CancelAsync(RecurringItem item)
    {
        var yes = await Shell.Current.DisplayAlertAsync("Hủy lịch cố định",
            $"Hủy {item.Upcoming} buổi sắp tới của {item.VenueName}? Các buổi đã chơi giữ nguyên.", "Hủy các buổi", "Không");
        if (!yes) return;

        var error = await _svc.CancelRecurringAsync(item.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa hủy được", error, "OK");
            return;
        }
        await LoadAsync();
    }
}