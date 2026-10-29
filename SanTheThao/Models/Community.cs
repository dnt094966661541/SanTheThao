using Microsoft.Maui.Graphics;

namespace SanTheThao.Models;

// ---------- Điểm thưởng ----------
public class RewardItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int Cost { get; set; }
    public bool CanAfford { get; set; }
    public string CostText => $"{Cost} điểm";
}

public class PointTx
{
    public int Points { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public string PointsText => Points > 0 ? $"+{Points}" : Points.ToString();
    public string DateText => CreatedAt.ToString("dd/MM/yyyy");
    public Color PointsColor => Color.FromArgb(Points > 0 ? "#1B8A3A" : "#D32F2F");
}

public class LoyaltyInfo
{
    public int Balance { get; set; }
    public int Lifetime { get; set; }
    public string Tier { get; set; } = "";
    public string? NextTier { get; set; }
    public int TierFrom { get; set; }
    public int NextAt { get; set; }
    public decimal Multiplier { get; set; }
    public List<RewardItem> Rewards { get; set; } = new();
    public List<PointTx> History { get; set; } = new();

    public double Progress => NextAt <= TierFrom
        ? 1
        : Math.Clamp((double)(Lifetime - TierFrom) / (NextAt - TierFrom), 0, 1);
}

public class RedeemResult
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public int PointsLeft { get; set; }
    public DateTime ValidTo { get; set; }
}

// ---------- Đặt sân cố định ----------
public class RecurringResult
{
    public Guid RecurringId { get; set; }
    public int Created { get; set; }
    public decimal Total { get; set; }
    public List<string> Skipped { get; set; } = new();
}

public class RecurringItem
{
    public Guid Id { get; set; }
    public string VenueName { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Weekday { get; set; }
    public List<int> Hours { get; set; } = new();
    public DateTime StartDate { get; set; }
    public int Weeks { get; set; }
    public int Upcoming { get; set; }
    public int Done { get; set; }
    public int Cancelled { get; set; }

    private static readonly string[] Days =
        { "Chủ nhật", "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy" };

    public string WeekdayText => Days[Math.Clamp(Weekday, 0, 6)];
    public string HoursText => string.Join(", ", Hours.OrderBy(h => h).Select(h => $"{h:00}h-{h + 1:00}h"));
    public string Summary => $"Mỗi {WeekdayText.ToLower()} · {HoursText}";
    public string CountText => $"Sắp tới {Upcoming} · Đã chơi {Done} · Đã hủy {Cancelled}";
    public string StartText => $"Từ {StartDate:dd/MM/yyyy} · {Weeks} tuần";
    public bool CanCancel => Upcoming > 0;
}

// ---------- Chia tiền ----------
public class SplitMemberItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public bool Paid { get; set; }
    public bool IsOwner { get; set; }

    public string AmountText => $"{Amount:N0} đ";
    public string StatusText => Paid ? "Đã trả" : "Chưa trả";
    public Color StatusColor => Color.FromArgb(Paid ? "#1B8A3A" : "#B7791F");
    public Color StatusBackColor => Color.FromArgb(Paid ? "#E8F5EC" : "#FEF3D7");
    public bool CanToggle => !IsOwner;
    public string ToggleText => Paid ? "Bỏ đánh dấu" : "Đánh dấu đã trả";
}

public class SplitInfo
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal Total { get; set; }
    public string Note { get; set; } = "";
    public decimal PaidTotal { get; set; }
    public List<SplitMemberItem> Members { get; set; } = new();

    public bool CanDelete => Members.All(m => m.IsOwner || !m.Paid);
}

// ---------- Ghép kèo ----------
public class MatchItem
{
    public Guid Id { get; set; }
    public string HostName { get; set; } = "";
    public string Sport { get; set; } = "";
    public int? VenueId { get; set; }
    public string VenueName { get; set; } = "";
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int PlayersNeeded { get; set; }
    public int Accepted { get; set; }
    public string Level { get; set; } = "";
    public decimal CostPerPerson { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsMine { get; set; }
    public string? MyJoinStatus { get; set; }

    public string SportIcon => Sport switch
    {
        "Bóng đá" => "⚽",
        "Cầu lông" => "🏸",
        "Pickleball" => "🏓",
        "Tennis" => "🎾",
        "Bóng rổ" => "🏀",
        _ => "🏟️"
    };

    public int Remaining => Math.Max(0, PlayersNeeded - Accepted);
    public string Title => Status == "Đủ người" ? $"{Sport} · Đã đủ người" : $"{Sport} · Cần thêm {Remaining} người";
    public string TimeText => $"{Date:dd/MM/yyyy} · {StartHour:00}h - {EndHour:00}h";
    public string PlaceText => string.IsNullOrWhiteSpace(VenueName) ? "Chưa chốt sân" : VenueName;
    public string CostText => CostPerPerson > 0 ? $"{CostPerPerson:N0} đ/người" : "Thỏa thuận";
    public string CountText => $"{Accepted}/{PlayersNeeded} người đã được nhận";
    public string HostText => $"Chủ kèo: {HostName}";
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public string RoleText => IsMine ? "Kèo của bạn" : MyJoinStatus switch
    {
        "Chờ duyệt" => "Bạn đang chờ duyệt",
        "Được nhận" => "Bạn đã được nhận",
        "Từ chối" => "Bạn bị từ chối",
        _ => ""
    };
    public bool HasRole => RoleText != "";

    public Color StatusColor => Color.FromArgb(
        Status == "Đang mở" ? "#1B8A3A" : Status == "Đủ người" ? "#B7791F" : "#6B7280");
    public Color StatusBackColor => Color.FromArgb(
        Status == "Đang mở" ? "#E8F5EC" : Status == "Đủ người" ? "#FEF3D7" : "#F0F1F0");
}

public class MatchJoinItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Phone { get; set; }
    public bool IsMe { get; set; }

    public bool CanReview => Status == "Chờ duyệt";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasPhone => !string.IsNullOrWhiteSpace(Phone);
    public string PhoneText => $"📞 {Phone}";
    public string NameText => IsMe ? $"{UserName} (bạn)" : UserName;
    public Color StatusColor => Color.FromArgb(
        Status == "Được nhận" ? "#1B8A3A" : Status == "Từ chối" ? "#D32F2F" : "#B7791F");
    public Color StatusBackColor => Color.FromArgb(
        Status == "Được nhận" ? "#E8F5EC" : Status == "Từ chối" ? "#FDECEA" : "#FEF3D7");
}

public class MatchDetail
{
    public MatchItem Match { get; set; } = new();
    public string? HostPhone { get; set; }
    public List<MatchJoinItem> Joins { get; set; } = new();
}