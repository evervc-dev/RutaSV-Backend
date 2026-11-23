using RutaSV.Application.Bookings.Service;
using RutaSV.Domain.Bookings.Enum;

namespace RutaSV.UnitTests.Bookings;

public sealed class BookingBusinessRulesTests
{
    [Fact]
    public void CalculateVisitorCancellationRefund_48OrMoreHours_ShouldReturn100Percent()
    {
        DateTime nowUtc = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc); // 12:00 SV
        DateOnly activityDate = new(2026, 6, 12);
        TimeOnly startTime = new(12, 0); // exactamente 48h

        (short pct, decimal amount, PaymentStatus status, _) = BookingBusinessRules.CalculateVisitorCancellationRefund(
            120m, activityDate, startTime, nowUtc);

        Assert.Equal((short)100, pct);
        Assert.Equal(120m, amount);
        Assert.Equal(PaymentStatus.REEMBOLSADO_DEMO, status);
    }

    [Fact]
    public void CalculateVisitorCancellationRefund_Between24And48Hours_ShouldReturn50Percent()
    {
        DateTime nowUtc = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc); // 12:00 SV
        DateOnly activityDate = new(2026, 6, 11);
        TimeOnly startTime = new(18, 0); // 30h antes

        (short pct, decimal amount, PaymentStatus status, _) = BookingBusinessRules.CalculateVisitorCancellationRefund(
            99m, activityDate, startTime, nowUtc);

        Assert.Equal((short)50, pct);
        Assert.Equal(49.50m, amount);
        Assert.Equal(PaymentStatus.REEMBOLSADO_PARCIAL_DEMO, status);
    }

    [Fact]
    public void CalculateVisitorCancellationRefund_LessThan24Hours_ShouldReturn0Percent()
    {
        DateTime nowUtc = new(2026, 6, 10, 18, 0, 0, DateTimeKind.Utc); // 12:00 SV
        DateOnly activityDate = new(2026, 6, 11);
        TimeOnly startTime = new(8, 0); // 20h antes

        (short pct, decimal amount, PaymentStatus status, _) = BookingBusinessRules.CalculateVisitorCancellationRefund(
            80m, activityDate, startTime, nowUtc);

        Assert.Equal((short)0, pct);
        Assert.Equal(0m, amount);
        Assert.Equal(PaymentStatus.PAGADO_DEMO, status);
    }
}
