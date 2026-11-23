using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Bookings.DTO;
using RutaSV.Application.Bookings.Service;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Common.DTO;
using RutaSV.Domain.Identity.Exception;

namespace RutaSV.Api.Controllers.Bookings;

[ApiController]
[Route("api")]
[Authorize]
public sealed class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpPost("reservations")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWebReservation(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        ReservationDto created = await reservationService.CreateWebReservationAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("reservations/in-person")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInPersonReservation(
        [FromBody] CreateInPersonReservationRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto created = await reservationService.CreateInPersonReservationAsync(
            userId,
            isAdmin,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("reservations/my")]
    [ProducesResponseType(typeof(PagedResponse<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReservations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        PagedResponse<ReservationDto> response = await reservationService.GetMyReservationsAsync(
            userId,
            pageNumber,
            pageSize,
            status,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("reservations/operator")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(PagedResponse<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperatorReservations(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] Guid? experienceId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        PagedResponse<ReservationDto> response = await reservationService.GetOperatorReservationsAsync(
            userId,
            isAdmin,
            pageNumber,
            pageSize,
            status,
            experienceId,
            search,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("reservations/{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto reservation = await reservationService.GetByIdAsync(userId, isAdmin, id, cancellationToken);
        return Ok(reservation);
    }

    [HttpGet("reservations/{id:guid}/cancel-preview")]
    [ProducesResponseType(typeof(CancelPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCancelPreview(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        CancelPreviewDto preview = await reservationService.GetCancelPreviewAsync(userId, isAdmin, id, cancellationToken);
        return Ok(preview);
    }

    [HttpPost("reservations/{id:guid}/confirm")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.ConfirmAsync(userId, isAdmin, id, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("reservations/{id:guid}/reject")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectReservationRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.RejectAsync(userId, isAdmin, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("reservations/{id:guid}/complete")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.CompleteAsync(userId, isAdmin, id, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("reservations/{id:guid}/reschedule")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RescheduleByOperator(
        [FromRoute] Guid id,
        [FromBody] RescheduleReservationRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.RescheduleByOperatorAsync(
            userId,
            isAdmin,
            id,
            request,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("reservations/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelReservationRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.CancelAsync(userId, isAdmin, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("reservations/{id:guid}/incident/options")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidentOptions(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<AvailabilityDto> options = await reservationService.GetIncidentRescheduleOptionsAsync(
            userId,
            isAdmin,
            id,
            cancellationToken);

        return Ok(options);
    }

    [HttpPost("reservations/{id:guid}/incident/resolve")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveIncident(
        [FromRoute] Guid id,
        [FromBody] ResolveIncidentRequest request,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        ReservationDto updated = await reservationService.ResolveIncidentAsync(
            userId,
            isAdmin,
            id,
            request,
            cancellationToken);

        return Ok(updated);
    }

    [HttpGet("refunds")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<RefundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperatorRefunds(CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        IReadOnlyList<RefundDto> refunds = await reservationService.GetOperatorRefundsAsync(userId, isAdmin, cancellationToken);
        return Ok(refunds);
    }

    [HttpGet("refunds/{idOrCode}")]
    [ProducesResponseType(typeof(RefundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRefundByIdOrCode(
        [FromRoute] string idOrCode,
        CancellationToken cancellationToken)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        RefundDto refund = await reservationService.GetRefundByIdOrCodeAsync(userId, isAdmin, idOrCode, cancellationToken);
        return Ok(refund);
    }

    [HttpPost("refunds/{id:guid}/complete")]
    [Authorize(Roles = "OPERADOR,ADMIN")]
    [ProducesResponseType(typeof(RefundDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteRefund(
        [FromRoute] Guid id,
        [FromBody] CompleteRefundRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        Guid userId = GetAuthenticatedUserId();
        bool isAdmin = User.IsInRole("ADMIN");

        RefundDto updated = await reservationService.CompleteRefundAsync(userId, isAdmin, id, request, cancellationToken);
        return Ok(updated);
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
