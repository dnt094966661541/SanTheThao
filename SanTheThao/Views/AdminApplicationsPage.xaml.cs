using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class AdminApplicationsPage : ContentPage
{
    private readonly AdminApplicationsViewModel _vm;

    public AdminApplicationsPage(AdminApplicationsViewModel vm)
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