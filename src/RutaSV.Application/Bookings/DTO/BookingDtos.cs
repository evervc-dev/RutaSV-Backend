namespace RutaSV.Application.Bookings.DTO;

public sealed record CreateReservationRequest(
    Guid ExperienceId,
    Guid AvailabilityId,
    short PeopleCount,
    string ContactName,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? SpecialNotes = null,
    string? PaymentMethod = "TARJETA_DEMO");

public sealed record CreateInPersonReservationRequest(
    Guid ExperienceId,
    Guid AvailabilityId,
    short PeopleCount,
    string ContactName,
    string ContactPhone,
    string? ContactEmail = null,
    string? SpecialNotes = null);

public sealed record CancelReservationRequest(
    string? Reason = null);

public sealed record RejectReservationRequest(
    string Reason);

public sealed record RescheduleReservationRequest(
    Guid AvailabilityId,
    short? PeopleCount = null,
    string? Notes = null);

public sealed record ResolveIncidentRequest(
    string Action,
    Guid? NewAvailabilityId = null,
    string? Notes = null);

public sealed record CompleteRefundRequest(
    string? Notes = null);

public sealed record ReservationHistoryDto(
    Guid Id,
    string? PreviousStatus,
    string NewStatus,
    Guid? ChangedBy,
    string? Notes,
    DateTime CreatedAt);

public sealed record RefundDto(
    Guid Id,
    string RefundCode,
    Guid ReservationId,
    string ReservationCode,
    string ExperienceTitle,
    string VisitorName,
    string? VisitorEmail,
    decimal Amount,
    short Percentage,
    string Currency,
    string Reason,
    string Status,
    string? Notes,
    DateTime RequestedAt,
    DateTime? ProcessedAt,
    Guid? ProcessedBy);

public sealed record ReservationIncidentDto(
    Guid Id,
    string Cause,
    string Status,
    string? Reason,
    Guid OriginalAvailabilityId,
    Guid? NewAvailabilityId,
    DateTime DecisionDeadline,
    DateTime? ResolvedAt,
    bool AutoResolved,
    DateTime CreatedAt);

public sealed record ReservationDto(
    Guid Id,
    string ReservationCode,
    Guid ExperienceId,
    string ExperienceTitle,
    string ExperienceSlug,
    string? ExperienceCoverImageUrl,
    string? CompanyTradeName,
    string? LocationLabel,
    Guid AvailabilityId,
    string ActivityDate,
    string StartTime,
    string? EndTime,
    Guid? VisitorId,
    Guid CreatedBy,
    string Source,
    short PeopleCount,
    decimal UnitPrice,
    decimal Total,
    string Currency,
    string Status,
    string PaymentStatus,
    string? PaymentReference,
    string ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? SpecialNotes,
    string? CancellationReason,
    DateTime? CancelledAt,
    DateTime? ConfirmedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    RefundDto? Refund,
    ReservationIncidentDto? ActiveIncident,
    IReadOnlyList<ReservationHistoryDto> History);

public sealed record CancelPreviewDto(
    Guid ReservationId,
    bool CanCancel,
    double HoursUntilStart,
    short RefundPercentage,
    decimal RefundAmount,
    string Currency,
    string PolicyDescription);
