using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class OwnerApplyPage : ContentPage
{
    public OwnerApplyPage(OwnerApplyViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}