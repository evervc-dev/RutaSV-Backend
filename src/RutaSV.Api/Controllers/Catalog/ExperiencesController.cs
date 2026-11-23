using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Service;
using RutaSV.Application.Common.DTO;
using RutaSV.Application.Common.Storage;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Catalog;

[ApiController]
[Route("api/experiences")]
public sealed class ExperiencesController(
    IExperienceService experienceService,
    IFileStorageService fileStorageService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<ExperienceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPublicCatalog(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] short? departmentId = null,
        [FromQuery] int? municipalityId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] decimal? minRating = null,
        [FromQuery] string? difficulty = null,
        [FromQuery] int? minDurationMinutes = null,
        [FromQuery] int? maxDurationMinutes = null,
        [FromQuery] bool? hasLimitedCapacity = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        GetPublicExperiencesQuery query = new(
            pageNumber,
            pageSize,
            search,
            categoryId,
            departmentId,
            municipalityId,
            minPrice,
            maxPrice,
            minRating,
            difficulty,
            minDurationMinutes,
            maxDurationMinutes,
            hasLimitedCapacity,
            sortBy);

        PagedResponse<ExperienceSummaryDto> response = await experienceService.GetPublicPagedAsync(
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("my")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(PagedResponse<ExperienceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyExperiences(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        GetMyExperiencesQuery query = new(pageNumber, pageSize, status, companyId);
        PagedResponse<ExperienceDto> response = await experienceService.GetMyPagedAsync(
            userId,
            isAdmin,
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{slugOrId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdOrSlug(
        [FromRoute] string slugOrId,
        CancellationToken cancellationToken)
    {
        Guid? currentUserId = TryGetAuthenticatedUserId();
        bool isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("ADMIN");

        ExperienceDto response = await experienceService.GetByIdOrSlugAsync(
            slugOrId,
            currentUserId,
            isAdmin,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ExperienceDto response = await experienceService.CreateAsync(
            userId,
            isAdmin,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateExperienceRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ExperienceDto response = await experienceService.UpdateAsync(
            userId,
            isAdmin,
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ExperienceDto response = await experienceService.PublishAsync(
            userId,
            isAdmin,
            id,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/pause")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pause(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ExperienceDto response = await experienceService.PauseAsync(
            userId,
            isAdmin,
            id,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        await experienceService.DeleteAsync(
            userId,
            isAdmin,
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(
        [FromRoute] Guid id,
        [FromBody] WithdrawExperienceRequest request,
        CancellationToken cancellationToken)
    {
        Guid adminUserId = GetAuthenticatedUserId();
        ExperienceDto response = await experienceService.WithdrawAsync(adminUserId, id, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid adminUserId = GetAuthenticatedUserId();
        ExperienceDto response = await experienceService.RestoreAsync(adminUserId, id, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveImage(
        [FromRoute] Guid id,
        [FromRoute] Guid imageId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");
        ExperienceDto response = await experienceService.RemoveImageAsync(userId, isAdmin, id, imageId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("images/upload")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadImage(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo inválido",
                Detail = "Debes seleccionar una imagen válida."
            });
        }

        if (file.Length > (5 * 1024 * 1024))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo demasiado grande",
                Detail = "La imagen no puede superar los 5 MB."
            });
        }

        string contentType = file.ContentType.ToLowerInvariant();
        if (contentType is not "image/jpeg" and not "image/png" and not "image/webp")
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Formato no soportado",
                Detail = "Solo se permiten imágenes JPG, PNG o WEBP."
            });
        }

        Guid userId = GetAuthenticatedUserId();
        string safeFileName = Path.GetFileName(file.FileName);
        string objectKey = $"experiences/{userId}/{Guid.NewGuid():N}-{safeFileName}";

        await using Stream stream = file.OpenReadStream();
        await fileStorageService.UploadAsync(stream, objectKey, contentType, cancellationToken);

        string url = $"/api/experiences/images/file?objectKey={Uri.EscapeDataString(objectKey)}";
        return Ok(new { objectKey, url });
    }

    [HttpGet("images/file")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImageFile(
        [FromQuery] string? objectKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return NotFound();
        }

        StoredFileResult? stored = await fileStorageService.GetObjectAsync(
            objectKey.Trim(),
            cancellationToken);

        if (stored is null)
        {
            return NotFound();
        }

        return File(stored.Stream, stored.ContentType);
    }

    private Guid GetAuthenticatedUserId()
    {
        Guid? userId = TryGetAuthenticatedUserId();
        if (!userId.HasValue)
        {
            throw new InvalidCredentialsException("No se pudo identificar al usuario autenticado desde el token.");
        }

        return userId.Value;
    }

    private Guid? TryGetAuthenticatedUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        string? rawUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out Guid userId) ? userId : null;
    }
}
