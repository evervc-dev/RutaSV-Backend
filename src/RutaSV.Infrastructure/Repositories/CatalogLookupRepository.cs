using Microsoft.EntityFrameworkCore;

using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Repositories;

public sealed class CatalogLookupRepository(ApplicationDbContext dbContext) : ICatalogLookupRepository
{
    public async Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        List<int> idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await dbContext.Categories
            .AsNoTracking()
            .Where(c => idList.Contains(c.Id))
            .OrderBy(c => c.SortOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(
        short? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Municipality> query = dbContext.Municipalities
            .AsNoTracking()
            .Include(m => m.Department);

        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        return await query
            .OrderBy(m => m.DepartmentId)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Municipality?> GetMunicipalityByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Municipalities
            .AsNoTracking()
            .Include(m => m.Department)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }
}
