using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class LoyaltyPage : ContentPage
{
    private readonly LoyaltyViewModel _vm;

    public LoyaltyPage(LoyaltyViewModel vm)
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