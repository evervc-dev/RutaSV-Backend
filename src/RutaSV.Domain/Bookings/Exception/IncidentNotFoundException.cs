using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class IncidentNotFoundException(Guid reservationId)
    : DomainException($"No se encontró una incidencia pendiente para la reserva '{reservationId}'.");
