namespace RutaSV.Application.Identity;

public interface IEmailService
{
    Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default);
}
