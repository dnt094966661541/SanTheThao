using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class MatchCreatePage : ContentPage
{
    private readonly MatchCreateViewModel _vm;

    public MatchCreatePage(MatchCreateViewModel vm)
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