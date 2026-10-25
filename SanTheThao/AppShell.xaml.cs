using SanTheThao.Views;

namespace SanTheThao
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("VenueDetail", typeof(VenueDetailPage));
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
            Routing.RegisterRoute("BookSlot", typeof(BookingPage));
        }

        // Chủ sân và Admin thấy tab Quản lý, chỉ Admin thấy tab Quản trị
        public void ApplyRole(string? role)
        {
            OwnerTab.IsVisible = role is "Owner" or "Admin";
            AdminTab.IsVisible = role is "Admin";
        }
    }
}