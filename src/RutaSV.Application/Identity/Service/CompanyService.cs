using FluentValidation;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Application.Catalog.Service;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Exception;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Identity.Service;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyDto>> GetMyCompaniesAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<CompanyProfileDto> GetPublicProfileAsync(
        string idOrSlug,
        CancellationToken cancellationToken = default);

    Task<CompanyDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.TradeName)
            .NotEmpty().WithMessage("El nombre comercial es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre comercial no puede superar los 150 caracteres.");

        RuleFor(x => x.LegalName)
            .MaximumLength(200).WithMessage("La razón social no puede superar los 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.LegalName));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("El correo de contacto no tiene un formato válido.")
            .MaximumLength(150).WithMessage("El correo de contacto no puede superar los 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20).WithMessage("El teléfono de contacto no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));

        RuleFor(x => x.Whatsapp)
            .MaximumLength(20).WithMessage("El WhatsApp no puede superar los 20 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Whatsapp));

        RuleFor(x => x.Website)
            .MaximumLength(300).WithMessage("El sitio web no puede superar los 300 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("La URL del logotipo no puede superar los 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl));

        RuleFor(x => x.MunicipalityId)
            .GreaterThan(0).WithMessage("El municipio seleccionado no es válido.")
            .When(x => x.MunicipalityId.HasValue);

        RuleFor(x => x.Address)
            .MaximumLength(300).WithMessage("La dirección no puede superar los 300 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Address));
    }
}

public sealed class CompanyService(
    ICompanyRepository companyRepository,
    ICatalogLookupRepository catalogLookupRepository,
    IExperienceService experienceService,
    IValidator<UpdateCompanyRequest> updateValidator) : ICompanyService
{
    public async Task<IReadOnlyList<CompanyDto>> GetMyCompaniesAsync(
        Guid userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Company> companies = isAdmin
            ? await companyRepository.GetApprovedCompaniesAsync(cancellationToken)
            : await companyRepository.GetByOperatorIdAsync(userId, cancellationToken);

        return companies.Select(MapToDto).ToList();
    }

    public async Task<CompanyProfileDto> GetPublicProfileAsync(
        string idOrSlug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            throw new CompanyNotFoundException(string.Empty);
        }

        string trimmed = idOrSlug.Trim();
        Company? company;

        if (Guid.TryParse(trimmed, out Guid companyId))
        {
            company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
        }
        else
        {
            IReadOnlyList<Company> approved = await companyRepository.GetApprovedCompaniesAsync(cancellationToken);
            string normalizedSlug = trimmed.ToLowerInvariant();
            company = approved.FirstOrDefault(c =>
                Slugify(c.TradeName) == normalizedSlug
                || string.Equals(c.TradeName, trimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (company is null || company.DeletedAt.HasValue || company.Status != CompanyStatus.APROBADA)
        {
            throw new CompanyNotFoundException(trimmed);
        }

        Municipality? municipality = company.Municipality;
        if (municipality is null && company.MunicipalityId.HasValue)
        {
            municipality = await catalogLookupRepository.GetMunicipalityByIdAsync(
                company.MunicipalityId.Value,
                cancellationToken);
        }

        Common.DTO.PagedResponse<ExperienceSummaryDto> publicExperiences = await experienceService.GetPublicPagedAsync(
            new GetPublicExperiencesQuery(PageNumber: 1, PageSize: 50),
            cancellationToken);

        List<ExperienceSummaryDto> companyExperiences = publicExperiences.Data
            .Where(e => e.CompanyId == company.Id)
            .ToList();

        return new CompanyProfileDto(
            company.Id,
            company.OperatorId,
            company.TradeName,
            company.LegalName,
            company.Description,
            company.ContactEmail,
            company.ContactPhone,
            company.Whatsapp,
            company.Website,
            company.LogoUrl,
            company.MunicipalityId,
            municipality?.Name,
            municipality?.DepartmentId,
            municipality?.Department?.Name,
            company.Address,
            company.Status.ToString(),
            company.ApprovedAt,
            company.CreatedAt,
            company.UpdatedAt,
            companyExperiences);
    }

    public async Task<CompanyDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        Company company = await companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new CompanyNotFoundException(companyId);

        if (!isAdmin && company.OperatorId != userId)
        {
            throw new CompanyAccessDeniedException("No tienes permisos para actualizar esta empresa.");
        }

        if (request.MunicipalityId.HasValue)
        {
            _ = await catalogLookupRepository.GetMunicipalityByIdAsync(request.MunicipalityId.Value, cancellationToken)
                ?? throw new InvalidCatalogReferenceException(
                    $"El municipio con identificador '{request.MunicipalityId.Value}' no existe.");
        }

        company.TradeName = request.TradeName.Trim();
        company.LegalName = request.LegalName?.Trim();
        company.Description = request.Description?.Trim();
        company.ContactEmail = request.ContactEmail?.Trim();
        company.ContactPhone = request.ContactPhone?.Trim();
        company.Whatsapp = request.Whatsapp?.Trim();
        company.Website = request.Website?.Trim();
        if (request.LogoUrl is not null)
        {
            company.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        }

        company.MunicipalityId = request.MunicipalityId;
        company.Address = request.Address?.Trim();
        company.UpdatedAt = DateTime.UtcNow;

        await companyRepository.UpdateAsync(company, cancellationToken);
        await companyRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(company);
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto(
            company.Id,
            company.OperatorId,
            company.TradeName,
            company.LegalName,
            company.Description,
            company.ContactEmail,
            company.ContactPhone,
            company.Whatsapp,
            company.Website,
            company.LogoUrl,
            company.MunicipalityId,
            company.Address,
            company.Status.ToString(),
            company.RejectionReason,
            company.ApprovedBy,
            company.ApprovedAt,
            company.CreatedAt,
            company.UpdatedAt);
    }

    private static string Slugify(string value)
    {
        string normalized = value.Trim().ToLowerInvariant();
        System.Text.StringBuilder sb = new();
        bool prevDash = false;
        foreach (char ch in normalized.Normalize(System.Text.NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)
                == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                _ = sb.Append(ch);
                prevDash = false;
            }
            else if (!prevDash && sb.Length > 0)
            {
                _ = sb.Append('-');
                prevDash = true;
            }
        }

        return sb.ToString().Trim('-');
    }
}
