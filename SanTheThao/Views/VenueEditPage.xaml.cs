using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class VenueEditPage : ContentPage
{
    private readonly VenueEditViewModel _vm;

    public VenueEditPage(VenueEditViewModel vm)
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