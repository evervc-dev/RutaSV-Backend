using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class OperatorRequestAlreadyReviewedException(Guid requestId, string currentStatus)
    : DomainException($"La solicitud de operador '{requestId}' ya fue procesada (estado actual: '{currentStatus}').");
