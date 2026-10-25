namespace SanTheThao.Api.Models;

public class Venue
{
    public int Id { get; set; }
    public Guid? OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Sport { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Address { get; set; } = "";
    public string District { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public decimal PricePerHour { get; set; }          // giá thường
    public decimal PeakPricePerHour { get; set; }      // giá giờ cao điểm (0 = không dùng)
    public int PeakStartHour { get; set; } = 17;       // cao điểm từ giờ này...
    public int PeakEndHour { get; set; } = 22;         // ...đến trước giờ này
    public decimal WeekendPricePerHour { get; set; }   // giá cuối tuần (0 = không dùng)

    public double Rating { get; set; }
    public int ReviewCount { get; set; }

    public string? ImageUrl { get; set; }              // đường dẫn ảnh, ví dụ /uploads/venue-1-abc.jpg
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }
    public int VenueId { get; set; }
    public Venue? Venue { get; set; }
    public DateTime Date { get; set; }
    public string HoursCsv { get; set; } = "";
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Đã đặt";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<BookingSlot> Slots { get; set; } = new();
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool Reviewed { get; set; }
}

// Mỗi dòng = một giờ của một sân trong một ngày. Dùng để khóa giờ, chống đặt trùng.
public class BookingSlot
{
    public int Id { get; set; }
    public Guid BookingId { get; set; }
    public int VenueId { get; set; }
    public DateTime Date { get; set; }
    public int Hour { get; set; }
}

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Customer";   // Customer, Owner, Admin
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
public class Review
{
    public int Id { get; set; }
    public Guid BookingId { get; set; }
    public int VenueId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Promotion
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public int PercentOff { get; set; }          // giảm bao nhiêu %
    public decimal MaxDiscount { get; set; }     // giảm tối đa (0 = không giới hạn)
    public decimal MinTotal { get; set; }        // đơn tối thiểu (0 = không yêu cầu)
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int UsageLimit { get; set; }          // số lượt dùng tối đa (0 = không giới hạn)
    public int UsedCount { get; set; }
    public int? VenueId { get; set; }            // null = dùng cho mọi sân
    public bool IsActive { get; set; } = true;
    public Guid? OwnerId { get; set; }           // chủ sân tạo mã (null = mã của hệ thống)
    public class Favorite
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int VenueId { get; set; }
    }
    public bool IsLocked { get; set; }
    public class OwnerApplication
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public string BusinessName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Note { get; set; } = "";
        public string Status { get; set; } = "Chờ duyệt";   // Chờ duyệt, Đã duyệt, Từ chối
        public string? AdminNote { get; set; }               // lý do từ chối
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ReviewedAt { get; set; }
    }
}