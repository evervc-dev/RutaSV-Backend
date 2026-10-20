using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class GoogleAuthRequestValidator : AbstractValidator<GoogleAuthRequest>
{
    public GoogleAuthRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("El token de Google (IdToken) es obligatorio.");
    }
}
