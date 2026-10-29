using Android.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;
using System.Collections.ObjectModel;

namespace SanTheThao.ViewModels;

public partial class LoyaltyViewModel : ObservableObject
{
    private readonly ICommunityService _svc;

    public LoyaltyViewModel(ICommunityService svc)
    {
        _svc = svc;
    }

    public ObservableCollection<RewardItem> Rewards { get; } = new();
    public ObservableCollection<PointTx> History { get; } = new();

    [ObservableProperty] private string balanceText = "0";
    [ObservableProperty] private string tierText = "";
    [ObservableProperty] private string nextText = "";
    [ObservableProperty] private string perkText = "";
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool hasHistory;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var info = await _svc.GetLoyaltyAsync();
            if (info == null) return;

            BalanceText = info.Balance.ToString("N0");
            TierText = $"Hạng {info.Tier}";
            Progress = info.Progress;
            NextText = info.NextTier == null
                ? "Bạn đang ở hạng cao nhất 🎉"
                : $"Còn {info.NextAt - info.Lifetime} điểm nữa để lên hạng {info.NextTier}";
            PerkText = info.Multiplier > 1
                ? $"Ưu đãi hạng {info.Tier}: mỗi đơn được cộng thêm {(int)Math.Round((info.Multiplier - 1) * 100)}% điểm."
                : "Cứ 10.000 đ của một đơn đã chơi xong được 1 điểm. Lên hạng Bạc được cộng thêm 10% điểm.";

            Rewards.Clear();
            foreach (var r in info.Rewards)
            {
                r.CanAfford = info.Balance >= r.Cost;
                Rewards.Add(r);
            }

            History.Clear();
            foreach (var h in info.History) History.Add(h);
            HasHistory = History.Count > 0;
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không tải được điểm thưởng.", "OK");
        }
    }

    [RelayCommand]
    private async Task RedeemAsync(RewardItem reward)
    {
        var yes = await Shell.Current.DisplayAlertAsync("Đổi quà",
            $"Dùng {reward.Cost} điểm để đổi: {reward.Title}?", "Đổi", "Không");
        if (!yes) return;

        var (error, result) = await _svc.RedeemAsync(reward.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa đổi được", error, "OK");
            return;
        }

        await Clipboard.Default.SetTextAsync(result!.Code);
        await Shell.Current.DisplayAlertAsync("Đổi thành công",
            $"Mã của bạn: {result.Code}\n{result.Description}\nDùng được một lần, hạn đến {result.ValidTo:dd/MM/yyyy}.\n" +
            "Mã đã được sao chép, bạn dán vào ô Mã giảm giá khi đặt sân.", "OK");
        await LoadAsync();
    }
}