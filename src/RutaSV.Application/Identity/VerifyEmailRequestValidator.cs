using FluentValidation;

namespace RutaSV.Application.Identity;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de verificación es obligatorio.");
    }
}
