using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class CommunityService(HttpClient http) : ICommunityService
{
    // ----- Trò chuyện trong kèo -----
    public async Task<(string? Error, ChatState? State)> GetChatAsync(Guid matchId, int after)
    {
        try
        {
            var res = await http.GetAsync($"api/matches/{matchId}/chat?after={after}");
            if (!res.IsSuccessStatusCode) return (await ReadError(res), null);
            return (null, await res.Content.ReadFromJsonAsync<ChatState>());
        }
        catch (Exception)
        {
            return ("Không kết nối được máy chủ.", null);
        }
    }

    public Task<(string? Error, ChatMessage? Message)> SendChatAsync(Guid matchId, string text) =>
        PostForAsync<ChatMessage>($"api/matches/{matchId}/chat", new { text });

    // ----- Điểm thưởng -----
    public async Task<LoyaltyInfo?> GetLoyaltyAsync() =>
        await http.GetFromJsonAsync<LoyaltyInfo>("api/loyalty");

    public Task<(string? Error, RedeemResult? Result)> RedeemAsync(string rewardId) =>
        PostForAsync<RedeemResult>("api/loyalty/redeem", new { rewardId });

    // ----- Đặt sân cố định -----
    public async Task<(string? Error, RecurringResult? Result)> CreateRecurringAsync(
        int venueId, DateTime startDate, List<int> hours, int weeks)
    {
        var r = await PostForAsync<RecurringResult>("api/recurring", new { venueId, startDate, hours, weeks });
        return r;
    }

    public async Task<List<RecurringItem>> GetRecurringAsync() =>
        await http.GetFromJsonAsync<List<RecurringItem>>("api/recurring") ?? new();

    public Task<string?> CancelRecurringAsync(Guid id) => PostAsync($"api/recurring/{id}/cancel");

    // ----- Chia tiền -----
    public async Task<SplitInfo?> GetSplitAsync(Guid bookingId) =>
        await http.GetFromJsonAsync<SplitInfo?>($"api/bookings/{bookingId}/split");

    public Task<(string? Error, SplitInfo? Split)> CreateSplitAsync(Guid bookingId, int people, List<string> names, string note) =>
        PostForAsync<SplitInfo>($"api/bookings/{bookingId}/split", new { people, names, note });

    public async Task<string?> DeleteSplitAsync(Guid bookingId)
    {
        try
        {
            var res = await http.DeleteAsync($"api/bookings/{bookingId}/split");
            return res.IsSuccessStatusCode ? null : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public Task<(string? Error, SplitInfo? Split)> TogglePaidAsync(Guid memberId) =>
        PostForAsync<SplitInfo>($"api/splits/members/{memberId}/paid");

    // ----- Ghép kèo -----
    public async Task<List<MatchItem>> GetMatchesAsync(string sport, bool mine) =>
        await http.GetFromJsonAsync<List<MatchItem>>(
            $"api/matches?sport={Uri.EscapeDataString(sport)}&mine={mine.ToString().ToLowerInvariant()}") ?? new();

    public async Task<MatchDetail?> GetMatchAsync(Guid id) =>
        await http.GetFromJsonAsync<MatchDetail>($"api/matches/{id}");

    public Task<string?> CreateMatchAsync(string sport, int? venueId, DateTime date, int startHour, int endHour,
        int playersNeeded, string level, decimal costPerPerson, string note) =>
        PostAsync("api/matches", new { sport, venueId, date, startHour, endHour, playersNeeded, level, costPerPerson, note });

    public Task<string?> JoinMatchAsync(Guid id, string message) =>
        PostAsync($"api/matches/{id}/join", new { message });

    public Task<string?> LeaveMatchAsync(Guid id) => PostAsync($"api/matches/{id}/leave");

    public Task<string?> ReviewJoinAsync(Guid matchId, Guid joinId, bool approve) =>
        PostAsync($"api/matches/{matchId}/joins/{joinId}/{(approve ? "approve" : "reject")}");

    public Task<string?> CloseMatchAsync(Guid id) => PostAsync($"api/matches/{id}/close");

    // ----- Hàm dùng chung -----
    private async Task<string?> PostAsync(string url, object? body = null)
    {
        try
        {
            var res = body == null ? await http.PostAsync(url, null) : await http.PostAsJsonAsync(url, body);
            return res.IsSuccessStatusCode ? null : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    private async Task<(string? Error, T? Value)> PostForAsync<T>(string url, object? body = null)
    {
        try
        {
            var res = body == null ? await http.PostAsync(url, null) : await http.PostAsJsonAsync(url, body);
            if (!res.IsSuccessStatusCode) return (await ReadError(res), default);
            return (null, await res.Content.ReadFromJsonAsync<T>());
        }
        catch (Exception)
        {
            return ("Không kết nối được máy chủ.", default);
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage res)
    {
        try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
        catch { return "Có lỗi xảy ra."; }
    }
}