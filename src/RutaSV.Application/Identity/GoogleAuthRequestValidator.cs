using FluentValidation;

namespace RutaSV.Application.Identity;

public sealed class GoogleAuthRequestValidator : AbstractValidator<GoogleAuthRequest>
{
    public GoogleAuthRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("El token de Google (IdToken) es obligatorio.");
    }
}
