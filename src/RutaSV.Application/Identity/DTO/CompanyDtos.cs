using RutaSV.Application.Catalog.DTO;

namespace RutaSV.Application.Identity.DTO;

public sealed record UpdateCompanyRequest(
    string TradeName,
    string? LegalName = null,
    string? Description = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? Whatsapp = null,
    string? Website = null,
    string? LogoUrl = null,
    int? MunicipalityId = null,
    string? Address = null);

public sealed record CompanyProfileDto(
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
    string? MunicipalityName,
    short? DepartmentId,
    string? DepartmentName,
    string? Address,
    string Status,
    DateTime? ApprovedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ExperienceSummaryDto> Experiences);
