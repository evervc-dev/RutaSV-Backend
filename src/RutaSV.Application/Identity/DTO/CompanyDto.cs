namespace RutaSV.Application.Identity.DTO;

public sealed record CompanyDto(
    Guid Id,
    Guid OperatorId,
    string TradeName,
    string? LegalName,
    string? Description,
    string? ContactEmail,
    string? ContactPhone,
    string? Whatsapp,
    string? Website,
    string? LogoUrl,
    int? MunicipalityId,
    string? Address,
    string Status,
    string? RejectionReason,
    Guid? ApprovedBy,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
