using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class ReservationAccessDeniedException(string message) : DomainException(message);
