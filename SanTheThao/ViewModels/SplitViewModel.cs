using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;
using System.Collections.ObjectModel;
using static Android.Provider.Contacts;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(Booking), "booking")]
public partial class SplitViewModel : ObservableObject
{
    private readonly ICommunityService _svc;
    private SplitInfo? _split;

    public SplitViewModel(ICommunityService svc)
    {
        _svc = svc;
    }

    public ObservableCollection<SplitMemberItem> Members { get; } = new();

    [ObservableProperty] private Booking? booking;
    [ObservableProperty] private bool hasSplit;
    [ObservableProperty] private bool noSplit = true;
    [ObservableProperty] private bool canDelete;
    [ObservableProperty] private string totalText = "";
    [ObservableProperty] private string paidText = "";
    [ObservableProperty] private string peopleText = "";
    [ObservableProperty] private string namesText = "";
    [ObservableProperty] private string noteText = "";
    [ObservableProperty] private string errorText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeopleLabel))]
    private double people = 4;

    public string PeopleLabel => $"{(int)People} người chơi (kể cả bạn)";

    partial void OnBookingChanged(Booking? value)
    {
        if (value != null) _ = LoadSplitAsync();
    }

    private async Task LoadSplitAsync()
    {
        if (Booking == null) return;
        TotalText = $"{Booking.TotalPrice:N0} đ";
        try
        {
            Apply(await _svc.GetSplitAsync(Booking.Id));
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlertAsync("Lỗi kết nối", "Không tải được cách chia tiền.", "OK");
        }
    }

    private void Apply(SplitInfo? s)
    {
        _split = s;
        HasSplit = s != null;
        NoSplit = s == null;

        Members.Clear();
        if (s == null) return;

        foreach (var m in s.Members) Members.Add(m);
        CanDelete = s.CanDelete;
        PaidText = $"Đã thu {s.PaidTotal:N0} / {s.Total:N0} đ";
        NoteText = s.Note;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        ErrorText = "";
        if (Booking == null) return;

        var names = NamesText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim()).Where(n => n != "").ToList();

        var (error, split) = await _svc.CreateSplitAsync(Booking.Id, (int)People, names, NoteText);
        if (error != null)
        {
            ErrorText = error;
            return;
        }
        Apply(split);
    }

    [RelayCommand]
    private async Task TogglePaidAsync(SplitMemberItem member)
    {
        var (error, split) = await _svc.TogglePaidAsync(member.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa làm được", error, "OK");
            return;
        }
        Apply(split);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Booking == null) return;
        var yes = await Shell.Current.DisplayAlertAsync("Xóa cách chia",
            "Xóa cách chia tiền này để chia lại?", "Xóa", "Không");
        if (!yes) return;

        var error = await _svc.DeleteSplitAsync(Booking.Id);
        if (error != null)
        {
            await Shell.Current.DisplayAlertAsync("Chưa xóa được", error, "OK");
            return;
        }
        NamesText = "";
        Apply(null);
    }

    // Mở bảng chia sẻ của điện thoại (Zalo, Messenger, tin nhắn...)
    [RelayCommand]
    private async Task ShareAsync()
    {
        if (_split == null || Booking == null) return;

        var lines = new List<string>
        {
            $"💰 Chia tiền sân {Booking.VenueName}",
            $"📅 {Booking.Date:dd/MM/yyyy} · {Booking.HoursText}",
            $"Tổng: {_split.Total:N0} đ",
            ""
        };
        foreach (var m in _split.Members)
            lines.Add($"- {m.Name}: {m.AmountText} {(m.Paid ? "✅" : "")}".TrimEnd());
        if (!string.IsNullOrWhiteSpace(_split.Note))
        {
            lines.Add("");
            lines.Add(_split.Note);
        }

        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Chia tiền sân",
            Text = string.Join("\n", lines)
        });
    }
}