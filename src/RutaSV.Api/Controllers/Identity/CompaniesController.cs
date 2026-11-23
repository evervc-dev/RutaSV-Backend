using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Common.Storage;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Identity;

[ApiController]
[Route("api/companies")]
public sealed class CompaniesController(
    ICompanyService companyService,
    IFileStorageService fileStorageService) : ControllerBase
{
    [HttpGet("my")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyCompanies(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<CompanyDto> companies = await companyService.GetMyCompaniesAsync(
            userId,
            isAdmin,
            cancellationToken);

        return Ok(companies);
    }

    [HttpGet("{idOrSlug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicProfile(
        [FromRoute] string idOrSlug,
        CancellationToken cancellationToken)
    {
        CompanyProfileDto profile = await companyService.GetPublicProfileAsync(idOrSlug, cancellationToken);
        return Ok(profile);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        CompanyDto updated = await companyService.UpdateAsync(userId, isAdmin, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("logo/upload")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadLogo(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo inválido",
                Detail = "Debes seleccionar un logotipo válido."
            });
        }

        if (file.Length > (5 * 1024 * 1024))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo demasiado grande",
                Detail = "El logotipo no puede superar los 5 MB."
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
        string objectKey = $"companies/{userId}/{Guid.NewGuid():N}-{safeFileName}";

        await using Stream stream = file.OpenReadStream();
        await fileStorageService.UploadAsync(stream, objectKey, contentType, cancellationToken);

        string url = $"/api/companies/logo/file?objectKey={Uri.EscapeDataString(objectKey)}";
        return Ok(new { objectKey, url });
    }

    [HttpGet("logo/file")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogoFile(
        [FromQuery] string? objectKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return NotFound();
        }

        StoredFileResult? stored = await fileStorageService.GetObjectAsync(objectKey.Trim(), cancellationToken);
        return stored is null ? NotFound() : File(stored.Stream, stored.ContentType);
    }

    private Guid GetAuthenticatedUserId()
    {
        string? rawUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(rawUserId, out Guid userId)
            ? userId
            : throw new InvalidCredentialsException("No se pudo identificar al usuario autenticado desde el token.");
    }
}
