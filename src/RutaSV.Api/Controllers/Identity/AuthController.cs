using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Service;

namespace RutaSV.Api.Controllers.Identity;

// Indica que es un controlador y define la ruta base para las acciones de autenticación
[ApiController]
[Route("api/auth")]
// Contructor recibe instancia para manejo de autenticación por DI
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    // Define los tipos de respuesta que puede devolver la acción Register (para documentación y manejo de errores)
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, // request se obtiene del cuerpo
        CancellationToken cancellationToken) // Si el cliente cancela la solicitud, se puede propagar la cancelación (de modo que se puede cancelar la operación de registro)
    {
        // Llama al servicio de autenticación para registrar al usuario y obtiene la respuesta
        AuthResponse response = await authService.RegisterAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Llama al servicio de autenticación para iniciar sesión y obtiene la respuesta
        AuthResponse response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleAuthRequest request,
        CancellationToken cancellationToken)
    {
        AuthResponse response = await authService.GoogleLoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(request, cancellationToken);
        return Ok(new { message = "Correo electrónico verificado exitosamente." });
    }

    [HttpPost("resend-verification-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ResendVerificationEmailRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        string email = !string.IsNullOrWhiteSpace(request?.Email)
            ? request.Email
            : User.FindFirstValue(JwtRegisteredClaimNames.Email)
              ?? User.FindFirstValue(ClaimTypes.Email)
              ?? string.Empty;

        await authService.ResendVerificationEmailAsync(
            new ResendVerificationEmailRequest(email),
            cancellationToken);

        return Ok(new { message = "Se ha enviado un nuevo correo de verificación." });
    }
}
