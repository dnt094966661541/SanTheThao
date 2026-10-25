using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class OwnerPromosPage : ContentPage
{
    private readonly OwnerPromosViewModel _vm;

    public OwnerPromosPage(OwnerPromosViewModel vm)
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