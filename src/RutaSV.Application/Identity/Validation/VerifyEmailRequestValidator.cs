using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        // Validación para que el campo Token no esté vacío
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de verificación es obligatorio.");
    }
}
