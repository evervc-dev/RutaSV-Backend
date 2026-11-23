using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class ReservationNotFoundException : DomainException
{
    public ReservationNotFoundException(Guid reservationId)
        : base($"No se encontró la reserva con identificador '{reservationId}'.")
    {
    }

    public ReservationNotFoundException(string code)
        : base($"No se encontró la reserva '{code}'.")
    {
    }
}
