namespace RutaSV.Application.Identity.DTO;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Phone = null
);
