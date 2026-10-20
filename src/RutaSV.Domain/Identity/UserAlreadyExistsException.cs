using RutaSV.Domain.Common;

namespace RutaSV.Domain.Identity;

public sealed class UserAlreadyExistsException(string email)
    : DomainException($"Ya existe un usuario registrado con el correo '{email}'.");
