using RutaSV.Domain.Common;

namespace RutaSV.Domain.Bookings.Exception;

public sealed class InsufficientCapacityException(string message) : DomainException(message);
