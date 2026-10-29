using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class RecurringPage : ContentPage
{
    private readonly RecurringViewModel _vm;

    public RecurringPage(RecurringViewModel vm)
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