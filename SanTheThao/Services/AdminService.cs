using System.Net.Http.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class AdminService(HttpClient http) : IAdminService
{
    public async Task<AdminStats?> GetStatsAsync() =>
        await http.GetFromJsonAsync<AdminStats>("api/admin/stats");

    public async Task<List<AdminUser>> GetUsersAsync() =>
        await http.GetFromJsonAsync<List<AdminUser>>("api/admin/users") ?? new();

    public async Task<string?> ToggleLockAsync(Guid id) =>
        await PostAsync($"api/admin/users/{id}/lock", null);

    public async Task<string?> SetRoleAsync(Guid id, string role) =>
        await PostAsync($"api/admin/users/{id}/role", new { role });

    private async Task<string?> PostAsync(string url, object? body)
    {
        try
        {
            var res = body == null
                ? await http.PostAsync(url, null)
                : await http.PostAsJsonAsync(url, body);

            if (res.IsSuccessStatusCode) return null;

            try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
            catch { return "Có lỗi xảy ra."; }
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }
    public async Task<List<OwnerApplication>> GetApplicationsAsync() =>
    await http.GetFromJsonAsync<List<OwnerApplication>>("api/admin/applications") ?? new();

    public async Task<string?> ApproveApplicationAsync(int id) =>
        await PostAsync($"api/admin/applications/{id}/approve", null);

    public async Task<string?> RejectApplicationAsync(int id, string note) =>
        await PostAsync($"api/admin/applications/{id}/reject", new { note });
}