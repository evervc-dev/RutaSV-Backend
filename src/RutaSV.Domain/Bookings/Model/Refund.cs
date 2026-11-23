using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Bookings.Model;

public class Refund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RefundCode { get; set; } = string.Empty;
    public Guid ReservationId { get; set; }
    public decimal Amount { get; set; }
    public short Percentage { get; set; }
    public RefundReason Reason { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.EN_PROCESO;
    public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Reservation? Reservation { get; set; }
    public User? ProcessedByUser { get; set; }
}
