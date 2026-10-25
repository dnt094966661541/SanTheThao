using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage(ChangePasswordViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}