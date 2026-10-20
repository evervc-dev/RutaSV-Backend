using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity;

public sealed class InvalidCredentialsException(string message = "Las credenciales proporcionadas son inválidas.")
    : DomainException(message);
