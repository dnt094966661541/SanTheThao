using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class ChangePasswordViewModel : ObservableObject
{
    private readonly IAuthService _auth;

    public ChangePasswordViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [ObservableProperty] private string currentPassword = "";
    [ObservableProperty] private string newPassword = "";
    [ObservableProperty] private string confirmPassword = "";
    [ObservableProperty] private string errorText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HidePassword))]
    [NotifyPropertyChangedFor(nameof(EyeText))]
    private bool showPassword;

    public bool HidePassword => !ShowPassword;
    public string EyeText => ShowPassword ? "🙈" : "👁";

    [RelayCommand]
    private void TogglePassword() => ShowPassword = !ShowPassword;

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorText = "";

        if (string.IsNullOrEmpty(CurrentPassword))
        {
            ErrorText = "Bạn hãy nhập mật khẩu hiện tại.";
            return;
        }
        if (NewPassword.Length < 6)
        {
            ErrorText = "Mật khẩu mới cần ít nhất 6 ký tự.";
            return;
        }
        if (NewPassword != ConfirmPassword)
        {
            ErrorText = "Hai ô mật khẩu mới chưa giống nhau.";
            return;
        }

        var error = await _auth.ChangePasswordAsync(CurrentPassword, NewPassword);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        CurrentPassword = "";
        NewPassword = "";
        ConfirmPassword = "";
        await Shell.Current.DisplayAlertAsync("Xong", "Bạn đã đổi mật khẩu.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}