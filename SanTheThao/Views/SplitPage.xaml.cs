using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class SplitPage : ContentPage
{
    public SplitPage(SplitViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}