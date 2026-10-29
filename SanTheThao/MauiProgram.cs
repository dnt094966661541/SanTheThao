
using Microsoft.Extensions.Logging;
using BarcodeScanning;

namespace SanTheThao
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseBarcodeScanning()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Home
            builder.Services.AddTransient<SanTheThao.ViewModels.HomeViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.HomePage>();

            // Venue detail
            builder.Services.AddTransient<SanTheThao.ViewModels.VenueDetailViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.VenueDetailPage>();

            // History
            builder.Services.AddTransient<SanTheThao.ViewModels.HistoryViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.HistoryPage>();

            // HTTP client
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(SanTheThao.Services.ApiConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            });

            // Services
            builder.Services.AddSingleton<
                SanTheThao.Services.IVenueService,
                SanTheThao.Services.ApiVenueService>();

            builder.Services.AddSingleton<
                SanTheThao.Services.IBookingService,
                SanTheThao.Services.ApiBookingService>();

            builder.Services.AddSingleton<
                SanTheThao.Services.IAuthService,
                SanTheThao.Services.AuthService>();

            // Login and profile
            builder.Services.AddTransient<SanTheThao.ViewModels.LoginViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.LoginPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.ProfileViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.ProfilePage>();

            // Owner
            builder.Services.AddSingleton<
                SanTheThao.Services.IOwnerService,
                SanTheThao.Services.OwnerService>();

            builder.Services.AddTransient<SanTheThao.ViewModels.OwnerHomeViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.OwnerHomePage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.VenueEditViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.VenueEditPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.OwnerBookingsViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.OwnerBookingsPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.RevenueViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.RevenuePage>();

            // Customer
            builder.Services.AddSingleton<
                SanTheThao.Services.ICustomerService,
                SanTheThao.Services.CustomerService>();

            builder.Services.AddTransient<SanTheThao.ViewModels.BookingQrViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.BookingQrPage>();

            // Promotions
            builder.Services.AddTransient<SanTheThao.ViewModels.OwnerPromosViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.OwnerPromosPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.PromoEditViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.PromoEditPage>();

            // Admin
            builder.Services.AddSingleton<
                SanTheThao.Services.IAdminService,
                SanTheThao.Services.AdminService>();

            builder.Services.AddTransient<SanTheThao.ViewModels.AdminViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.AdminPage>();

            // Change password
            builder.Services.AddTransient<SanTheThao.ViewModels.ChangePasswordViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.ChangePasswordPage>();

            // Scan QR check-in
            builder.Services.AddTransient<SanTheThao.Views.ScanCheckInPage>();

            builder.Services.AddTransient<SanTheThao.ViewModels.OwnerApplyViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.OwnerApplyPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.AdminApplicationsViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.AdminApplicationsPage>();

            builder.Services.AddTransient<SanTheThao.Views.BookingPage>();

            builder.Services.AddTransient<SanTheThao.ViewModels.OtpViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.OtpPage>();
            builder.Services.AddSingleton<SanTheThao.Services.ICommunityService, SanTheThao.Services.CommunityService>();

            builder.Services.AddTransient<SanTheThao.ViewModels.LoyaltyViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.LoyaltyPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.RecurringViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.RecurringPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.SplitViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.SplitPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.MatchesViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.MatchesPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.MatchDetailViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.MatchDetailPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.MatchCreateViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.MatchCreatePage>();

            builder.Services.AddSingleton<SanTheThao.Services.IReminderService, SanTheThao.AndroidReminderService>();
            builder.Services.AddSingleton<SanTheThao.Services.INotificationService, SanTheThao.Services.NotificationService>();
            builder.Services.AddTransient<SanTheThao.ViewModels.NotificationsViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.NotificationsPage>();
            builder.Services.AddTransient<SanTheThao.ViewModels.ChatViewModel>();
            builder.Services.AddTransient<SanTheThao.Views.ChatPage>();
            return builder.Build();
        }
    }
}
