namespace SanTheThao.Services;

public static class ApiConfig
{
    // Máy ảo Android gọi vào máy tính của bạn qua địa chỉ 10.0.2.2
    public static string BaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5080"
            : "http://localhost:5080";
}