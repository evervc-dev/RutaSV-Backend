using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Service;

public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema y devuelve un objeto AuthResponse con la información del usuario registrado y un token de autenticación.
    /// </summary>
    /// <param name="request">La solicitud de registro.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>AuthResponse con la información del usuario registrado y el token de autenticación.</returns>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia sesión en el sistema y devuelve el AuthResponse con la información del usuario autenticado y un token de autenticación.
    /// </summary>
    /// <param name="request">La solicitud de inicio de sesión que contiene las credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>AuthResponse con la información del usuario autenticado y un token de autenticación.</returns>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia sesión un usuario utilizando la autenticación de Google y devuelve el AuthResponse con la información del usuario autenticado y su token de autenticación.
    /// </summary>
    /// <param name="request">La solicitud de autenticación de Google con las credenciales del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>AuthResponse que contiene la información del usuario autenticado y un token de autenticación.</returns>
    Task<AuthResponse> GoogleLoginAsync(GoogleAuthRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica el correo electrónico de un usuario utilizando un token de verificación y actualiza el estado de verificación del correo electrónico en el sistema.
    /// </summary>
    /// <param name="request">Solicitud de verificación de correo electrónico.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns></returns>
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalida cualquier token anterior de verificación para el usuario, genera uno nuevo y reenvía el correo de verificación.
    /// </summary>
    /// <param name="request">Solicitud con el correo electrónico del usuario.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    Task ResendVerificationEmailAsync(ResendVerificationEmailRequest request, CancellationToken cancellationToken = default);
}
