using SanTheThao.Views;
using Microsoft.Extensions.DependencyInjection;
using SanTheThao.Services;

namespace SanTheThao
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("VenueDetail", typeof(VenueDetailPage));
            Routing.RegisterRoute("BookSlot", typeof(BookingPage));
            Routing.RegisterRoute("VenueEdit", typeof(VenueEditPage));
            Routing.RegisterRoute("OwnerBookings", typeof(OwnerBookingsPage));
            Routing.RegisterRoute("Revenue", typeof(RevenuePage));
            Routing.RegisterRoute("BookingQr", typeof(BookingQrPage));
            Routing.RegisterRoute("OwnerPromos", typeof(OwnerPromosPage));
            Routing.RegisterRoute("PromoEdit", typeof(PromoEditPage));
            Routing.RegisterRoute("ChangePassword", typeof(ChangePasswordPage));
            Routing.RegisterRoute("ScanCheckIn", typeof(ScanCheckInPage));
            Routing.RegisterRoute("OwnerApply", typeof(OwnerApplyPage));
            Routing.RegisterRoute("AdminApplications", typeof(AdminApplicationsPage));
            Routing.RegisterRoute("Otp", typeof(OtpPage));

            Routing.RegisterRoute("Loyalty", typeof(LoyaltyPage));
            Routing.RegisterRoute("Recurring", typeof(RecurringPage));
            Routing.RegisterRoute("Split", typeof(SplitPage));
            Routing.RegisterRoute("MatchDetail", typeof(MatchDetailPage));
            Routing.RegisterRoute("MatchCreate", typeof(MatchCreatePage));
            Routing.RegisterRoute("Notifications", typeof(NotificationsPage));
            Routing.RegisterRoute("MatchChat", typeof(ChatPage));
        }

        // Chủ sân thấy tab Quản lý, quản trị viên thấy tab Quản trị
        // (mỗi vai trò chỉ có tối đa 5 tab để vừa thanh tab của Android)
        public void ApplyRole(string? role)
        {
            OwnerTab.IsVisible = role == "Owner";
            AdminTab.IsVisible = role == "Admin";

            var notifications = Handler?.MauiContext?.Services.GetService<INotificationService>();
            if (role == null) notifications?.StopPolling();
            else notifications?.StartPolling();
        }
    }
}