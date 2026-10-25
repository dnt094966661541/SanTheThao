namespace SanTheThao.Api.Models;

public static class Pricing
{
    public static decimal HourPrice(Venue v, DateTime date, int hour)
    {
        var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (weekend && v.WeekendPricePerHour > 0)
            return v.WeekendPricePerHour;

        if (v.PeakPricePerHour > 0 && hour >= v.PeakStartHour && hour < v.PeakEndHour)
            return v.PeakPricePerHour;

        return v.PricePerHour;
    }
}