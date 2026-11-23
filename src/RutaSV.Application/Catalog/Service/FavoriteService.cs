using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Application.Catalog.Service;

public interface IFavoriteService
{
    Task<IReadOnlyList<FavoriteItemDto>> GetMyFavoritesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetMyFavoriteIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddFavoriteAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);
    Task RemoveFavoriteAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default);
}

public sealed class FavoriteService(
    IFavoriteRepository favoriteRepository,
    IExperienceRepository experienceRepository) : IFavoriteService
{
    public async Task<IReadOnlyList<FavoriteItemDto>> GetMyFavoritesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Favorite> favorites = await favoriteRepository.GetByUserIdAsync(userId, cancellationToken);

        List<FavoriteItemDto> items = [];
        foreach (Favorite fav in favorites)
        {
            Experience? experience = fav.Experience
                ?? await experienceRepository.GetByIdWithDetailsAsync(fav.ExperienceId, cancellationToken);

            if (experience is null
                || experience.DeletedAt.HasValue
                || experience.Status != ExperienceStatus.PUBLICADA
                || experience.Company?.Status != CompanyStatus.APROBADA)
            {
                continue;
            }

            items.Add(new FavoriteItemDto(
                fav.ExperienceId,
                fav.CreatedAt,
                MapSummary(experience)));
        }

        return items;
    }

    public Task<IReadOnlyList<Guid>> GetMyFavoriteIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return favoriteRepository.GetExperienceIdsByUserIdAsync(userId, cancellationToken);
    }

    public async Task AddFavoriteAsync(
        Guid userId,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        Experience experience = await experienceRepository.GetByIdWithDetailsAsync(experienceId, cancellationToken)
            ?? throw new ExperienceNotFoundException(experienceId);

        if (experience.DeletedAt.HasValue || experience.Status != ExperienceStatus.PUBLICADA)
        {
            throw new ExperienceNotFoundException(experienceId);
        }

        bool exists = await favoriteRepository.ExistsAsync(userId, experienceId, cancellationToken);
        if (exists)
        {
            return;
        }

        await favoriteRepository.AddAsync(new Favorite
        {
            UserId = userId,
            ExperienceId = experienceId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await favoriteRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFavoriteAsync(
        Guid userId,
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        await favoriteRepository.RemoveAsync(userId, experienceId, cancellationToken);
        await favoriteRepository.SaveChangesAsync(cancellationToken);
    }

    private static ExperienceSummaryDto MapSummary(Experience experience)
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

        CategoryDto? primaryCategory = categories.FirstOrDefault(c => c.IsPrimary) ?? categories.FirstOrDefault();

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
}
