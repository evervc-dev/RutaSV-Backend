using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Bookings.Model;

public class ReservationHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public ReservationStatus? PreviousStatus { get; set; }
    public ReservationStatus NewStatus { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Reservation? Reservation { get; set; }
    public User? ChangedByUser { get; set; }
}
