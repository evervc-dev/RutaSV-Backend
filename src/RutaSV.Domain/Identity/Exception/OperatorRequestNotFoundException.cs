using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class OperatorRequestNotFoundException(Guid requestId)
    : DomainException($"No se encontró la solicitud de operador con identificador '{requestId}'.");
