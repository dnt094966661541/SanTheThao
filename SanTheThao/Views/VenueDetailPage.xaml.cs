using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class VenueDetailPage : ContentPage
{
    private readonly VenueDetailViewModel _vm;

    public VenueDetailPage(VenueDetailViewModel vm)
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