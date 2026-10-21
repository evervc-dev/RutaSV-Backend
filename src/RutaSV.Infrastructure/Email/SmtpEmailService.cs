using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using RutaSV.Application.Identity.Service;

namespace RutaSV.Infrastructure.Email;

public sealed class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    public async Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        // Configuración de los parámetros SMTP desde la configuración.
        string smtpHost = configuration["Smtp:Host"] ?? "localhost"; // Valor predeterminado a "localhost" si no se proporciona en la configuración (Archivo en ./RutaSV.Api/appsettings.json).
        int smtpPort = int.TryParse(configuration["Smtp:Port"], out int parsedPort) ? parsedPort : 1025; // Valor predeterminado a 1025 si no se proporciona en la configuración o si no se puede analizar como un número entero.
        string senderEmail = configuration["Smtp:SenderEmail"] ?? "no-reply@rutasv.com"; // Valor predeterminado del que envía el correo por si no se proporciona en la configuración.
        string senderName = configuration["Smtp:SenderName"] ?? "RutaSV Marketplace"; // Valor predeterminado del nombre del remitente por si no se proporciona en la configuración.
        string frontendVerifyUrl = configuration["App:FrontendVerifyEmailUrl"] ?? "http://localhost:5173/verify-email"; // Valor predeterminado de la URL de verificación del frontend por si no se proporciona en la configuración.

        string verificationLink = $"{frontendVerifyUrl}?token={Uri.EscapeDataString(verificationToken)}"; // Construye el enlace de verificación utilizando la URL del frontend y el token de verificación proporcionado.

        MimeMessage message = new(); // Crea un nuevo mensaje de correo electrónico utilizando la biblioteca MimeKit.
        message.From.Add(new MailboxAddress(senderName, senderEmail)); // Agrega la dirección del remitente al mensaje de correo electrónico.
        message.To.Add(new MailboxAddress(firstName, recipientEmail)); // Agrega la dirección del destinatario al mensaje de correo electrónico.
        message.Subject = "Verificación de tu cuenta en RutaSV"; // Establece el asunto del correo electrónico.

        // Construye el cuerpo del correo electrónico en formato HTML y texto plano utilizando interpolación de cadenas y un objeto BodyBuilder.
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

        message.Body = bodyBuilder.ToMessageBody(); // Convierte el contenido del cuerpo del correo electrónico en un objeto MimeEntity y lo asigna al mensaje de correo electrónico.

        using SmtpClient client = new(); // Crea un nuevo cliente SMTP para enviar el correo electrónico.
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None, cancellationToken); // Conecta al servidor SMTP utilizando la configuración proporcionada y sin cifrado (SecureSocketOptions.None).

        string? smtpUser = configuration["Smtp:Username"]; // Obtiene el nombre de usuario para la autenticación SMTP desde la configuración.
        string? smtpPass = configuration["Smtp:Password"]; // Obtiene la contraseña para la autenticación SMTP desde la configuración.

        // Si se proporcionan credenciales de autenticación SMTP, se autentica con el servidor SMTP antes de enviar el correo electrónico.
        // Sino, no hay problema, ya que Mailpit permite enviar correos sin autenticación (en local)
        if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
        {
            await client.AuthenticateAsync(smtpUser, smtpPass, cancellationToken); // Autentica con el servidor SMTP utilizando las credenciales proporcionadas.
        }

        _ = await client.SendAsync(message, cancellationToken); // Envía el mensaje de correo electrónico al servidor SMTP.
        await client.DisconnectAsync(true, cancellationToken); // Desconecta del servidor SMTP de manera segura, asegurándose de que todos los datos pendientes se envíen antes de cerrar la conexión.
    }
}
