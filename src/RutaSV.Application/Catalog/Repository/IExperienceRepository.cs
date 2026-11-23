using RutaSV.Application.Catalog.DTO;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Repository;

public interface IExperienceRepository
{
    Task<Experience?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Experience?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludeExperienceId = null, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetPublicPagedAsync(
        GetPublicExperiencesQuery query,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ExperienceStatus? status = null,
        Guid? companyId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Experience experience, CancellationToken cancellationToken = default);
    Task AddCategoriesAsync(IEnumerable<ExperienceCategory> categories, CancellationToken cancellationToken = default);
    Task ReplaceCategoriesAsync(Guid experienceId, IEnumerable<ExperienceCategory> categories, CancellationToken cancellationToken = default);
    Task AddImagesAsync(IEnumerable<ExperienceImage> images, CancellationToken cancellationToken = default);
    Task ReplaceImagesAsync(Guid experienceId, IEnumerable<ExperienceImage> images, CancellationToken cancellationToken = default);
    Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
