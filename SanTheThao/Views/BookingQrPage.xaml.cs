using SanTheThao.ViewModels;

namespace SanTheThao.Views;

public partial class BookingQrPage : ContentPage
{
    public BookingQrPage(BookingQrViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}