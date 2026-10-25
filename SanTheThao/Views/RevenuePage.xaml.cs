using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class RevenuePage : ContentPage
{
    private readonly RevenueViewModel _vm;

    public RevenuePage(RevenueViewModel vm)
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