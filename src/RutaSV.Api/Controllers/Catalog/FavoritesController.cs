using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Service;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Catalog;

[ApiController]
[Route("api/favorites")]
[Authorize]
public sealed class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FavoriteItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFavorites(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        IReadOnlyList<FavoriteItemDto> items = await favoriteService.GetMyFavoritesAsync(userId, cancellationToken);
        return Ok(items);
    }

    [HttpGet("ids")]
    [ProducesResponseType(typeof(IReadOnlyList<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFavoriteIds(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        IReadOnlyList<Guid> ids = await favoriteService.GetMyFavoriteIdsAsync(userId, cancellationToken);
        return Ok(ids);
    }

    [HttpPost("{experienceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddFavorite(
        [FromRoute] Guid experienceId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        await favoriteService.AddFavoriteAsync(userId, experienceId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{experienceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavorite(
        [FromRoute] Guid experienceId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        await favoriteService.RemoveFavoriteAsync(userId, experienceId, cancellationToken);
        return NoContent();
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
