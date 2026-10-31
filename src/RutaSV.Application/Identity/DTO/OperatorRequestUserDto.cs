namespace RutaSV.Application.Identity.DTO;

public sealed record OperatorRequestUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    string Role,
    string Status
);
