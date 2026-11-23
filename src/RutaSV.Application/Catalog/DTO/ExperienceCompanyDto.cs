namespace RutaSV.Application.Catalog.DTO;

public sealed record ExperienceCompanyDto(
    Guid Id,
    Guid OperatorId,
    string TradeName,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Whatsapp,
    string? Website,
    string? LogoUrl,
    string Status);
