using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class OtpPage : ContentPage
{
    private readonly OtpViewModel _vm;

    public OtpPage(OtpViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Stop();
    }
}