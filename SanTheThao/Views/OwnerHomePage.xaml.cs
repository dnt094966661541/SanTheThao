using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class OwnerHomePage : ContentPage
{
    private readonly OwnerHomeViewModel _vm;

    public OwnerHomePage(OwnerHomeViewModel vm)
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