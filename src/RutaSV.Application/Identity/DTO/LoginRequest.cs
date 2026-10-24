namespace RutaSV.Application.Identity.DTO;

public sealed record LoginRequest(
    string Email,
    string Password
);
