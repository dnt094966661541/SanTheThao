using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    private readonly INotificationService _svc;

    public NotificationsViewModel(INotificationService svc)
    {
        _svc = svc;
    }

    public ObservableCollection<NotificationItem> Items { get; } = new();

    [ObservableProperty] private bool isRefreshing;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _svc.GetAllAsync();
            Items.Clear();
            foreach (var n in list) Items.Add(n);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không tải được thông báo.", "OK");
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task OpenAsync(NotificationItem item)
    {
        if (!item.IsRead) await _svc.MarkReadAsync(item.Id);

        if (string.IsNullOrWhiteSpace(item.Route))
        {
            await LoadAsync();
            return;
        }

        try
        {
            await Shell.Current.GoToAsync(item.Route);
        }
        catch (Exception)
        {
            await LoadAsync();   // trang đích không mở được (ví dụ không đủ quyền) thì chỉ đánh dấu đã đọc
        }
    }

    [RelayCommand]
    private async Task MarkAllAsync()
    {
        await _svc.MarkAllReadAsync();
        await LoadAsync();
    }
}