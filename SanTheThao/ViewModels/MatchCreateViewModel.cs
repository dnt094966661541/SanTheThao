using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public class MatchCreateViewModel : ObservableObject
{
    private readonly ICommunityService _svc;
    private readonly IVenueService _venues;
    private List<Venue> _all = new();
    private List<Venue> _shown = new();
    private bool _loaded;

    private string _sport = "Cầu lông";
    private int _venueIndex;
    private DateTime _date = DateTime.Today;
    private int _startIndex = 13;      // 18:00
    private int _endIndex = 15;        // 20:00
    private string _level = "Trung bình";
    private string _costText = "0";
    private string _note = "";
    private string _errorText = "";
    private double _players = 2;

    public MatchCreateViewModel(ICommunityService svc, IVenueService venues)
    {
        _svc = svc;
        _venues = venues;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    public List<string> Sports { get; } = new() { "Bóng đá", "Cầu lông", "Pickleball", "Tennis", "Bóng rổ" };
    public List<string> Levels { get; } = new() { "Mới chơi", "Trung bình", "Khá", "Giỏi" };
    public List<string> Hours { get; } = Enumerable.Range(5, 19).Select(h => $"{h:00}:00").ToList();   // 05:00 đến 23:00
    public ObservableCollection<string> VenueNames { get; } = new();

    public DateTime MinDate => DateTime.Today;
    public DateTime MaxDate => DateTime.Today.AddDays(60);

    public string Sport
    {
        get => _sport;
        set
        {
            if (SetProperty(ref _sport, value))
                RebuildVenues();
        }
    }

    public int VenueIndex
    {
        get => _venueIndex;
        set => SetProperty(ref _venueIndex, value);
    }

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public int StartIndex
    {
        get => _startIndex;
        set => SetProperty(ref _startIndex, value);
    }

    public int EndIndex
    {
        get => _endIndex;
        set => SetProperty(ref _endIndex, value);
    }

    public string Level
    {
        get => _level;
        set => SetProperty(ref _level, value);
    }

    public string CostText
    {
        get => _costText;
        set => SetProperty(ref _costText, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    public string ErrorText
    {
        get => _errorText;
        set => SetProperty(ref _errorText, value);
    }

    public double Players
    {
        get => _players;
        set
        {
            if (SetProperty(ref _players, value))
                OnPropertyChanged(nameof(PlayersLabel));
        }
    }

    public string PlayersLabel => $"Cần thêm {(int)Players} người";

    private async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try { _all = await _venues.GetVenuesAsync(); }
        catch (Exception) { _all = new(); }

        RebuildVenues();
    }

    private void RebuildVenues()
    {
        _shown = _all.Where(v => v.Sport == Sport).ToList();

        VenueNames.Clear();
        VenueNames.Add("Chưa chốt sân");
        foreach (var v in _shown) VenueNames.Add(v.Name);

        VenueIndex = 0;
    }

    private async Task SaveAsync()
    {
        ErrorText = "";

        var startHour = 5 + StartIndex;
        var endHour = 5 + EndIndex;
        if (endHour <= startHour)
        {
            ErrorText = "Giờ kết thúc phải sau giờ bắt đầu.";
            return;
        }

        var digits = new string(CostText.Where(char.IsDigit).ToArray());
        if (!decimal.TryParse(digits.Length == 0 ? "0" : digits, out var cost))
        {
            ErrorText = "Chi phí mỗi người chỉ được nhập số.";
            return;
        }

        int? venueId = null;
        if (VenueIndex > 0 && VenueIndex <= _shown.Count)
            venueId = _shown[VenueIndex - 1].Id;

        var error = await _svc.CreateMatchAsync(Sport, venueId, Date.Date, startHour, endHour,
            (int)Players, Level, cost, Note);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        await Shell.Current.DisplayAlertAsync("Đã đăng kèo", "Kèo của bạn đã hiện trong danh sách Ghép kèo.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}