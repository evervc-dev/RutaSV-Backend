namespace RutaSV.Application.Identity.DTO;

public sealed record AuthResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool EmailVerified,
    string Token // Token de autenticación generado para el usuario
);
