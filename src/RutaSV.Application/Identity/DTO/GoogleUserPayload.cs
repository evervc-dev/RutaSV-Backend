namespace RutaSV.Application.Identity.DTO;

public sealed record GoogleUserPayload(
    string ProviderId,
    string Email,
    string FirstName,
    string LastName,
    string? PhotoUrl
);
