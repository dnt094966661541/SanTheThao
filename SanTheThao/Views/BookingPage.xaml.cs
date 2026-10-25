using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class BookingPage : ContentPage
{
    private readonly VenueDetailViewModel _vm;

    public BookingPage(VenueDetailViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}