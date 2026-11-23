using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using FluentValidation;

using RutaSV.Application.Bookings.Service;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Application.Catalog.Service;

public sealed partial class ExperienceService(
    IExperienceRepository experienceRepository,
    ICatalogLookupRepository catalogLookupRepository,
    ICompanyRepository companyRepository,
    IValidator<CreateExperienceRequest> createValidator,
    IValidator<UpdateExperienceRequest> updateValidator,
    IValidator<GetPublicExperiencesQuery> publicQueryValidator,
    IValidator<GetMyExperiencesQuery> myQueryValidator,
    IReservationIncidentCoordinator? incidentCoordinator = null) : IExperienceService
{
    private const decimal ElSalvadorMinLatitude = 13.0m;
    private const decimal ElSalvadorMaxLatitude = 14.6m;
    private const decimal ElSalvadorMinLongitude = -90.3m;
    private const decimal ElSalvadorMaxLongitude = -87.5m;
    private const int MinPublicationDescriptionLength = 20;

    public async Task<ExperienceDto> CreateAsync(
        Guid userId,
        bool isAdmin,
        CreateExperienceRequest request,
        CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        Company company = await ResolveAuthorizedCompanyAsync(
            userId,
            isAdmin,
            request.CompanyId,
            cancellationToken);

        Municipality municipality = await catalogLookupRepository.GetMunicipalityByIdAsync(
            request.MunicipalityId,
            cancellationToken)
            ?? throw new InvalidCatalogReferenceException($"El municipio con identificador '{request.MunicipalityId}' no existe.");

        IReadOnlyList<Category> validCategories = await ResolveValidCategoriesAsync(
            request.CategoryIds,
            cancellationToken);

        Guid experienceId = Guid.NewGuid();
        string slug = await ResolveUniqueSlugAsync(
            request.Slug,
            request.Title,
            experienceId,
            excludeExperienceId: null,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        Experience experience = new()
        {
            Id = experienceId,
            CompanyId = company.Id,
            Title = request.Title.Trim(),
            Slug = slug,
            ShortDescription = request.ShortDescription.Trim(),
            Description = request.Description.Trim(),
            Includes = request.Includes?.Trim(),
            Excludes = request.Excludes?.Trim(),
            WhatToBring = request.WhatToBring?.Trim(),
            Price = decimal.Round(request.Price, 2),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            DurationMinutes = request.DurationMinutes,
            Difficulty = ParseDifficulty(request.Difficulty),
            MinAge = request.MinAge,
            ServiceLanguage = request.ServiceLanguage?.Trim(),
            HasLimitedCapacity = request.HasLimitedCapacity,
            MinPeople = request.MinPeople,
            MaxPeople = request.HasLimitedCapacity ? request.MaxPeople : null,
            MunicipalityId = municipality.Id,
            ReferenceAddress = request.ReferenceAddress?.Trim(),
            Latitude = decimal.Round(request.Latitude, 6),
            Longitude = decimal.Round(request.Longitude, 6),
            AutoConfirmation = request.AutoConfirmation,
            Status = ExperienceStatus.BORRADOR,
            AverageRating = 0m,
            TotalReviews = 0,
            CreatedAt = now,
            UpdatedAt = now,
            Company = company,
            Municipality = municipality
        };

        List<ExperienceCategory> experienceCategories = BuildExperienceCategories(
            experience.Id,
            validCategories,
            request.PrimaryCategoryId);

        List<ExperienceImage> experienceImages = BuildExperienceImages(
            experience.Id,
            request.Images,
            now);

        if (request.PublishImmediately)
        {
            ValidateForPublication(experience, municipality, experienceCategories, experienceImages);
            experience.Status = ExperienceStatus.PUBLICADA;
            experience.PublishedAt = now;
        }

        await experienceRepository.ExecuteInTransactionAsync(async ct =>
        {
            await experienceRepository.AddAsync(experience, ct);

            if (experienceCategories.Count > 0)
            {
                await experienceRepository.AddCategoriesAsync(experienceCategories, ct);
            }

            if (experienceImages.Count > 0)
            {
                await experienceRepository.AddImagesAsync(experienceImages, ct);
            }
        }, cancellationToken);

        experience.Categories = experienceCategories;
        experience.Images = experienceImages;

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> UpdateAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        UpdateExperienceRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(
            experienceId,
            cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManageExperience(experience, userId, isAdmin);

        if (experience.Status is ExperienceStatus.ARCHIVADA or ExperienceStatus.RETIRADA)
        {
            throw new ExperienceInvalidStateTransitionException(
                $"No se puede editar una experiencia en estado '{experience.Status}'.");
        }

        Municipality municipality = await catalogLookupRepository.GetMunicipalityByIdAsync(
            request.MunicipalityId,
            cancellationToken)
            ?? throw new InvalidCatalogReferenceException($"El municipio con identificador '{request.MunicipalityId}' no existe.");

        IReadOnlyList<Category> validCategories = await ResolveValidCategoriesAsync(
            request.CategoryIds,
            cancellationToken);

        string slug = await ResolveUniqueSlugAsync(
            request.Slug,
            request.Title,
            experience.Id,
            excludeExperienceId: experience.Id,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        experience.Title = request.Title.Trim();
        experience.Slug = slug;
        experience.ShortDescription = request.ShortDescription.Trim();
        experience.Description = request.Description.Trim();
        experience.Includes = request.Includes?.Trim();
        experience.Excludes = request.Excludes?.Trim();
        experience.WhatToBring = request.WhatToBring?.Trim();
        experience.Price = decimal.Round(request.Price, 2);
        experience.Currency = request.Currency.Trim().ToUpperInvariant();
        experience.DurationMinutes = request.DurationMinutes;
        experience.Difficulty = ParseDifficulty(request.Difficulty);
        experience.MinAge = request.MinAge;
        experience.ServiceLanguage = request.ServiceLanguage?.Trim();
        experience.HasLimitedCapacity = request.HasLimitedCapacity;
        experience.MinPeople = request.MinPeople;
        experience.MaxPeople = request.HasLimitedCapacity ? request.MaxPeople : null;
        experience.MunicipalityId = municipality.Id;
        experience.Municipality = municipality;
        experience.ReferenceAddress = request.ReferenceAddress?.Trim();
        experience.Latitude = decimal.Round(request.Latitude, 6);
        experience.Longitude = decimal.Round(request.Longitude, 6);
        experience.AutoConfirmation = request.AutoConfirmation;
        experience.UpdatedAt = now;

        List<ExperienceCategory> updatedCategories = request.CategoryIds is not null
            ? BuildExperienceCategories(experience.Id, validCategories, request.PrimaryCategoryId)
            : experience.Categories.ToList();

        List<ExperienceImage> updatedImages = request.Images is not null
            ? BuildExperienceImages(experience.Id, request.Images, now)
            : experience.Images.Where(i => i.RemovedAt == null).ToList();

        if (experience.Status == ExperienceStatus.PUBLICADA)
        {
            ValidateForPublication(experience, municipality, updatedCategories, updatedImages);
        }

        await experienceRepository.ExecuteInTransactionAsync(async ct =>
        {
            await experienceRepository.UpdateAsync(experience, ct);

            if (request.CategoryIds is not null)
            {
                await experienceRepository.ReplaceCategoriesAsync(experience.Id, updatedCategories, ct);
            }

            if (request.Images is not null)
            {
                await experienceRepository.ReplaceImagesAsync(experience.Id, updatedImages, ct);
            }
        }, cancellationToken);

        experience.Categories = updatedCategories;
        experience.Images = updatedImages;

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> PublishAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(
            experienceId,
            cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManageExperience(experience, userId, isAdmin);

        if (experience.Status is ExperienceStatus.ARCHIVADA or ExperienceStatus.RETIRADA)
        {
            throw new ExperienceInvalidStateTransitionException(
                $"No se puede publicar una experiencia en estado '{experience.Status}'.");
        }

        if (experience.Company?.Status != CompanyStatus.APROBADA)
        {
            throw new ExperienceAccessDeniedException(
                "No se puede publicar la experiencia porque la empresa asociada no se encuentra APROBADA.");
        }

        Municipality? municipality = experience.Municipality
            ?? await catalogLookupRepository.GetMunicipalityByIdAsync(experience.MunicipalityId, cancellationToken);

        List<ExperienceImage> activeImages = experience.Images
            .Where(i => i.RemovedAt == null)
            .ToList();

        ValidateForPublication(experience, municipality, experience.Categories.ToList(), activeImages);

        DateTime now = DateTime.UtcNow;
        experience.Status = ExperienceStatus.PUBLICADA;
        experience.PublishedAt ??= now;
        experience.UpdatedAt = now;

        await experienceRepository.UpdateAsync(experience, cancellationToken);
        await experienceRepository.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> PauseAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(
            experienceId,
            cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManageExperience(experience, userId, isAdmin);

        if (experience.Status != ExperienceStatus.PUBLICADA)
        {
            throw new ExperienceInvalidStateTransitionException(
                "Solo se pueden pausar experiencias que se encuentren en estado PUBLICADA.");
        }

        experience.Status = ExperienceStatus.PAUSADA;
        experience.UpdatedAt = DateTime.UtcNow;

        await experienceRepository.UpdateAsync(experience, cancellationToken);
        await experienceRepository.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(experience);
    }

    public async Task DeleteAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(
            experienceId,
            cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManageExperience(experience, userId, isAdmin);

        DateTime now = DateTime.UtcNow;
        experience.Status = ExperienceStatus.ARCHIVADA;
        experience.DeletedAt = now;
        experience.UpdatedAt = now;

        await experienceRepository.UpdateAsync(experience, cancellationToken);
        await experienceRepository.SaveChangesAsync(cancellationToken);

        if (incidentCoordinator is not null)
        {
            await incidentCoordinator.HandleExperienceRemovedAsync(
                experience,
                IncidentCause.EXPERIENCIA_ELIMINADA,
                userId,
                "La experiencia fue archivada por el operador.",
                cancellationToken);
        }
    }

    public async Task<ExperienceDto> WithdrawAsync(
        Guid adminUserId,
        Guid experienceId,
        WithdrawExperienceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ExperienceInvalidStateTransitionException("Debes indicar un motivo para retirar la experiencia.");
        }

        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        if (experience.Status == ExperienceStatus.ARCHIVADA)
        {
            throw new ExperienceInvalidStateTransitionException("No se puede retirar una experiencia archivada.");
        }

        DateTime now = DateTime.UtcNow;
        experience.Status = ExperienceStatus.RETIRADA;
        experience.WithdrawnBy = adminUserId;
        experience.WithdrawnAt = now;
        experience.WithdrawalReason = request.Reason.Trim();
        experience.UpdatedAt = now;

        await experienceRepository.UpdateAsync(experience, cancellationToken);
        await experienceRepository.SaveChangesAsync(cancellationToken);

        if (incidentCoordinator is not null)
        {
            await incidentCoordinator.HandleExperienceRemovedAsync(
                experience,
                IncidentCause.EXPERIENCIA_RETIRADA,
                adminUserId,
                experience.WithdrawalReason,
                cancellationToken);
        }

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> RestoreAsync(
        Guid adminUserId,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        if (experience.Status != ExperienceStatus.RETIRADA)
        {
            throw new ExperienceInvalidStateTransitionException("Solo se pueden restaurar experiencias en estado RETIRADA.");
        }

        _ = adminUserId;
        experience.Status = ExperienceStatus.PUBLICADA;
        experience.WithdrawnBy = null;
        experience.WithdrawnAt = null;
        experience.WithdrawalReason = null;
        experience.UpdatedAt = DateTime.UtcNow;

        await experienceRepository.UpdateAsync(experience, cancellationToken);
        await experienceRepository.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> RemoveImageAsync(
        Guid userId,
        bool isAdmin,
        Guid experienceId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        EnsureCanManageExperience(experience, userId, isAdmin);

        List<ExperienceImage> activeImages = experience.Images
            .Where(i => i.RemovedAt == null)
            .OrderBy(i => i.SortOrder)
            .ToList();

        ExperienceImage? target = activeImages.FirstOrDefault(i => i.Id == imageId);
        if (target is null)
        {
            throw new ExperienceNotFoundException($"Imagen '{imageId}' no encontrada en la experiencia.");
        }

        if (experience.Status == ExperienceStatus.PUBLICADA && activeImages.Count <= 1)
        {
            throw new ExperienceInvalidStateTransitionException(
                "No puedes eliminar la última fotografía de una experiencia publicada.");
        }

        List<ExperienceImage> remaining = activeImages
            .Where(i => i.Id != imageId)
            .Select((img, idx) => new ExperienceImage
            {
                Id = Guid.NewGuid(),
                ExperienceId = experience.Id,
                ObjectKey = img.ObjectKey,
                Url = img.Url,
                ThumbnailUrl = img.ThumbnailUrl,
                AltText = img.AltText,
                SortOrder = (short)idx,
                IsCover = img.IsCover,
                CreatedAt = img.CreatedAt
            })
            .ToList();

        if (remaining.Count > 0 && !remaining.Any(i => i.IsCover))
        {
            remaining[0].IsCover = true;
        }

        experience.Images = remaining;
        experience.UpdatedAt = DateTime.UtcNow;

        await experienceRepository.ExecuteInTransactionAsync(async ct =>
        {
            await experienceRepository.ReplaceImagesAsync(experience.Id, remaining, ct);
            await experienceRepository.UpdateAsync(experience, ct);
        }, cancellationToken);

        return MapToDetailDto(experience);
    }

    public async Task<ExperienceDto> GetByIdOrSlugAsync(
        string slugOrId,
        Guid? currentUserId = null,
        bool isAdmin = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slugOrId))
        {
            throw new ExperienceNotFoundException(string.Empty);
        }

        string trimmed = slugOrId.Trim();
        Experience? experience = Guid.TryParse(trimmed, out Guid parsedId)
            ? await experienceRepository.GetByIdWithDetailsAsync(parsedId, cancellationToken)
            : await experienceRepository.GetBySlugWithDetailsAsync(trimmed.ToLowerInvariant(), cancellationToken);

        if (experience is null || experience.DeletedAt.HasValue)
        {
            throw new ExperienceNotFoundException(trimmed);
        }

        bool isOwner = currentUserId.HasValue && experience.Company?.OperatorId == currentUserId.Value;

        if (!isAdmin && !isOwner && !IsPubliclyVisible(experience))
        {
            throw new ExperienceNotFoundException(trimmed);
        }

        return MapToDetailDto(experience);
    }

    public async Task<PagedResponse<ExperienceSummaryDto>> GetPublicPagedAsync(
        GetPublicExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        await publicQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        (IReadOnlyList<Experience> items, int totalRecords) = await experienceRepository.GetPublicPagedAsync(
            query,
            cancellationToken);

        List<ExperienceSummaryDto> dtos = items
            .Select(MapToSummaryDto)
            .ToList();

        return PagedResponse<ExperienceSummaryDto>.Create(
            dtos,
            totalRecords,
            query.PageNumber,
            query.PageSize);
    }

    public async Task<PagedResponse<ExperienceDto>> GetMyPagedAsync(
        Guid userId,
        bool isAdmin,
        GetMyExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        await myQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        ExperienceStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            statusFilter = Enum.Parse<ExperienceStatus>(query.Status.Trim(), ignoreCase: true);
        }

        Guid? operatorFilter = isAdmin ? null : userId;

        (IReadOnlyList<Experience> items, int totalRecords) = await experienceRepository.GetByOperatorPagedAsync(
            operatorFilter,
            query.PageNumber,
            query.PageSize,
            statusFilter,
            query.CompanyId,
            cancellationToken);

        List<ExperienceDto> dtos = items
            .Select(MapToDetailDto)
            .ToList();

        return PagedResponse<ExperienceDto>.Create(
            dtos,
            totalRecords,
            query.PageNumber,
            query.PageSize);
    }

    private async Task<Company> ResolveAuthorizedCompanyAsync(
        Guid userId,
        bool isAdmin,
        Guid? requestedCompanyId,
        CancellationToken cancellationToken)
    {
        if (requestedCompanyId.HasValue)
        {
            Company company = await companyRepository.GetByIdAsync(requestedCompanyId.Value, cancellationToken)
                ?? throw new ExperienceAccessDeniedException("La empresa especificada no existe.");

            if (!isAdmin && company.OperatorId != userId)
            {
                throw new ExperienceAccessDeniedException("No tienes permisos para gestionar experiencias de esta empresa.");
            }

            if (company.Status != CompanyStatus.APROBADA)
            {
                throw new ExperienceAccessDeniedException("La empresa debe estar APROBADA para registrar experiencias.");
            }

            return company;
        }

        IReadOnlyList<Company> operatorCompanies = await companyRepository.GetByOperatorIdAsync(userId, cancellationToken);
        Company? approvedCompany = operatorCompanies.FirstOrDefault(c => c.Status == CompanyStatus.APROBADA);

        if (approvedCompany is null)
        {
            throw new ExperienceAccessDeniedException(
                "No tienes ninguna empresa APROBADA asociada a tu cuenta de operador.");
        }

        return approvedCompany;
    }

    private static void EnsureCanManageExperience(Experience experience, Guid userId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (experience.Company is null || experience.Company.OperatorId != userId)
        {
            throw new ExperienceAccessDeniedException(
                "No tienes permisos para modificar o eliminar esta experiencia.");
        }
    }

    private static bool IsPubliclyVisible(Experience experience)
    {
        return experience.DeletedAt == null
            && experience.Status == ExperienceStatus.PUBLICADA
            && experience.Company is not null
            && experience.Company.DeletedAt == null
            && experience.Company.Status == CompanyStatus.APROBADA
            && (experience.Company.Operator is null || experience.Company.Operator.Status == UserStatus.ACTIVO);
    }

    private async Task<IReadOnlyList<Category>> ResolveValidCategoriesAsync(
        IReadOnlyList<int>? categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return [];
        }

        List<int> distinctIds = categoryIds.Distinct().ToList();
        IReadOnlyList<Category> categories = await catalogLookupRepository.GetCategoriesByIdsAsync(
            distinctIds,
            cancellationToken);

        if (categories.Count != distinctIds.Count || categories.Any(c => !c.IsActive))
        {
            throw new InvalidCatalogReferenceException("Una o más categorías seleccionadas no existen o no se encuentran activas.");
        }

        return categories;
    }

    private async Task<string> ResolveUniqueSlugAsync(
        string? customSlug,
        string title,
        Guid experienceId,
        Guid? excludeExperienceId,
        CancellationToken cancellationToken)
    {
        bool hasCustomSlug = !string.IsNullOrWhiteSpace(customSlug);
        string baseSlug = GenerateSlug(hasCustomSlug ? customSlug! : title);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = $"experiencia-{experienceId.ToString("N")[..8]}";
        }

        bool exists = await experienceRepository.ExistsBySlugAsync(baseSlug, excludeExperienceId, cancellationToken);
        if (!exists)
        {
            return baseSlug;
        }

        if (hasCustomSlug)
        {
            throw new ExperienceSlugAlreadyExistsException(baseSlug);
        }

        string suffix = experienceId.ToString("N")[..6];
        string maxBase = baseSlug.Length > 170 ? baseSlug[..170].TrimEnd('-') : baseSlug;
        return $"{maxBase}-{suffix}";
    }

    private static List<ExperienceCategory> BuildExperienceCategories(
        Guid experienceId,
        IReadOnlyList<Category> categories,
        int? primaryCategoryId)
    {
        if (categories.Count == 0)
        {
            return [];
        }

        int resolvedPrimaryId = primaryCategoryId.HasValue && categories.Any(c => c.Id == primaryCategoryId.Value)
            ? primaryCategoryId.Value
            : categories[0].Id;

        return categories
            .Select(c => new ExperienceCategory
            {
                ExperienceId = experienceId,
                CategoryId = c.Id,
                IsPrimary = c.Id == resolvedPrimaryId,
                Category = c
            })
            .ToList();
    }

    private static List<ExperienceImage> BuildExperienceImages(
        Guid experienceId,
        IReadOnlyList<CreateExperienceImageRequest>? imageRequests,
        DateTime now)
    {
        if (imageRequests is null || imageRequests.Count == 0)
        {
            return [];
        }

        List<ExperienceImage> images = imageRequests
            .OrderBy(i => i.SortOrder)
            .Select((img, index) =>
            {
                string objectKey = img.ObjectKey?.Trim() ?? string.Empty;
                string url = !string.IsNullOrWhiteSpace(img.Url)
                    ? img.Url.Trim()
                    : !string.IsNullOrWhiteSpace(objectKey)
                        ? $"/api/experiences/images/file?objectKey={Uri.EscapeDataString(objectKey)}"
                        : string.Empty;

                return new ExperienceImage
                {
                    Id = Guid.NewGuid(),
                    ExperienceId = experienceId,
                    ObjectKey = objectKey,
                    Url = url,
                    ThumbnailUrl = img.ThumbnailUrl?.Trim(),
                    AltText = img.AltText?.Trim(),
                    SortOrder = img.SortOrder > 0 ? img.SortOrder : (short)index,
                    IsCover = img.IsCover,
                    CreatedAt = now
                };
            })
            .ToList();

        if (!images.Any(i => i.IsCover))
        {
            images[0].IsCover = true;
        }

        return images;
    }

    private static void ValidateForPublication(
        Experience experience,
        Municipality? municipality,
        IReadOnlyCollection<ExperienceCategory> categories,
        IReadOnlyCollection<ExperienceImage> activeImages)
    {
        List<string> missingFields = [];

        if (string.IsNullOrWhiteSpace(experience.Description)
            || experience.Description.Trim().Length < MinPublicationDescriptionLength)
        {
            missingFields.Add("Descripción detallada (mínimo 20 caracteres)");
        }

        if (categories.Count == 0)
        {
            missingFields.Add("Al menos una categoría");
        }

        if (municipality is null || experience.MunicipalityId <= 0)
        {
            missingFields.Add("Municipio válido");
        }

        if (!IsWithinElSalvadorBounds(experience.Latitude, experience.Longitude))
        {
            missingFields.Add("Ubicación válida dentro de El Salvador (latitud entre 13.0 y 14.6, longitud entre -90.3 y -87.5)");
        }

        if (experience.Price < 0)
        {
            missingFields.Add("Precio válido");
        }

        if (experience.MinPeople < 1
            || (experience.HasLimitedCapacity && (!experience.MaxPeople.HasValue || experience.MaxPeople.Value < experience.MinPeople)))
        {
            missingFields.Add("Configuración de capacidad válida (cupo mínimo y máximo)");
        }

        if (!activeImages.Any(i => i.RemovedAt == null && i.IsCover))
        {
            missingFields.Add("Al menos una fotografía con imagen de portada (isCover = true)");
        }

        if (missingFields.Count > 0)
        {
            throw new ExperienceIncompleteForPublicationException(missingFields);
        }
    }

    private static bool IsWithinElSalvadorBounds(decimal latitude, decimal longitude)
    {
        return latitude is >= ElSalvadorMinLatitude and <= ElSalvadorMaxLatitude
            && longitude is >= ElSalvadorMinLongitude and <= ElSalvadorMaxLongitude;
    }

    private static DifficultyLevel? ParseDifficulty(string? difficulty)
    {
        return string.IsNullOrWhiteSpace(difficulty)
            ? null
            : Enum.Parse<DifficultyLevel>(difficulty.Trim(), ignoreCase: true);
    }

    private static ExperienceSummaryDto MapToSummaryDto(Experience experience)
    {
        List<CategoryDto> categories = experience.Categories
            .Where(ec => ec.Category is not null)
            .OrderByDescending(ec => ec.IsPrimary)
            .ThenBy(ec => ec.Category!.SortOrder)
            .Select(ec => new CategoryDto(
                ec.Category!.Id,
                ec.Category.Name,
                ec.Category.Slug,
                ec.Category.Description,
                ec.Category.Icon,
                ec.Category.SortOrder,
                ec.IsPrimary))
            .ToList();

        CategoryDto? primaryCategory = categories.FirstOrDefault(c => c.IsPrimary)
            ?? categories.FirstOrDefault();

        ExperienceImage? coverImage = experience.Images
            .Where(i => i.RemovedAt == null)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .FirstOrDefault();

        return new ExperienceSummaryDto(
            experience.Id,
            experience.CompanyId,
            experience.Company?.TradeName ?? string.Empty,
            experience.Title,
            experience.Slug,
            experience.ShortDescription,
            experience.Price,
            experience.Currency,
            experience.DurationMinutes,
            experience.Difficulty?.ToString(),
            experience.HasLimitedCapacity,
            experience.MinPeople,
            experience.MaxPeople,
            experience.MunicipalityId,
            experience.Municipality?.Name ?? string.Empty,
            experience.Municipality?.DepartmentId ?? 0,
            experience.Municipality?.Department?.Name ?? string.Empty,
            experience.Latitude,
            experience.Longitude,
            coverImage?.Url,
            primaryCategory,
            categories,
            experience.AverageRating,
            experience.TotalReviews,
            experience.Status.ToString(),
            experience.PublishedAt,
            experience.CreatedAt);
    }

    private static ExperienceDto MapToDetailDto(Experience experience)
    {
        Company company = experience.Company ?? new Company { Id = experience.CompanyId };

        ExperienceCompanyDto companyDto = new(
            company.Id,
            company.OperatorId,
            company.TradeName,
            company.Description,
            company.ContactEmail,
            company.ContactPhone,
            company.Whatsapp,
            company.Website,
            company.LogoUrl,
            company.Status.ToString());

        List<CategoryDto> categories = experience.Categories
            .Where(ec => ec.Category is not null)
            .OrderByDescending(ec => ec.IsPrimary)
            .ThenBy(ec => ec.Category!.SortOrder)
            .Select(ec => new CategoryDto(
                ec.Category!.Id,
                ec.Category.Name,
                ec.Category.Slug,
                ec.Category.Description,
                ec.Category.Icon,
                ec.Category.SortOrder,
                ec.IsPrimary))
            .ToList();

        List<ExperienceImageDto> images = experience.Images
            .Where(i => i.RemovedAt == null)
            .OrderByDescending(i => i.IsCover)
            .ThenBy(i => i.SortOrder)
            .Select(i => new ExperienceImageDto(
                i.Id,
                i.ObjectKey,
                i.Url,
                i.ThumbnailUrl,
                i.AltText,
                i.SortOrder,
                i.IsCover,
                i.CreatedAt))
            .ToList();

        return new ExperienceDto(
            experience.Id,
            experience.CompanyId,
            experience.Title,
            experience.Slug,
            experience.ShortDescription,
            experience.Description,
            experience.Includes,
            experience.Excludes,
            experience.WhatToBring,
            experience.Price,
            experience.Currency,
            experience.DurationMinutes,
            experience.Difficulty?.ToString(),
            experience.MinAge,
            experience.ServiceLanguage,
            experience.HasLimitedCapacity,
            experience.MinPeople,
            experience.MaxPeople,
            experience.MunicipalityId,
            experience.Municipality?.Name ?? string.Empty,
            experience.Municipality?.DepartmentId ?? 0,
            experience.Municipality?.Department?.Name ?? string.Empty,
            experience.ReferenceAddress,
            experience.Latitude,
            experience.Longitude,
            experience.AutoConfirmation,
            experience.Status.ToString(),
            experience.WithdrawalReason,
            experience.AverageRating,
            experience.TotalReviews,
            experience.PublishedAt,
            experience.CreatedAt,
            experience.UpdatedAt,
            companyDto,
            categories,
            images);
    }

    public static string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder sb = new(normalized.Length);

        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                _ = sb.Append(c);
            }
        }

        string clean = sb.ToString().Normalize(NormalizationForm.FormC);
        clean = NonAlphanumericRegex().Replace(clean, "-");
        clean = MultipleHyphensRegex().Replace(clean, "-").Trim('-');

        return clean.Length > 180 ? clean[..180].TrimEnd('-') : clean;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("-{2,}")]
    private static partial Regex MultipleHyphensRegex();
}
