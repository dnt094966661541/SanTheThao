namespace SanTheThao.Api.Models;

public static class BookingTime
{
    // Giờ kết thúc của đơn (hết khung giờ cuối cùng)
    public static DateTime EndOf(Booking b)
    {
        var last = b.HoursCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).Max();
        return b.Date.Date.AddHours(last + 1);
    }
}

public class PointTransaction
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int Points { get; set; }                 // dương là được cộng, âm là đã đổi quà
    public string Reason { get; set; } = "";
    public Guid? BookingId { get; set; }            // chỉ dùng cho điểm cộng từ đơn, mỗi đơn một lần
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class RecurringBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int VenueId { get; set; }
    public int Weekday { get; set; }                // 0 = Chủ nhật ... 6 = Thứ bảy
    public string HoursCsv { get; set; } = "";
    public DateTime StartDate { get; set; }
    public int Weeks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class CostSplit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public decimal Total { get; set; }
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<SplitMember> Members { get; set; } = new();
}

public class SplitMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CostSplitId { get; set; }
    public int Sort { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
    public bool IsOwner { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class MatchPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HostId { get; set; }
    public string Sport { get; set; } = "";
    public int? VenueId { get; set; }
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int PlayersNeeded { get; set; }
    public string Level { get; set; } = "Trung bình";
    public decimal CostPerPerson { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "Đang mở";   // Đang mở, Đủ người, Đã đóng
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MatchJoin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Chờ duyệt";  // Chờ duyệt, Được nhận, Từ chối
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}