namespace RutaSV.Application.Identity.Service;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default);
}
