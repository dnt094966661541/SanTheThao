namespace SanTheThao.Api.Models;

public static class PromoRules
{
    // Trả về null nếu mã dùng được, ngược lại trả về lý do
    public static string? Validate(Promotion? p, Venue venue, DateTime now)
    {
        if (p is null || !p.IsActive) return "Mã giảm giá không tồn tại.";

        // Ngày kết thúc tính hết cả ngày đó
        if (now < p.ValidFrom.Date || now >= p.ValidTo.Date.AddDays(1))
            return "Mã giảm giá đã hết hạn hoặc chưa đến ngày dùng.";

        // Mã gắn với một sân cụ thể
        if (p.VenueId != null && p.VenueId != venue.Id)
            return "Mã này không dùng được cho sân đang chọn.";

        // Mã của một chủ sân (áp dụng cho mọi sân của người đó)
        if (p.VenueId == null && p.OwnerId != null && p.OwnerId != venue.OwnerId)
            return "Mã này không dùng được cho sân đang chọn.";

        if (p.UsageLimit > 0 && p.UsedCount >= p.UsageLimit)
            return "Mã giảm giá đã hết lượt dùng.";

        return null;
    }

    public static decimal Discount(Promotion p, decimal subtotal)
    {
        if (subtotal < p.MinTotal) return 0;
        var d = Math.Floor(subtotal * p.PercentOff / 100m);
        if (p.MaxDiscount > 0) d = Math.Min(d, p.MaxDiscount);
        return d;
    }
}