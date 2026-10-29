using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    public List<string> ChannelOptions { get; } = new() { "Email", "Tin nhắn (SMS)" };

    // 0 = đăng nhập bằng mật khẩu, 1 = đăng nhập bằng mã OTP, 2 = đăng ký
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordMode))]
    [NotifyPropertyChangedFor(nameof(IsOtpMode))]
    [NotifyPropertyChangedFor(nameof(IsRegisterMode))]
    [NotifyPropertyChangedFor(nameof(ShowTabs))]
    [NotifyPropertyChangedFor(nameof(ShowNameField))]
    [NotifyPropertyChangedFor(nameof(ShowEmailField))]
    [NotifyPropertyChangedFor(nameof(ShowPhoneField))]
    [NotifyPropertyChangedFor(nameof(ShowPasswordField))]
    [NotifyPropertyChangedFor(nameof(ShowChannel))]
    [NotifyPropertyChangedFor(nameof(TitleText))]
    [NotifyPropertyChangedFor(nameof(SubmitText))]
    [NotifyPropertyChangedFor(nameof(SwitchText))]
    private int mode;

    public bool IsPasswordMode => Mode == 0;
    public bool IsOtpMode => Mode == 1;
    public bool IsRegisterMode => Mode == 2;
    public bool ShowTabs => Mode != 2;
    public bool ShowNameField => Mode == 2;
    public bool ShowEmailField => Mode != 1;
    public bool ShowPhoneField => Mode != 0;
    public bool ShowPasswordField => Mode != 1;
    public bool ShowChannel => Mode != 0;

    public string TitleText => Mode switch
    {
        2 => "Tạo tài khoản mới",
        1 => "Đăng nhập bằng mã OTP",
        _ => "Đăng nhập để tiếp tục"
    };

    public string SubmitText => Mode switch
    {
        2 => "Đăng ký",
        1 => "Gửi mã OTP",
        _ => "Đăng nhập"
    };

    public string SwitchText => Mode == 2 ? "Đã có tài khoản? Đăng nhập" : "Đăng ký tài khoản";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HidePassword))]
    [NotifyPropertyChangedFor(nameof(EyeText))]
    private bool showPassword;

    public bool IsNotBusy => !IsBusy;
    public bool HidePassword => !ShowPassword;
    public string EyeText => ShowPassword ? "🙈" : "👁";

    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private int channelIndex;      // 0 = Email, 1 = Tin nhắn
    [ObservableProperty] private string errorText = "";

    private string Channel => ChannelIndex == 1 ? "sms" : "email";

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private void SelectMode(string value)
    {
        Mode = int.Parse(value);
        ErrorText = "";
    }

    [RelayCommand]
    private void ToggleMode()
    {
        Mode = Mode == 2 ? 0 : 2;
        ErrorText = "";
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorText = "";
        IsBusy = true;
        try
        {
            switch (Mode)
            {
                case 0: await LoginWithPasswordAsync(); break;
                case 1: await LoginWithOtpAsync(); break;
                default: await RegisterAsync(); break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoginWithPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorText = "Bạn hãy nhập email và mật khẩu.";
            return;
        }

        var error = await _auth.LoginAsync(Email.Trim(), Password);
        if (error == null)
        {
            Password = "";
            await EnterAppAsync();
            return;
        }

        // Tài khoản chưa xác minh: gửi mã qua email rồi chuyển sang màn nhập mã
        if (error.Contains(AuthService.UnverifiedMarker, StringComparison.OrdinalIgnoreCase))
        {
            var (sendError, sent) = await _auth.SendOtpAsync(Email.Trim(), "email");
            if (sendError != null)
            {
                ErrorText = sendError;
                return;
            }
            await OpenOtpAsync(sent!);
            return;
        }

        ErrorText = error;
    }

    private async Task LoginWithOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorText = "Bạn hãy nhập số điện thoại.";
            return;
        }

        var (error, sent) = await _auth.SendOtpAsync(Phone.Trim(), Channel);
        if (error != null)
        {
            ErrorText = error;
            return;
        }
        await OpenOtpAsync(sent!);
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorText = "Bạn hãy nhập họ tên.";
            return;
        }
        if (!Email.Contains('@'))
        {
            ErrorText = "Email chưa đúng.";
            return;
        }
        if (Phone.Count(char.IsDigit) < 9)
        {
            ErrorText = "Số điện thoại chưa đúng.";
            return;
        }
        if (Password.Length < 6)
        {
            ErrorText = "Mật khẩu cần ít nhất 6 ký tự.";
            return;
        }

        var (error, sent) = await _auth.RegisterAsync(
            FullName.Trim(), Email.Trim(), Phone.Trim(), Password, Channel);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        Password = "";
        await OpenOtpAsync(sent!);
    }

    private Task OpenOtpAsync(OtpSent sent) =>
        Shell.Current.GoToAsync("Otp", new Dictionary<string, object> { ["sent"] = sent });

    private async Task EnterAppAsync()
    {
        (Shell.Current as AppShell)?.ApplyRole(_auth.CurrentUser?.Role);
        await Shell.Current.GoToAsync("//HomePage");
    }

    // Gọi khi mở màn hình: nếu còn phiên đăng nhập cũ thì vào thẳng Trang chủ
    public async Task TryAutoLoginAsync()
    {
        if (await _auth.TryRestoreAsync())
            await EnterAppAsync();
    }
}