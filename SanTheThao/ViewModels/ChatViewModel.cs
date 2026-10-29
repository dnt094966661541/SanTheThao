using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(MatchId), "id")]
public partial class ChatViewModel : ObservableObject
{
    // Dấu hiệu máy chủ báo mình không còn trong nhóm
    private const string ForbiddenMarker = "Chỉ chủ kèo";

    private readonly ICommunityService _svc;
    private IDispatcherTimer? _timer;
    private Guid _id;
    private int _lastId;
    private bool _busy;

    public ChatViewModel(ICommunityService svc)
    {
        _svc = svc;
    }

    public string MatchId { get; set; } = "";
    public ObservableCollection<ChatMessage> Messages { get; } = new();

    // Trang lắng nghe sự kiện này để cuộn xuống tin mới nhất
    public event Action? ScrollToEnd;

    [ObservableProperty] private string titleText = "Trò chuyện";
    [ObservableProperty] private string infoText = "";
    [ObservableProperty] private string draft = "";
    [ObservableProperty] private string errorText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CannotSend))]
    private bool canSend = true;

    public bool CannotSend => !CanSend;

    public async Task StartAsync()
    {
        if (!Guid.TryParse(MatchId, out _id)) return;

        _lastId = 0;
        Messages.Clear();
        await RefreshAsync();
        StartTimer();
    }

    public void Stop() => _timer?.Stop();

    private void StartTimer()
    {
        _timer?.Stop();

        var timer = Application.Current?.Dispatcher.CreateTimer();
        if (timer == null) return;

        timer.Interval = TimeSpan.FromSeconds(3);
        timer.Tick += async (_, _) => await RefreshAsync();
        timer.Start();
        _timer = timer;
    }

    private async Task RefreshAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var (error, state) = await _svc.GetChatAsync(_id, _lastId);

            if (error != null)
            {
                if (error.Contains(ForbiddenMarker, StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    await Shell.Current.DisplayAlertAsync("Không vào được", error, "OK");
                    await Shell.Current.GoToAsync("..");
                }
                return;   // lỗi mạng thoáng qua thì lần sau thử lại
            }
            if (state == null) return;

            TitleText = state.Title;
            InfoText = $"{state.ParticipantCount} người trong nhóm";
            CanSend = state.CanSend;

            var added = false;
            foreach (var m in state.Messages)
            {
                if (m.Id <= _lastId || Messages.Any(x => x.Id == m.Id)) continue;
                Messages.Add(m);
                _lastId = Math.Max(_lastId, m.Id);
                added = true;
            }
            if (added) ScrollToEnd?.Invoke();
        }
        finally
        {
            _busy = false;
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = Draft.Trim();
        if (text == "" || !CanSend) return;

        ErrorText = "";
        Draft = "";   // xóa ô nhập ngay để bấm gửi cảm giác nhanh

        var (error, msg) = await _svc.SendChatAsync(_id, text);
        if (error != null)
        {
            ErrorText = error;
            Draft = text;   // trả lại nội dung để bạn gửi lại
            return;
        }

        if (msg != null && !Messages.Any(x => x.Id == msg.Id))
        {
            Messages.Add(msg);
            _lastId = Math.Max(_lastId, msg.Id);
            ScrollToEnd?.Invoke();
        }
    }
}