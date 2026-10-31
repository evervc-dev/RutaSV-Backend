using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Common.DTO;
using RutaSV.Application.Common.Storage;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Identity;

[ApiController]
[Route("api/operator-requests")]
public sealed class OperatorRequestsController(
    IOperatorRequestService operatorRequestService,
    IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(OperatorRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOperatorRequestRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        OperatorRequestDto response = await operatorRequestService.CreateAsync(
            userId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(OperatorRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyLatest(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        OperatorRequestDto? response = await operatorRequestService.GetMyLatestAsync(
            userId,
            cancellationToken);

        return response is null ? NoContent() : Ok(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OperatorRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateMyRequest(
        [FromRoute] Guid id,
        [FromBody] CreateOperatorRequestRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        OperatorRequestDto response = await operatorRequestService.UpdateMyRequestAsync(
            userId,
            id,
            request,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("documents/upload")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Archivo inválido",
                Detail = "Debes seleccionar un archivo válido."
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
                Detail = "Solo se permiten imágenes JPG o PNG."
            });
        }

        Guid userId = GetAuthenticatedUserId();
        string safeFileName = Path.GetFileName(file.FileName);
        string objectKey = $"operator-requests/{userId}/{Guid.NewGuid():N}-{safeFileName}";

        await using Stream stream = file.OpenReadStream();
        await fileStorageService.UploadAsync(stream, objectKey, contentType, cancellationToken);

        string url = $"/api/operator-requests/documents/file?objectKey={Uri.EscapeDataString(objectKey)}";
        return Ok(new { objectKey, url });
    }

    [HttpGet("documents/file")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocumentFile(
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

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(PagedResponse<OperatorRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        GetOperatorRequestsQuery query = new(pageNumber, pageSize, status);
        PagedResponse<OperatorRequestDto> response = await operatorRequestService.GetPagedAsync(
            query,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(OperatorRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Review(
        [FromRoute] Guid id,
        [FromBody] ReviewOperatorRequestRequest request,
        CancellationToken cancellationToken)
    {
        Guid adminUserId = GetAuthenticatedUserId();
        OperatorRequestDto response = await operatorRequestService.ReviewAsync(
            id,
            adminUserId,
            request,
            cancellationToken);

        return Ok(response);
    }

    private Guid GetAuthenticatedUserId()
    {
        string? rawUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(rawUserId, out Guid userId))
        {
            throw new InvalidCredentialsException("No se pudo identificar al usuario autenticado desde el token.");
        }

        return userId;
    }
}
