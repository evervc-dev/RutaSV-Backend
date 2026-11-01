using FluentValidation;

using RutaSV.Application.Identity.DTO;

namespace RutaSV.Application.Identity.Validation;

public sealed class ReviewOperatorRequestRequestValidator : AbstractValidator<ReviewOperatorRequestRequest>
{
    public ReviewOperatorRequestRequestValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("El motivo de rechazo es obligatorio cuando se rechaza una solicitud.")
            .When(x => !x.IsApproved);

        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("El motivo de rechazo no puede exceder 500 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.RejectionReason));
    }
}
