using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class AuthService(HttpClient http) : IAuthService
{
    private const string StorageKey = "session";

    public AuthResponse? CurrentUser { get; private set; }

    public string RoleText => CurrentUser?.Role switch
    {
        "Admin" => "Quản trị viên",
        "Owner" => "Chủ sân",
        _ => "Khách hàng"
    };

    // Mở app lại: lấy phiên đăng nhập đã lưu nếu còn hạn
    public async Task<bool> TryRestoreAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            if (string.IsNullOrEmpty(json)) return false;

            var saved = JsonSerializer.Deserialize<AuthResponse>(json);
            if (saved == null || saved.ExpiresAt <= DateTime.UtcNow)
            {
                SecureStorage.Default.Remove(StorageKey);
                return false;
            }

            Apply(saved);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<string?> LoginAsync(string email, string password) =>
        SendAsync("api/auth/login", new { email, password });

    public Task<string?> RegisterAsync(string fullName, string email, string password) =>
        SendAsync("api/auth/register", new { fullName, email, password });

    // Trả về null nếu thành công, ngược lại trả về nội dung lỗi để hiện cho người dùng
    private async Task<string?> SendAsync(string url, object body)
    {
        AuthResponse? auth;
        try
        {
            var res = await http.PostAsJsonAsync(url, body);
            if (!res.IsSuccessStatusCode)
            {
                try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
                catch { return "Có lỗi xảy ra."; }
            }

            auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return "Có lỗi xảy ra.";
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }

        Apply(auth);

        try { await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(auth)); }
        catch { /* không lưu được thì lần sau đăng nhập lại, không sao */ }

        return null;
    }

    private void Apply(AuthResponse auth)
    {
        CurrentUser = auth;
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);
    }

    public void Logout()
    {
        CurrentUser = null;
        http.DefaultRequestHeaders.Authorization = null;
        SecureStorage.Default.Remove(StorageKey);
    }
    public async Task<string?> UpdateProfileAsync(string fullName)
    {
        try
        {
            var res = await http.PutAsJsonAsync("api/auth/profile", new { fullName });
            if (!res.IsSuccessStatusCode) return await ReadError(res);

            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return "Có lỗi xảy ra.";

            Apply(auth);
            try { await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(auth)); }
            catch { /* không lưu được thì lần sau đăng nhập lại, không sao */ }
            return null;
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<string?> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var res = await http.PostAsJsonAsync("api/auth/change-password", new { currentPassword, newPassword });
            return res.IsSuccessStatusCode ? null : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage res)
    {
        try { return await res.Content.ReadFromJsonAsync<string>() ?? "Có lỗi xảy ra."; }
        catch { return "Có lỗi xảy ra."; }
    }
    // Lấy token mới sau khi quyền thay đổi (ví dụ vừa được duyệt làm chủ sân)
    public async Task<string?> RefreshAsync()
    {
        try
        {
            var res = await http.PostAsync("api/auth/refresh", null);
            if (!res.IsSuccessStatusCode) return await ReadError(res);

            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
            if (auth == null) return "Có lỗi xảy ra.";

            Apply(auth);
            try { await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(auth)); }
            catch { /* không lưu được thì lần sau đăng nhập lại, không sao */ }
            return null;
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }
}