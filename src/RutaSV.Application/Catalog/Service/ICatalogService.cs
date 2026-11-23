using RutaSV.Application.Catalog.DTO;

namespace RutaSV.Application.Catalog.Service;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MunicipalityDto>> GetMunicipalitiesAsync(short? departmentId = null, CancellationToken cancellationToken = default);
}
