using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class MatchDetailPage : ContentPage
{
    private readonly MatchDetailViewModel _vm;

    public MatchDetailPage(MatchDetailViewModel vm)
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