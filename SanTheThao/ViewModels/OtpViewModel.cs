using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IntelliJ.Lang.Annotations;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(Sent), "sent")]
public partial class OtpViewModel : ObservableObject
{
    private readonly IAuthService _auth;
    private IDispatcherTimer? _timer;

    public OtpViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    [ObservableProperty] private OtpSent? sent;
    [ObservableProperty] private string code = "";
    [ObservableProperty] private string errorText = "";
    [ObservableProperty] private string infoText = "";
    [ObservableProperty] private string devMessage = "";
    [ObservableProperty] private bool hasDevMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResend))]
    [NotifyPropertyChangedFor(nameof(ResendText))]
    private int secondsLeft;

    [ObservableProperty] private bool isBusy;

    public bool CanResend => SecondsLeft <= 0;
    public string ResendText => SecondsLeft > 0 ? $"Gửi lại mã sau {SecondsLeft} giây" : "Gửi lại mã";
    public string OtherChannelText => Sent?.Channel == "sms" ? "Nhận mã qua email" : "Nhận mã qua tin nhắn";

    partial void OnSentChanged(OtpSent? value)
    {
        if (value == null) return;

        var way = value.Channel == "sms" ? "tin nhắn" : "email";
        InfoText = $"Mã gồm 6 số đã được gửi bằng {way} đến {value.Destination}. " +
                   $"Mã có hiệu lực {Math.Max(1, value.ExpiresInSeconds / 60)} phút." +
                   (value.Channel == "email" ? " Nếu chưa thấy thư, bạn kiểm tra cả mục Thư rác." : "");

        DevMessage = value.DevMessage ?? "";
        HasDevMessage = !string.IsNullOrEmpty(DevMessage);
        OnPropertyChanged(nameof(OtherChannelText));

        StartCountdown(60);
    }

    private void StartCountdown(int seconds)
    {
        SecondsLeft = seconds;
        _timer?.Stop();

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer == null) return;

        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) =>
        {
            SecondsLeft--;
            if (SecondsLeft <= 0) _timer?.Stop();
        };
        _timer.Start();
    }

    public void Stop() => _timer?.Stop();

    [RelayCommand]
    private async Task VerifyAsync()
    {
        ErrorText = "";
        if (Sent == null) return;

        var digits = new string(Code.Where(char.IsDigit).ToArray());
        if (digits.Length != 6)
        {
            ErrorText = "Bạn hãy nhập mã gồm 6 số.";
            return;
        }

        IsBusy = true;
        var error = await _auth.VerifyOtpAsync(Sent.Identifier, digits);
        IsBusy = false;

        if (error != null)
        {
            ErrorText = error;
            return;
        }

        Stop();
        (Shell.Current as AppShell)?.ApplyRole(_auth.CurrentUser?.Role);
        await Shell.Current.GoToAsync("//HomePage");
    }

    [RelayCommand]
    private Task ResendAsync() => SendAsync(Sent?.Channel ?? "email");

    [RelayCommand]
    private Task SwitchChannelAsync() => SendAsync(Sent?.Channel == "sms" ? "email" : "sms");

    private async Task SendAsync(string channel)
    {
        ErrorText = "";
        if (Sent == null || SecondsLeft > 0) return;

        IsBusy = true;
        var (error, newSent) = await _auth.SendOtpAsync(Sent.Identifier, channel);
        IsBusy = false;

        if (error != null)
        {
            ErrorText = error;
            return;
        }

        Code = "";
        Sent = newSent;
    }
}