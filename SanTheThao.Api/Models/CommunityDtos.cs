namespace SanTheThao.Api.Models;

// Điểm thưởng
public record RewardDto(string Id, string Title, int Cost);
public record PointTxDto(int Points, string Reason, DateTime CreatedAt);
public record LoyaltyDto(int Balance, int Lifetime, string Tier, string? NextTier, int TierFrom, int NextAt,
    decimal Multiplier, List<RewardDto> Rewards, List<PointTxDto> History);
public record RedeemRequest(string RewardId);
public record RedeemResponse(string Code, string Description, int PointsLeft, DateTime ValidTo);

// Đặt sân cố định
public record RecurringRequest(int VenueId, DateTime StartDate, List<int> Hours, int Weeks);
public record RecurringResult(Guid RecurringId, int Created, decimal Total, List<string> Skipped);
public record RecurringDto(Guid Id, string VenueName, string Icon, int Weekday, List<int> Hours,
    DateTime StartDate, int Weeks, int Upcoming, int Done, int Cancelled);

// Chia tiền
public record CreateSplitRequest(int People, List<string>? Names, string? Note);
public record SplitMemberDto(Guid Id, string Name, decimal Amount, bool Paid, bool IsOwner);
public record SplitDto(Guid Id, Guid BookingId, decimal Total, string Note, decimal PaidTotal, List<SplitMemberDto> Members);

// Ghép kèo
public record MatchDto(Guid Id, string HostName, string Sport, int? VenueId, string VenueName, DateTime Date,
    int StartHour, int EndHour, int PlayersNeeded, int Accepted, string Level, decimal CostPerPerson,
    string Note, string Status, bool IsMine, string? MyJoinStatus);
public record MatchJoinDto(Guid Id, string UserName, string Message, string Status, string? Phone, bool IsMe);
public record MatchDetailDto(MatchDto Match, string? HostPhone, List<MatchJoinDto> Joins);
public record CreateMatchRequest(string Sport, int? VenueId, DateTime Date, int StartHour, int EndHour,
    int PlayersNeeded, string Level, decimal CostPerPerson, string? Note);
public record JoinMatchRequest(string? Message);