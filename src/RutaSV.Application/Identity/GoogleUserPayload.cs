namespace RutaSV.Application.Identity;

public sealed record GoogleUserPayload(
    string ProviderId,
    string Email,
    string FirstName,
    string LastName,
    string? PhotoUrl);
