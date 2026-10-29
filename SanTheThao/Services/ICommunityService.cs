using SanTheThao.Models;

namespace SanTheThao.Services;

public interface ICommunityService
{
    // Điểm thưởng
    Task<LoyaltyInfo?> GetLoyaltyAsync();
    Task<(string? Error, RedeemResult? Result)> RedeemAsync(string rewardId);

    // Đặt sân cố định
    Task<(string? Error, RecurringResult? Result)> CreateRecurringAsync(int venueId, DateTime startDate, List<int> hours, int weeks);
    Task<List<RecurringItem>> GetRecurringAsync();
    Task<string?> CancelRecurringAsync(Guid id);

    // Chia tiền
    Task<SplitInfo?> GetSplitAsync(Guid bookingId);
    Task<(string? Error, SplitInfo? Split)> CreateSplitAsync(Guid bookingId, int people, List<string> names, string note);
    Task<string?> DeleteSplitAsync(Guid bookingId);
    Task<(string? Error, SplitInfo? Split)> TogglePaidAsync(Guid memberId);

    // Ghép kèo
    Task<List<MatchItem>> GetMatchesAsync(string sport, bool mine);
    Task<MatchDetail?> GetMatchAsync(Guid id);
    Task<string?> CreateMatchAsync(string sport, int? venueId, DateTime date, int startHour, int endHour,
        int playersNeeded, string level, decimal costPerPerson, string note);
    Task<string?> JoinMatchAsync(Guid id, string message);
    Task<string?> LeaveMatchAsync(Guid id);
    Task<string?> ReviewJoinAsync(Guid matchId, Guid joinId, bool approve);
    Task<string?> CloseMatchAsync(Guid id);
    Task<(string? Error, ChatState? State)> GetChatAsync(Guid matchId, int after);
    Task<(string? Error, ChatMessage? Message)> SendChatAsync(Guid matchId, string text);
}