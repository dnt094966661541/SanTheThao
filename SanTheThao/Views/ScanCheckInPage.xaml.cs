using BarcodeScanning;
using SanTheThao.Services;

namespace SanTheThao.Views;

public partial class ScanCheckInPage : ContentPage
{
    private readonly IOwnerService _owner;
    private bool _busy;

    public ScanCheckInPage(IOwnerService owner)
    {
        InitializeComponent();
        _owner = owner;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlertAsync("Cần quyền camera",
                "Bạn hãy cho phép app dùng camera để quét mã QR.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        Camera.CameraEnabled = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Camera.CameraEnabled = false;
    }

    // Camera nhận ra mã thì hàm này chạy liên tục, nên dùng _busy để mỗi lần chỉ xử lý một mã
    private void Camera_OnDetectionFinished(object? sender, OnDetectionFinishedEventArg e)
    {
        var value = e.BarcodeResults.FirstOrDefault()?.DisplayValue;
        if (string.IsNullOrWhiteSpace(value) || _busy) return;

        _busy = true;
        MainThread.BeginInvokeOnMainThread(async () => await HandleAsync(value));
    }

    private async Task HandleAsync(string value)
    {
        try
        {
            var (ok, message) = await _owner.CheckInAsync(value);
            await DisplayAlertAsync(
                ok ? "✅ Check-in thành công" : "Không check-in được", message, "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Lỗi", "Không kết nối được máy chủ.", "OK");
        }
        finally
        {
            _busy = false;
        }
    }

    private async void Done_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}