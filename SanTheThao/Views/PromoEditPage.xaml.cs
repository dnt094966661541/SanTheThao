using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class PromoEditPage : ContentPage
{
    private readonly PromoEditViewModel _vm;

    public PromoEditPage(PromoEditViewModel vm)
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