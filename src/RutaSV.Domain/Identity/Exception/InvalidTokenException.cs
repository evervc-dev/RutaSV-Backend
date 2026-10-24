using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity.Exception;

public sealed class InvalidTokenException(string message = "El token proporcionado es inválido o ha expirado.")
    : DomainException(message);
