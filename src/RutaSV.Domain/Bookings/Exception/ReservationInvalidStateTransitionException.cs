using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class ReservationInvalidStateTransitionException(string message) : DomainException(message);
