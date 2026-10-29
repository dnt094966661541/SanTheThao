using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class MatchesPage : ContentPage
{
    private readonly MatchesViewModel _vm;

    public MatchesPage(MatchesViewModel vm)
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