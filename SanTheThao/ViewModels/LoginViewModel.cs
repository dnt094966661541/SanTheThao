using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleText))]
    [NotifyPropertyChangedFor(nameof(SubmitText))]
    [NotifyPropertyChangedFor(nameof(SwitchText))]
    private bool isRegister;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private string fullName = "";

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HidePassword))]
    [NotifyPropertyChangedFor(nameof(EyeText))]
    private bool showPassword;

    [ObservableProperty]
    private string errorText = "";

    public bool IsNotBusy => !IsBusy;
    public bool HidePassword => !ShowPassword;
    public string EyeText => ShowPassword ? "🙈" : "👁";

    public string TitleText => IsRegister ? "Tạo tài khoản mới" : "Đăng nhập để tiếp tục";

    public string SubmitText =>
        IsRegister ? "Đăng ký" : "Đăng nhập";

    public string SwitchText => IsRegister ? "Đã có tài khoản? Đăng nhập" : "Đăng ký tài khoản";

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegister = !IsRegister;
        ErrorText = "";
    }

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorText = "";

        if (string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            ErrorText = "Bạn hãy nhập email và mật khẩu.";
            return;
        }

        if (IsRegister && string.IsNullOrWhiteSpace(FullName))
        {
            ErrorText = "Bạn hãy nhập họ tên.";
            return;
        }

        IsBusy = true;

        try
        {
            var error = IsRegister
                ? await _auth.RegisterAsync(FullName, Email, Password)
                : await _auth.LoginAsync(Email, Password);

            if (error != null)
            {
                ErrorText = error;
                return;
            }

            Password = "";

            // Áp dụng quyền tài khoản sau khi đăng nhập thành công.
            (Shell.Current as AppShell)?
                .ApplyRole(_auth.CurrentUser?.Role);

            await Shell.Current.GoToAsync("//HomePage");
        }
        catch (Exception)
        {
            ErrorText = "Đã xảy ra lỗi. Vui lòng thử lại.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Khôi phục phiên đăng nhập khi mở ứng dụng.
    public async Task TryAutoLoginAsync()
    {
        try
        {
            if (await _auth.TryRestoreAsync())
            {
                // Áp dụng quyền trước khi chuyển đến trang chủ.
                (Shell.Current as AppShell)?
                    .ApplyRole(_auth.CurrentUser?.Role);

                await Shell.Current.GoToAsync("//HomePage");
            }
        }
        catch (Exception)
        {
            // Nếu khôi phục phiên thất bại, người dùng ở lại màn hình đăng nhập.
        }
    }
}