
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SanTheThao.Models;

namespace SanTheThao.Services;

public class AuthService(HttpClient http) : IAuthService
{
    private const string StorageKey = "session";

    // Server báo tài khoản chưa xác minh
    public const string UnverifiedMarker = "chưa xác minh";

    public AuthResponse? CurrentUser { get; private set; }

    public string RoleText => CurrentUser?.Role switch
    {
        "Admin" => "Quản trị viên",
        "Owner" => "Chủ sân",
        _ => "Khách hàng"
    };

    // Khôi phục phiên đăng nhập khi mở app
    public async Task<bool> TryRestoreAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            if (string.IsNullOrEmpty(json))
                return false;

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

    // Đăng nhập bằng email và mật khẩu
    public Task<string?> LoginAsync(string email, string password) =>
        SendAsync("api/auth/login", new { email, password });

    // Đăng ký và yêu cầu server gửi OTP
    public Task<(string? Error, OtpSent? Sent)> RegisterAsync(
        string fullName,
        string email,
        string phone,
        string password,
        string channel) =>
        PostForOtpAsync(
            "api/auth/register",
            new { fullName, email, phone, password, channel });

    // Gửi lại OTP qua kênh được chọn
    public Task<(string? Error, OtpSent? Sent)> SendOtpAsync(
        string identifier,
        string channel) =>
        PostForOtpAsync(
            "api/auth/otp/send",
            new { identifier, channel });

    // Xác minh OTP; thành công thì lưu phiên đăng nhập
    public Task<string?> VerifyOtpAsync(string identifier, string code) =>
        SendAsync(
            "api/auth/otp/verify",
            new { identifier, code });

    // Gửi request và nhận thông tin gửi OTP
    private async Task<(string? Error, OtpSent? Sent)> PostForOtpAsync(
        string url,
        object body)
    {
        try
        {
            var res = await http.PostAsJsonAsync(url, body);

            if (!res.IsSuccessStatusCode)
                return (await ReadError(res), null);

            var sent = await res.Content.ReadFromJsonAsync<OtpSent>();

            return sent == null
                ? ("Có lỗi xảy ra.", null)
                : (null, sent);
        }
        catch (Exception)
        {
            return ("Không kết nối được máy chủ.", null);
        }
    }

    // Gửi request nhận AuthResponse và lưu phiên đăng nhập
    private async Task<string?> SendAsync(string url, object body)
    {
        AuthResponse? auth;

        try
        {
            var res = await http.PostAsJsonAsync(url, body);

            if (!res.IsSuccessStatusCode)
                return await ReadError(res);

            auth = await res.Content.ReadFromJsonAsync<AuthResponse>();

            if (auth == null)
                return "Có lỗi xảy ra.";
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }

        Apply(auth);

        try
        {
            await SecureStorage.Default.SetAsync(
                StorageKey,
                JsonSerializer.Serialize(auth));
        }
        catch
        {
            // Không lưu được phiên thì lần sau đăng nhập lại
        }

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
            var res = await http.PutAsJsonAsync(
                "api/auth/profile",
                new { fullName });

            if (!res.IsSuccessStatusCode)
                return await ReadError(res);

            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();

            if (auth == null)
                return "Có lỗi xảy ra.";

            Apply(auth);

            try
            {
                await SecureStorage.Default.SetAsync(
                    StorageKey,
                    JsonSerializer.Serialize(auth));
            }
            catch
            {
                // Không lưu được phiên thì lần sau đăng nhập lại
            }

            return null;
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    public async Task<string?> ChangePasswordAsync(
        string currentPassword,
        string newPassword)
    {
        try
        {
            var res = await http.PostAsJsonAsync(
                "api/auth/change-password",
                new { currentPassword, newPassword });

            return res.IsSuccessStatusCode
                ? null
                : await ReadError(res);
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }

    private static async Task<string> ReadError(HttpResponseMessage res)
    {
        try
        {
            var message = await res.Content.ReadFromJsonAsync<string>();

            if (!string.IsNullOrWhiteSpace(message))
                return message;

            return "Có lỗi xảy ra.";
        }
        catch
        {
            return "Có lỗi xảy ra.";
        }
    }

    // Làm mới token sau khi quyền tài khoản thay đổi
    public async Task<string?> RefreshAsync()
    {
        try
        {
            var res = await http.PostAsync("api/auth/refresh", null);

            if (!res.IsSuccessStatusCode)
                return await ReadError(res);

            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();

            if (auth == null)
                return "Có lỗi xảy ra.";

            Apply(auth);

            try
            {
                await SecureStorage.Default.SetAsync(
                    StorageKey,
                    JsonSerializer.Serialize(auth));
            }
            catch
            {
                // Không lưu được phiên thì lần sau đăng nhập lại
            }

            return null;
        }
        catch (Exception)
        {
            return "Không kết nối được máy chủ.";
        }
    }
}
