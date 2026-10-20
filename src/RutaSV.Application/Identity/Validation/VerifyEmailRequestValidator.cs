using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de verificación es obligatorio.");
    }
}
