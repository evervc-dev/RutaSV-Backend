using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class ExperienceRepository(ApplicationDbContext dbContext) : IExperienceRepository
{
    public async Task<Experience?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Experiences
            .Include(e => e.Company)
                .ThenInclude(c => c!.Operator)
            .Include(e => e.Municipality)
                .ThenInclude(m => m!.Department)
            .Include(e => e.Categories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.Images.Where(i => i.RemovedAt == null))
            .FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null, cancellationToken);
    }

    public async Task<Experience?> GetBySlugWithDetailsAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Experiences
            .Include(e => e.Company)
                .ThenInclude(c => c!.Operator)
            .Include(e => e.Municipality)
                .ThenInclude(m => m!.Department)
            .Include(e => e.Categories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.Images.Where(i => i.RemovedAt == null))
            .FirstOrDefaultAsync(e => e.Slug == slug && e.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeExperienceId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Experience> query = dbContext.Experiences
            .Where(e => e.Slug == slug);

        if (excludeExperienceId.HasValue)
        {
            query = query.Where(e => e.Id != excludeExperienceId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetPublicPagedAsync(
        GetPublicExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Experience> dbQuery = dbContext.Experiences
            .AsNoTracking()
            .Include(e => e.Company)
                .ThenInclude(c => c!.Operator)
            .Include(e => e.Municipality)
                .ThenInclude(m => m!.Department)
            .Include(e => e.Categories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.Images.Where(i => i.RemovedAt == null))
            .Where(e =>
                e.DeletedAt == null
                && e.Status == ExperienceStatus.PUBLICADA
                && e.Company != null
                && e.Company.DeletedAt == null
                && e.Company.Status == CompanyStatus.APROBADA
                && (e.Company.Operator == null || e.Company.Operator.Status == UserStatus.ACTIVO));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search.Trim()}%";
            dbQuery = dbQuery.Where(e =>
                EF.Functions.ILike(e.Title, pattern)
                || EF.Functions.ILike(e.ShortDescription, pattern)
                || EF.Functions.ILike(e.Description, pattern));
        }

        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Categories.Any(ec => ec.CategoryId == query.CategoryId.Value));
        }

        if (query.MunicipalityId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.MunicipalityId == query.MunicipalityId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Municipality != null && e.Municipality.DepartmentId == query.DepartmentId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Price <= query.MaxPrice.Value);
        }

        if (query.MinRating.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.AverageRating >= query.MinRating.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Difficulty)
            && Enum.TryParse<DifficultyLevel>(query.Difficulty.Trim(), ignoreCase: true, out DifficultyLevel parsedDiff))
        {
            dbQuery = dbQuery.Where(e => e.Difficulty == parsedDiff);
        }

        if (query.MinDurationMinutes.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.DurationMinutes >= query.MinDurationMinutes.Value);
        }

        if (query.MaxDurationMinutes.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.DurationMinutes <= query.MaxDurationMinutes.Value);
        }

        if (query.HasLimitedCapacity.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.HasLimitedCapacity == query.HasLimitedCapacity.Value);
        }

        int totalRecords = await dbQuery.CountAsync(cancellationToken);

        string sort = query.SortBy?.Trim().ToLowerInvariant() ?? "recent";
        dbQuery = sort switch
        {
            "price_asc" => dbQuery.OrderBy(e => e.Price).ThenByDescending(e => e.PublishedAt),
            "price_desc" => dbQuery.OrderByDescending(e => e.Price).ThenByDescending(e => e.PublishedAt),
            "rating" => dbQuery.OrderByDescending(e => e.AverageRating).ThenByDescending(e => e.TotalReviews),
            _ => dbQuery.OrderByDescending(e => e.PublishedAt ?? e.CreatedAt)
        };

        List<Experience> items = await dbQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalRecords);
    }

    public async Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ExperienceStatus? status = null,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Experience> dbQuery = dbContext.Experiences
            .AsNoTracking()
            .Include(e => e.Company)
            .Include(e => e.Municipality)
                .ThenInclude(m => m!.Department)
            .Include(e => e.Categories)
                .ThenInclude(ec => ec.Category)
            .Include(e => e.Images.Where(i => i.RemovedAt == null))
            .Where(e => e.DeletedAt == null);

        if (operatorId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Company != null && e.Company.OperatorId == operatorId.Value);
        }

        if (companyId.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            dbQuery = dbQuery.Where(e => e.Status == status.Value);
        }

        int totalRecords = await dbQuery.CountAsync(cancellationToken);

        List<Experience> items = await dbQuery
            .OrderByDescending(e => e.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalRecords);
    }

    public Task AddAsync(
        Experience experience,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Entry(experience).State = EntityState.Added;
        return Task.CompletedTask;
    }

    public Task AddCategoriesAsync(
        IEnumerable<ExperienceCategory> categories,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<ExperienceCategory> snapshot = categories.ToList();
        foreach (ExperienceCategory c in snapshot)
        {
            ExperienceCategory detached = new()
            {
                ExperienceId = c.ExperienceId,
                CategoryId = c.CategoryId,
                IsPrimary = c.IsPrimary
            };
            dbContext.Entry(detached).State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public async Task ReplaceCategoriesAsync(
        Guid experienceId,
        IEnumerable<ExperienceCategory> categories,
        CancellationToken cancellationToken = default)
    {
        List<ExperienceCategory> snapshot = categories.ToList();
        List<ExperienceCategory> existing = await dbContext.ExperienceCategories
            .Where(ec => ec.ExperienceId == experienceId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.ExperienceCategories.RemoveRange(existing);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        await AddCategoriesAsync(snapshot, cancellationToken);
    }

    public Task AddImagesAsync(
        IEnumerable<ExperienceImage> images,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<ExperienceImage> snapshot = images.ToList();
        foreach (ExperienceImage img in snapshot)
        {
            ExperienceImage detached = new()
            {
                Id = img.Id,
                ExperienceId = img.ExperienceId,
                ObjectKey = img.ObjectKey,
                Url = img.Url,
                ThumbnailUrl = img.ThumbnailUrl,
                AltText = img.AltText,
                SortOrder = img.SortOrder,
                IsCover = img.IsCover,
                RemovedAt = img.RemovedAt,
                RemovedBy = img.RemovedBy,
                RemovalReason = img.RemovalReason,
                CreatedAt = img.CreatedAt
            };
            dbContext.Entry(detached).State = EntityState.Added;
        }

        return Task.CompletedTask;
    }

    public async Task ReplaceImagesAsync(
        Guid experienceId,
        IEnumerable<ExperienceImage> images,
        CancellationToken cancellationToken = default)
    {
        List<ExperienceImage> snapshot = images.ToList();
        List<ExperienceImage> existing = await dbContext.ExperienceImages
            .Where(i => i.ExperienceId == experienceId && i.RemovedAt == null)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.ExperienceImages.RemoveRange(existing);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        await AddImagesAsync(snapshot, cancellationToken);
    }

    public Task UpdateAsync(
        Experience experience,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dbContext.Entry(experience).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
