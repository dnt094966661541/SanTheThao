using CommunityToolkit.Mvvm.ComponentModel;
using QRCoder;
using SanTheThao.Models;

namespace SanTheThao.ViewModels;

[QueryProperty(nameof(Booking), "booking")]
public partial class BookingQrViewModel : ObservableObject
{
    [ObservableProperty] private Booking? booking;
    [ObservableProperty] private ImageSource? qrImage;

    partial void OnBookingChanged(Booking? value)
    {
        if (value == null) return;

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode($"SANTHETHAO:{value.Id:N}", QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data).GetGraphic(20);

        QrImage = ImageSource.FromStream(() => new MemoryStream(png));
    }
}