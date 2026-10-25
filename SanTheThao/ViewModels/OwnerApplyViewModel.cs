using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SanTheThao.Services;
using static Android.Provider.ContactsContract.CommonDataKinds;

namespace SanTheThao.ViewModels;

public partial class OwnerApplyViewModel : ObservableObject
{
    private readonly ICustomerService _customer;

    public OwnerApplyViewModel(ICustomerService customer)
    {
        _customer = customer;
    }

    [ObservableProperty] private string businessName = "";
    [ObservableProperty] private string phone = "";
    [ObservableProperty] private string address = "";
    [ObservableProperty] private string note = "";
    [ObservableProperty] private string errorText = "";

    [RelayCommand]
    private async Task SubmitAsync()
    {
        ErrorText = "";

        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            ErrorText = "Bạn chưa nhập tên cơ sở kinh doanh.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Phone))
        {
            ErrorText = "Bạn chưa nhập số điện thoại.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Address))
        {
            ErrorText = "Bạn chưa nhập địa chỉ.";
            return;
        }

        var error = await _customer.SubmitOwnerApplicationAsync(BusinessName, Phone, Address, Note);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        await Shell.Current.DisplayAlertAsync("Đã gửi hồ sơ",
            "Quản trị viên sẽ xem và duyệt hồ sơ của bạn. Bạn theo dõi kết quả ở tab Tài khoản.", "OK");
        await Shell.Current.GoToAsync("..");
    }
}