using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Domain.Bookings.Model;

public class ReservationIncident
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReservationId { get; set; }
    public IncidentCause Cause { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.PENDIENTE;
    public string? Reason { get; set; }
    public Guid OriginalAvailabilityId { get; set; }
    public Guid? NewAvailabilityId { get; set; }
    public DateTime DecisionDeadline { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool AutoResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Reservation? Reservation { get; set; }
    public Availability? OriginalAvailability { get; set; }
    public Availability? NewAvailability { get; set; }
}
