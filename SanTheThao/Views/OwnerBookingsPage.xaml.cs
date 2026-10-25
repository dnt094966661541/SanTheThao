using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class OwnerBookingsPage : ContentPage
{
    private readonly OwnerBookingsViewModel _vm;

    public OwnerBookingsPage(OwnerBookingsViewModel vm)
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