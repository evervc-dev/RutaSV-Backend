using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Service;

public sealed class CatalogService(ICatalogLookupRepository catalogLookupRepository) : ICatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Category> categories = await catalogLookupRepository.GetActiveCategoriesAsync(cancellationToken);

        return categories
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Icon,
                c.SortOrder,
                false))
            .ToList();
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Department> departments = await catalogLookupRepository.GetDepartmentsAsync(cancellationToken);

        return departments
            .Select(d => new DepartmentDto(d.Id, d.Name))
            .ToList();
    }

    public async Task<IReadOnlyList<MunicipalityDto>> GetMunicipalitiesAsync(
        short? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Municipality> municipalities = await catalogLookupRepository.GetMunicipalitiesAsync(
            departmentId,
            cancellationToken);

        return municipalities
            .Select(m => new MunicipalityDto(
                m.Id,
                m.DepartmentId,
                m.Department?.Name ?? string.Empty,
                m.Name))
            .ToList();
    }
}
