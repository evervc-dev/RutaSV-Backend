using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Service;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Catalog;

[ApiController]
[Route("api")]
public sealed class AvailabilitiesController(IAvailabilityService availabilityService) : ControllerBase
{
    [HttpGet("experiences/{experienceId:guid}/availabilities")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByExperience(
        [FromRoute] Guid experienceId,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] bool includeUnavailable = false,
        CancellationToken cancellationToken = default)
    {
        Guid? userId = TryGetAuthenticatedUserId();
        bool isAdmin = User.Identity?.IsAuthenticated == true && User.IsInRole("ADMIN");

        IReadOnlyList<AvailabilityDto> items = await availabilityService.GetByExperienceAsync(
            experienceId,
            fromDate,
            toDate,
            includeUnavailable,
            userId,
            isAdmin,
            cancellationToken);

        return Ok(items);
    }

    [HttpGet("availabilities/my")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyAvailabilities(
        [FromQuery] Guid? experienceId = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<AvailabilityDto> items = await availabilityService.GetMyAvailabilitiesAsync(
            userId,
            isAdmin,
            experienceId,
            fromDate,
            toDate,
            cancellationToken);

        return Ok(items);
    }

    [HttpPost("experiences/{experienceId:guid}/availabilities")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromRoute] Guid experienceId,
        [FromBody] CreateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        AvailabilityDto created = await availabilityService.CreateAsync(
            userId,
            isAdmin,
            experienceId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("availabilities/{id:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        AvailabilityDto updated = await availabilityService.UpdateAsync(
            userId,
            isAdmin,
            id,
            request,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("availabilities/{id:guid}/cancel")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelAvailabilityRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        AvailabilityDto cancelled = await availabilityService.CancelAsync(
            userId,
            isAdmin,
            id,
            request,
            cancellationToken);

        return Ok(cancelled);
    }

    [HttpGet("experiences/{experienceId:guid}/schedules")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<ExperienceScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedules(
        [FromRoute] Guid experienceId,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<ExperienceScheduleDto> schedules = await availabilityService.GetSchedulesAsync(
            userId,
            isAdmin,
            experienceId,
            cancellationToken);

        return Ok(schedules);
    }

    [HttpPost("experiences/{experienceId:guid}/schedules")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ExperienceScheduleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchedule(
        [FromRoute] Guid experienceId,
        [FromBody] CreateExperienceScheduleRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ExperienceScheduleDto created = await availabilityService.CreateScheduleAsync(
            userId,
            isAdmin,
            experienceId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("schedules/{id:guid}")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSchedule(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        await availabilityService.DeleteScheduleAsync(userId, isAdmin, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("experiences/{experienceId:guid}/availabilities/generate")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> GenerateFromSchedules(
        [FromRoute] Guid experienceId,
        [FromBody] GenerateAvailabilitiesRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<AvailabilityDto> generated = await availabilityService.GenerateFromSchedulesAsync(
            userId,
            isAdmin,
            experienceId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, generated);
    }

    private Guid GetAuthenticatedUserId()
    {
        Guid? userId = TryGetAuthenticatedUserId();
        return userId ?? throw new InvalidCredentialsException("No se pudo identificar al usuario autenticado desde el token.");
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
