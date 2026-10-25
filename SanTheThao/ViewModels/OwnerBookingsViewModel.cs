using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Models;
using SanTheThao.Services;

namespace SanTheThao.ViewModels;

public partial class OwnerBookingsViewModel : ObservableObject
{
    private readonly IOwnerService _owner;
    private readonly IBookingService _bookings;

    public OwnerBookingsViewModel(IOwnerService owner, IBookingService bookings)
    {
        _owner = owner;
        _bookings = bookings;
    }

    public ObservableCollection<Booking> Bookings { get; } = new();

    [RelayCommand]
    private async Task LoadAsync()
    {
        var list = await _owner.GetBookingsAsync();
        Bookings.Clear();
        foreach (var b in list) Bookings.Add(b);
    }

    [RelayCommand]
    private async Task CancelAsync(Booking booking)
    {
        if (!booking.CanCancel) return;

        var yes = await Shell.Current.DisplayAlertAsync("Hủy đơn",
            $"Hủy đơn {booking.Code} của {booking.CustomerName}?", "Hủy đơn", "Không");
        if (!yes) return;

        await _bookings.CancelAsync(booking.Id);
        await LoadAsync();
    }
}