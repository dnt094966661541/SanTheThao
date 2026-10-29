
using Microsoft.EntityFrameworkCore;
using SanTheThao.Api.Models;
using static SanTheThao.Api.Models.Promotion;

namespace SanTheThao.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<OwnerApplication> OwnerApplications => Set<OwnerApplication>();
    public DbSet<VenueImage> VenueImages => Set<VenueImage>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<RecurringBooking> RecurringBookings => Set<RecurringBooking>();
    public DbSet<CostSplit> CostSplits => Set<CostSplit>();
    public DbSet<SplitMember> SplitMembers => Set<SplitMember>();
    public DbSet<MatchPost> MatchPosts => Set<MatchPost>();
    public DbSet<MatchJoin> MatchJoins => Set<MatchJoin>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MatchMessage> MatchMessages => Set<MatchMessage>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Venue>(e =>
        {
            e.Property(v => v.PricePerHour).HasPrecision(18, 0);
            e.Property(v => v.PeakPricePerHour).HasPrecision(18, 0);
            e.Property(v => v.WeekendPricePerHour).HasPrecision(18, 0);
        });

        mb.Entity<Booking>()
            .Property(b => b.TotalPrice)
            .HasPrecision(18, 0);

        mb.Entity<AppUser>()
            .Property(u => u.Email)
            .HasMaxLength(256);

        mb.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Một sân, một ngày, một giờ chỉ được có một đơn.
        mb.Entity<BookingSlot>()
            .HasIndex(s => new { s.VenueId, s.Date, s.Hour })
            .IsUnique();

        mb.Entity<Favorite>().HasIndex(f => new { f.UserId, f.VenueId }).IsUnique();

        mb.Entity<Venue>().HasData(
            new Venue
            {
                Id = 1,
                Name = "Sân cầu lông Thăng Long",
                Sport = "Cầu lông",
                Icon = "🏸",
                Address = "12 Láng Hạ",
                District = "Đống Đa",
                PricePerHour = 80000,
                PeakPricePerHour = 100000,
                WeekendPricePerHour = 110000,
                Rating = 4.6,
                ReviewCount = 128
            },
            new Venue
            {
                Id = 2,
                Name = "Pickleball Cầu Giấy",
                Sport = "Pickleball",
                Icon = "🏓",
                Address = "45 Trần Thái Tông",
                District = "Cầu Giấy",
                PricePerHour = 150000,
                PeakPricePerHour = 180000,
                WeekendPricePerHour = 200000,
                Rating = 4.8,
                ReviewCount = 64
            },
            new Venue
            {
                Id = 3,
                Name = "Tennis Tây Hồ",
                Sport = "Tennis",
                Icon = "🎾",
                Address = "8 Xuân Diệu",
                District = "Tây Hồ",
                PricePerHour = 220000,
                PeakPricePerHour = 260000,
                WeekendPricePerHour = 280000,
                Rating = 4.5,
                ReviewCount = 92
            },
            new Venue
            {
                Id = 4,
                Name = "Sân bóng rổ Hoàng Mai",
                Sport = "Bóng rổ",
                Icon = "🏀",
                Address = "101 Giải Phóng",
                District = "Hoàng Mai",
                PricePerHour = 120000,
                PeakPricePerHour = 140000,
                WeekendPricePerHour = 150000,
                Rating = 4.3,
                ReviewCount = 37
            },
            new Venue
            {
                Id = 5,
                Name = "Cầu lông Mỹ Đình",
                Sport = "Cầu lông",
                Icon = "🏸",
                Address = "20 Lê Đức Thọ",
                District = "Nam Từ Liêm",
                PricePerHour = 90000,
                PeakPricePerHour = 110000,
                WeekendPricePerHour = 120000,
                Rating = 4.7,
                ReviewCount = 210
            }
        );

        // Cấu hình giảm giá của đơn đặt sân.
        mb.Entity<Booking>()
            .Property(b => b.DiscountAmount)
            .HasPrecision(18, 0);

        // Mỗi đơn chỉ được đánh giá một lần.
        mb.Entity<Review>()
            .HasIndex(r => r.BookingId)
            .IsUnique();

        // Cấu hình mã khuyến mãi.
        mb.Entity<Promotion>(e =>
        {
            e.Property(p => p.Code)
                .HasMaxLength(50);

            e.HasIndex(p => p.Code)
                .IsUnique();

            e.Property(p => p.MaxDiscount)
                .HasPrecision(18, 0);

            e.Property(p => p.MinTotal)
                .HasPrecision(18, 0);
        });

        // Dữ liệu khuyến mãi mẫu.
        mb.Entity<Promotion>().HasData(
            new Promotion
            {
                Id = 1,
                Code = "WELCOME10",
                Description = "Giảm 10% (tối đa 30.000 đ)",
                PercentOff = 10,
                MaxDiscount = 30000,
                ValidFrom = new DateTime(2026, 1, 1),
                ValidTo = new DateTime(2030, 12, 31)
            },
            new Promotion
            {
                Id = 2,
                Code = "SAN20",
                Description = "Giảm 20% cho đơn từ 200.000 đ (tối đa 50.000 đ)",
                PercentOff = 20,
                MaxDiscount = 50000,
                MinTotal = 200000,
                UsageLimit = 100,
                ValidFrom = new DateTime(2026, 1, 1),
                ValidTo = new DateTime(2030, 12, 31)
            }
        );
        mb.Entity<VenueImage>().HasIndex(i => i.VenueId);

        mb.Entity<AppUser>().Property(u => u.Phone).HasMaxLength(20);
        mb.Entity<AppUser>().HasIndex(u => u.Phone).IsUnique();

        mb.Entity<OtpCode>().Property(o => o.CodeHash).HasMaxLength(64);
        mb.Entity<OtpCode>().HasIndex(o => new { o.UserId, o.CreatedAt });
        mb.Entity<PointTransaction>().HasIndex(t => t.UserId);
        mb.Entity<PointTransaction>().HasIndex(t => t.BookingId).IsUnique();   // mỗi đơn cộng điểm một lần

        mb.Entity<RecurringBooking>().HasIndex(r => r.UserId);

        mb.Entity<CostSplit>().Property(s => s.Total).HasPrecision(18, 0);
        mb.Entity<CostSplit>().HasIndex(s => s.BookingId).IsUnique();
        mb.Entity<SplitMember>().Property(m => m.Amount).HasPrecision(18, 0);

        mb.Entity<MatchPost>().Property(m => m.CostPerPerson).HasPrecision(18, 0);
        mb.Entity<MatchJoin>().HasIndex(j => new { j.MatchId, j.UserId }).IsUnique();
        mb.Entity<Notification>().HasIndex(n => new { n.UserId, n.Id });

        mb.Entity<MatchMessage>().Property(m => m.Text).HasMaxLength(1000);
        mb.Entity<MatchMessage>().HasIndex(m => new { m.MatchId, m.Id });

    }
}
