using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using MimeKit;

using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;
using RutaSV.Infrastructure.Persistence;

namespace RutaSV.Infrastructure.Email;

public sealed class SmtpEmailService(
    IConfiguration configuration,
    ApplicationDbContext? dbContext = null) : IEmailService
{
    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        string frontendVerifyUrl = configuration["App:FrontendVerifyEmailUrl"] ?? "http://localhost:5173/verify-email";
        string verificationLink = $"{frontendVerifyUrl}?token={Uri.EscapeDataString(verificationToken)}";

        string subject = "Verificación de tu cuenta en RutaSV";
        string htmlBody = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2>¡Hola, {firstName}! Bienvenido a RutaSV</h2>
                <p>Gracias por registrarte en el marketplace turístico de El Salvador.</p>
                <p>Para activar tu cuenta, utiliza el siguiente código o enlace de verificación:</p>
                <p><strong>Token de verificación:</strong> <code>{verificationToken}</code></p>
                <p>
                    <a href="{verificationLink}" style="display: inline-block; padding: 10px 20px; background-color: #087e9a; color: #ffffff; text-decoration: none; border-radius: 5px;">
                        Verificar mi correo electrónico
                    </a>
                </p>
                <p><small>Este enlace expirará en 24 horas.</small></p>
            </div>
            """;
        string textBody = $"¡Hola, {firstName}! Verifica tu cuenta en RutaSV usando el token: {verificationToken} o visitando: {verificationLink}";

        MimeMessage message = BuildMimeMessage(firstName, recipientEmail, subject, htmlBody, textBody);

        EmailNotification? notification = null;
        if (dbContext is not null)
        {
            string normalizedEmail = recipientEmail.Trim().ToLowerInvariant();
            User? recipientUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.DeletedAt == null, cancellationToken);

            notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                UserId = recipientUser?.Id,
                Type = NotificationType.VERIFICAR_EMAIL,
                Recipient = recipientEmail,
                Subject = subject,
                Body = htmlBody,
                Status = EmailDeliveryStatus.PENDIENTE,
                Attempts = 1,
                CreatedAt = DateTime.UtcNow
            };

            _ = await dbContext.EmailNotifications.AddAsync(notification, cancellationToken);
        }

        try
        {
            await SendViaSmtpAsync(message, cancellationToken);

            if (notification is not null && dbContext is not null)
            {
                notification.Status = EmailDeliveryStatus.ENVIADO;
                notification.SentAt = DateTime.UtcNow;
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (notification is not null && dbContext is not null)
            {
                notification.Status = EmailDeliveryStatus.FALLIDO;
                notification.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }

    public async Task SendNotificationEmailAsync(
        EmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            return;
        }

        string recipientEmail = request.RecipientEmail.Trim();
        User? recipientUser = null;

        if (dbContext is not null)
        {
            if (request.UserId.HasValue)
            {
                recipientUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId.Value && u.DeletedAt == null, cancellationToken);
            }
            else
            {
                string normalizedEmail = recipientEmail.ToLowerInvariant();
                recipientUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail && u.DeletedAt == null, cancellationToken);
            }
        }

        // RN-22: Si el destinatario requiere correo verificado y no lo tiene, registrar OMITIDO y no enviar
        if (request.RequireVerifiedEmail && recipientUser is not null && !recipientUser.EmailVerified)
        {
            if (dbContext is not null)
            {
                _ = await dbContext.EmailNotifications.AddAsync(new EmailNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = recipientUser.Id,
                    ReservationId = request.ReservationId,
                    Type = request.Type,
                    Recipient = recipientEmail,
                    Subject = request.Subject.Length > 200 ? request.Subject[..200] : request.Subject,
                    Body = request.HtmlBody,
                    Status = EmailDeliveryStatus.OMITIDO,
                    Attempts = 0,
                    Error = "Correo del destinatario no verificado (RN-22).",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        MimeMessage message = BuildMimeMessage(
            request.RecipientName,
            recipientEmail,
            request.Subject,
            request.HtmlBody,
            request.TextBody);

        EmailNotification? notification = null;
        if (dbContext is not null)
        {
            notification = new EmailNotification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId ?? recipientUser?.Id,
                ReservationId = request.ReservationId,
                Type = request.Type,
                Recipient = recipientEmail,
                Subject = request.Subject.Length > 200 ? request.Subject[..200] : request.Subject,
                Body = request.HtmlBody,
                Status = EmailDeliveryStatus.PENDIENTE,
                Attempts = 1,
                CreatedAt = DateTime.UtcNow
            };
            _ = await dbContext.EmailNotifications.AddAsync(notification, cancellationToken);
        }

        try
        {
            await SendViaSmtpAsync(message, cancellationToken);

            if (notification is not null && dbContext is not null)
            {
                notification.Status = EmailDeliveryStatus.ENVIADO;
                notification.SentAt = DateTime.UtcNow;
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (notification is not null && dbContext is not null)
            {
                notification.Status = EmailDeliveryStatus.FALLIDO;
                notification.Error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private MimeMessage BuildMimeMessage(
        string recipientName,
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody)
    {
        string? smtpUser = configuration["Smtp:Username"]?.Trim();
        string? configuredSender = configuration["Smtp:SenderEmail"]?.Trim();
        string senderEmail = !string.IsNullOrWhiteSpace(configuredSender)
            ? configuredSender
            : !string.IsNullOrWhiteSpace(smtpUser)
                ? smtpUser
                : "no-reply@rutasv.com";
        string senderName = configuration["Smtp:SenderName"]?.Trim() ?? "RutaSV Marketplace";

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        }.ToMessageBody();

        return message;
    }

    private async Task SendViaSmtpAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        string smtpHost = configuration["Smtp:Host"]?.Trim() ?? "localhost";
        int smtpPort = int.TryParse(configuration["Smtp:Port"], out int parsedPort) ? parsedPort : 1025;
        string? smtpUser = configuration["Smtp:Username"]?.Trim();
        string? smtpPass = configuration["Smtp:Password"]?.Trim();
        bool hasCredentials = !string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass);

        SecureSocketOptions socketOptions = ResolveSecureSocketOptions(smtpHost, smtpPort, hasCredentials);

        using SmtpClient client = new();
        await client.ConnectAsync(smtpHost, smtpPort, socketOptions, cancellationToken);

        if (hasCredentials)
        {
            await client.AuthenticateAsync(smtpUser, smtpPass, cancellationToken);
        }

        _ = await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions ResolveSecureSocketOptions(string host, int port, bool hasCredentials)
    {
        if (port == 465)
        {
            return SecureSocketOptions.SslOnConnect;
        }

        if (port == 587)
        {
            return SecureSocketOptions.StartTls;
        }

        bool isLocalTestHost = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "mailpit", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1";

        return !isLocalTestHost && hasCredentials
            ? SecureSocketOptions.StartTlsWhenAvailable
            : SecureSocketOptions.None;
    }
}
