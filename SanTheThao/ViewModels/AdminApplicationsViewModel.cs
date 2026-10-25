using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class AdminApplicationsViewModel : ObservableObject
{
    private readonly IAdminService _admin;

    public AdminApplicationsViewModel(IAdminService admin)
    {
        _admin = admin;
    }

    public ObservableCollection<OwnerApplication> Applications { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var list = await _admin.GetApplicationsAsync();
            Applications.Clear();
            foreach (var a in list) Applications.Add(a);
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối",
                "Không tải được danh sách hồ sơ.", "OK");
        }
    }

    [RelayCommand]
    private async Task ApproveAsync(OwnerApplication app)
    {
        var yes = await Shell.Current.DisplayAlertAsync("Duyệt hồ sơ",
            $"Cho {app.UserName} làm chủ sân?", "Duyệt", "Không");
        if (!yes) return;

        var error = await _admin.ApproveApplicationAsync(app.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RejectAsync(OwnerApplication app)
    {
        var note = await Shell.Current.DisplayPromptAsync(
            "Từ chối hồ sơ", "Nhập lý do (khách sẽ thấy lý do này)", "Từ chối", "Hủy", maxLength: 200);
        if (string.IsNullOrWhiteSpace(note)) return;

        var error = await _admin.RejectApplicationAsync(app.Id, note);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }
}