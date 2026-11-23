using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class AvailabilityNotFoundException(Guid availabilityId)
    : DomainException($"No se encontró la disponibilidad con identificador '{availabilityId}'.");
