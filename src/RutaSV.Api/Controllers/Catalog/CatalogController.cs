using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Service;

namespace RutaSV.Api.Controllers.Catalog;

[ApiController]
[Route("api")]
public sealed class CatalogController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> categories = await catalogService.GetCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("departments")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentDto> departments = await catalogService.GetDepartmentsAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpGet("departments/{departmentId:int}/municipalities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MunicipalityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMunicipalitiesByDepartment(
        [FromRoute] short departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<MunicipalityDto> municipalities = await catalogService.GetMunicipalitiesAsync(
            departmentId,
            cancellationToken);
        return Ok(municipalities);
    }

    [HttpGet("municipalities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<MunicipalityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMunicipalities(
        [FromQuery] short? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MunicipalityDto> municipalities = await catalogService.GetMunicipalitiesAsync(
            departmentId,
            cancellationToken);
        return Ok(municipalities);
    }
}
