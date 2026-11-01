namespace RutaSV.Application.Identity.DTO;

public sealed record CreateOperatorCompanyRequest(
    string TradeName,
    string? LegalName = null,
    string? Description = null,
    string? ContactEmail = null,
    string? ContactPhone = null,
    string? Whatsapp = null,
    string? Website = null,
    string? LogoUrl = null,
    int? MunicipalityId = null,
    string? Address = null
);
