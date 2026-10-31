using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class PendingOperatorRequestAlreadyExistsException(Guid userId)
    : DomainException($"El usuario '{userId}' ya posee una solicitud de operador con estado PENDIENTE.");
