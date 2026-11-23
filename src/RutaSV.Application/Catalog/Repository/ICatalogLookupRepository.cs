using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Application.Catalog.Repository;

public interface ICatalogLookupRepository
{
    Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(short? departmentId = null, CancellationToken cancellationToken = default);
    Task<Municipality?> GetMunicipalityByIdAsync(int id, CancellationToken cancellationToken = default);
}
