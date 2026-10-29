using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(MatchId), "id")]
public partial class MatchDetailViewModel : ObservableObject
{
    private readonly ICommunityService _svc;

    public MatchDetailViewModel(ICommunityService svc)
    {
        _svc = svc;
    }

    public string MatchId { get; set; } = "";
    public ObservableCollection<MatchJoinItem> Joins { get; } = new();

    [ObservableProperty] private MatchItem? match;
    [ObservableProperty] private string hostPhone = "";
    [ObservableProperty] private bool hasHostPhone;
    [ObservableProperty] private bool isHost;
    [ObservableProperty] private bool canJoin;
    [ObservableProperty] private bool canLeave;
    [ObservableProperty] private bool canClose;
    [ObservableProperty] private bool hasJoins;

    // Chỗ 1: Thêm thuộc tính điều kiện hiển thị chat và chú thích
    [ObservableProperty] private bool canChat;
    [ObservableProperty] private bool showChatHint;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (!Guid.TryParse(MatchId, out var id)) return;

        try
        {
            var detail = await _svc.GetMatchAsync(id);
            if (detail == null) return;

            Match = detail.Match;
            IsHost = detail.Match.IsMine;
            CanJoin = !IsHost && detail.Match.MyJoinStatus == null && detail.Match.Status == "Đang mở";
            CanLeave = !IsHost && detail.Match.MyJoinStatus != null;
            CanClose = IsHost && detail.Match.Status != "Đã đóng";

            // Chỗ 2: Cập nhật điều kiện bật nút chat và hiển thị thông báo chờ duyệt
            CanChat = IsHost || detail.Match.MyJoinStatus == "Được nhận";
            ShowChatHint = !IsHost && detail.Match.MyJoinStatus == "Chờ duyệt";

            HostPhone = detail.HostPhone ?? "";
            HasHostPhone = !string.IsNullOrEmpty(HostPhone);

            Joins.Clear();
            foreach (var j in detail.Joins) Joins.Add(j);
            HasJoins = Joins.Count > 0;
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không tải được chi tiết kèo.", "OK");
        }
    }

    [RelayCommand]
    private async Task JoinAsync()
    {
        if (Match == null) return;

        var msg = await Shell.Current.DisplayPromptAsync("Xin tham gia",
            "Lời nhắn cho chủ kèo (không bắt buộc), ví dụ trình độ của bạn:", "Gửi", "Hủy", maxLength: 200);
        if (msg == null) return;

        var error = await _svc.JoinMatchAsync(Match.Id, msg);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa gửi được", error, "OK");
            return;
        }
        await Shell.Current.DisplayAlertAsync("Đã gửi", "Chủ kèo sẽ xem và duyệt. Bạn theo dõi ở mục Ghép kèo, bật công tắc Của tôi.", "OK");
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LeaveAsync()
    {
        if (Match == null) return;
        var yes = await Shell.Current.DisplayAlertAsync("Rút khỏi kèo", "Bạn muốn rút khỏi kèo này?", "Rút", "Không");
        if (!yes) return;

        var error = await _svc.LeaveMatchAsync(Match.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ApproveAsync(MatchJoinItem join) => await ReviewAsync(join, true);

    [RelayCommand]
    private async Task RejectAsync(MatchJoinItem join) => await ReviewAsync(join, false);

    private async Task ReviewAsync(MatchJoinItem join, bool approve)
    {
        if (Match == null) return;
        var error = await _svc.ReviewJoinAsync(Match.Id, join.Id, approve);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        if (Match == null) return;
        var yes = await Shell.Current.DisplayAlertAsync("Đóng kèo", "Đóng kèo này? Người khác sẽ không thấy nữa.", "Đóng", "Không");
        if (!yes) return;

        var error = await _svc.CloseMatchAsync(Match.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CallAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        try { await Launcher.Default.OpenAsync(new Uri($"tel:{phone}")); }
        catch (Exception) { await Shell.Current.DisplayAlertAsync("Không gọi được", phone, "OK"); }
    }

    [RelayCommand]
    private async Task ZaloAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return;
        try { await Launcher.Default.OpenAsync(new Uri($"https://zalo.me/{phone}")); }
        catch (Exception) { await Shell.Current.DisplayAlertAsync("Không mở được Zalo", phone, "OK"); }
    }

    // Chỗ 3: Thêm lệnh điều hướng mở trang trò chuyện của kèo
    [RelayCommand]
    private Task OpenChatAsync() =>
        Match == null ? Task.CompletedTask : Shell.Current.GoToAsync($"MatchChat?id={Match.Id}");
}