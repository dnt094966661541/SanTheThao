using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

public partial class TimeSlot : ObservableObject
{
    public int Hour { get; init; }
    public decimal Price { get; init; }

    public string Text => $"{Hour:00}:00 - {Hour + 1:00}:00";
    public string PriceText => $"{Price:N0} đ";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(SubTextColor))]
    private bool isBooked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    [NotifyPropertyChangedFor(nameof(SubTextColor))]
    private bool isSelected;

    public Color BackColor =>
        IsBooked ? Color.FromArgb("#F0F1F0") :
        IsSelected ? Color.FromArgb("#1B8A3A") :
        Color.FromArgb("#FFFFFF");

    public Color TextColor =>
        IsBooked ? Color.FromArgb("#9CA3AF") :
        IsSelected ? Color.FromArgb("#FFFFFF") :
        Color.FromArgb("#222222");

    public Color SubTextColor =>
        IsBooked ? Color.FromArgb("#9CA3AF") :
        IsSelected ? Color.FromArgb("#DDF2E3") :
        Color.FromArgb("#6B7280");
}