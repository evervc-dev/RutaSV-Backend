using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using RutaSV.Application.Identity;

namespace RutaSV.Infrastructure.Email;

public sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        string smtpHost = configuration["Smtp:Host"] ?? "localhost";
        int smtpPort = int.TryParse(configuration["Smtp:Port"], out int parsedPort) ? parsedPort : 1025;
        string senderEmail = configuration["Smtp:SenderEmail"] ?? "no-reply@rutasv.com";
        string senderName = configuration["Smtp:SenderName"] ?? "RutaSV Marketplace";
        string frontendVerifyUrl = configuration["App:FrontendVerifyEmailUrl"] ?? "http://localhost:5173/verify-email";

        string verificationLink = $"{frontendVerifyUrl}?token={Uri.EscapeDataString(verificationToken)}";

        MimeMessage message = new();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress(firstName, recipientEmail));
        message.Subject = "Verifica tu cuenta en RutaSV";

        BodyBuilder bodyBuilder = new()
        {
            HtmlBody = $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                    <h2>¡Hola, {firstName}! Bienvenido a RutaSV</h2>
                    <p>Gracias por registrarte en el marketplace turístico de El Salvador.</p>
                    <p>Para activar tu cuenta, utiliza el siguiente código o enlace de verificación:</p>
                    <p><strong>Token de verificación:</strong> <code>{verificationToken}</code></p>
                    <p>
                        <a href="{verificationLink}" style="display: inline-block; padding: 10px 20px; background-color: #0d6efd; color: #ffffff; text-decoration: none; border-radius: 5px;">
                            Verificar mi correo electrónico
                        </a>
                    </p>
                    <p><small>Este enlace expirará en 24 horas.</small></p>
                </div>
                """,
            TextBody = $"¡Hola, {firstName}! Verifica tu cuenta en RutaSV usando el token: {verificationToken} o visitando: {verificationLink}"
        };

        message.Body = bodyBuilder.ToMessageBody();

        using SmtpClient client = new();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None, cancellationToken);

        string? smtpUser = configuration["Smtp:Username"];
        string? smtpPass = configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
        {
            await client.AuthenticateAsync(smtpUser, smtpPass, cancellationToken);
        }

        _ = await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
