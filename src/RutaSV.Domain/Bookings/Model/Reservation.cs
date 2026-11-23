using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Bookings.Model;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReservationCode { get; set; } = string.Empty;
    public Guid ExperienceId { get; set; }
    public Guid AvailabilityId { get; set; }
    public Guid? VisitorId { get; set; }
    public Guid CreatedBy { get; set; }
    public ReservationSource Source { get; set; } = ReservationSource.WEB;
    public short PeopleCount { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "USD";
    public ReservationStatus Status { get; set; } = ReservationStatus.PENDIENTE;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.PAGADO_DEMO;
    public string? PaymentReference { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? SpecialNotes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Experience? Experience { get; set; }
    public Availability? Availability { get; set; }
    public User? Visitor { get; set; }
    public User? CreatedByUser { get; set; }
    public User? CancelledByUser { get; set; }
    public Refund? Refund { get; set; }
    public ICollection<ReservationHistory> History { get; set; } = [];
    public ICollection<ReservationIncident> Incidents { get; set; } = [];
}
