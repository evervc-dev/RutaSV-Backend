using RutaSV.Domain.Common;

namespace RutaSV.Domain.Catalog.Exception;

public sealed class AvailabilityConflictException(string message) : DomainException(message);
