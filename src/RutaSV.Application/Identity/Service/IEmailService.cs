namespace RutaSV.Application.Identity.Service;

public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico de verificación al usuario con el token proporcionado.
    /// </summary>
    /// <param name="recipientEmail">El correo electrónico del destinatario.</param>
    /// <param name="firstName">El nombre del destinatario.</param>
    /// <param name="verificationToken">El token de verificación.</param>
    /// <param name="cancellationToken">El token de cancelación.</param>
    /// <returns>Una tarea que representa la operación asincrónica.</returns>
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default
    );
}
