using RutaSV.Domain.Identity.Enum;

namespace RutaSV.Application.Identity.Service;

public sealed record EmailNotificationRequest(
    string RecipientEmail,
    string RecipientName,
    NotificationType Type,
    string Subject,
    string HtmlBody,
    string TextBody,
    Guid? UserId = null,
    Guid? ReservationId = null,
    bool RequireVerifiedEmail = false);

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

    /// <summary>
    /// Envía un correo transaccional de notificación (reservas, reembolsos, incidencias, etc.)
    /// respetando RN-22 (OMITIDO si RequireVerifiedEmail es true y el correo del usuario no está verificado).
    /// </summary>
    Task SendNotificationEmailAsync(
        EmailNotificationRequest request,
        CancellationToken cancellationToken = default
    );
}
