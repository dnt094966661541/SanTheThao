namespace SanTheThao.Models;

public class RevenueDay
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public int Count { get; set; }

    // Tỷ lệ so với ngày cao nhất của tháng (0 đến 1), dùng cho thanh dài ngắn
    public double Ratio { get; set; }

    public string DayText => Date.ToString("dd/MM/yyyy");
    public string TotalText => $"{Total:N0} đ";
    public string CountText => $"{Count} đơn";
}

public class RevenueResult
{
    public string Month { get; set; } = "";
    public decimal Total { get; set; }
    public int BookingCount { get; set; }
    public List<RevenueDay> PerDay { get; set; } = new();
}