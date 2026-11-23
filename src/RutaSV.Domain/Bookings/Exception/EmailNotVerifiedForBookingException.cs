using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class EmailNotVerifiedForBookingException()
    : DomainException("Debes verificar tu correo electrónico antes de poder realizar una reserva.");
