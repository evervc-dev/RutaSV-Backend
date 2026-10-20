namespace RutaSV.Application.Identity;

public sealed record LoginRequest(
    string Email,
    string Password);
