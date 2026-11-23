using FluentValidation;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Domain.Catalog.Enum;

namespace RutaSV.Application.Catalog.Validation;

public sealed class CreateExperienceRequestValidator : AbstractValidator<CreateExperienceRequest>
{
    public CreateExperienceRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título de la experiencia es obligatorio.")
            .MaximumLength(150).WithMessage("El título no puede exceder 150 caracteres.");

        RuleFor(x => x.Slug)
            .MaximumLength(180).WithMessage("El slug no puede exceder 180 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.ShortDescription)
            .NotEmpty().WithMessage("La descripción corta es obligatoria.")
            .MaximumLength(300).WithMessage("La descripción corta no puede exceder 300 caracteres.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción completa es obligatoria.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("El precio debe ser mayor o igual a 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Length(3).WithMessage("La moneda debe ser un código ISO de 3 caracteres (ej. USD).");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("La duración en minutos debe ser mayor que 0.")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.Difficulty)
            .Must(BeValidDifficulty)
            .WithMessage("La dificultad proporcionada no es válida. Valores permitidos: FACIL, MODERADA, DIFICIL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Difficulty));

        RuleFor(x => x.MinAge)
            .GreaterThanOrEqualTo((short)0).WithMessage("La edad mínima no puede ser negativa.")
            .When(x => x.MinAge.HasValue);

        RuleFor(x => x.ServiceLanguage)
            .MaximumLength(50).WithMessage("El idioma de atención no puede exceder 50 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ServiceLanguage));

        RuleFor(x => x.MinPeople)
            .GreaterThanOrEqualTo((short)1).WithMessage("El mínimo de personas por reserva debe ser al menos 1.");

        When(x => x.HasLimitedCapacity, () =>
        {
            RuleFor(x => x.MaxPeople)
                .NotNull().WithMessage("El cupo máximo de personas es obligatorio cuando la experiencia tiene cupo limitado.");

            When(x => x.MaxPeople.HasValue, () =>
            {
                RuleFor(x => x.MaxPeople!.Value)
                    .GreaterThanOrEqualTo((short)1).WithMessage("El cupo máximo de personas debe ser al menos 1.")
                    .GreaterThanOrEqualTo(x => x.MinPeople).WithMessage("El cupo máximo de personas debe ser mayor o igual al mínimo de personas.");
            });
        });

        When(x => !x.HasLimitedCapacity && x.MaxPeople.HasValue, () =>
        {
            RuleFor(x => x.MaxPeople!.Value)
                .GreaterThanOrEqualTo(x => x.MinPeople)
                .WithMessage("Si se especifica un máximo de personas, debe ser mayor o igual al mínimo de personas.");
        });

        RuleFor(x => x.MunicipalityId)
            .GreaterThan(0).WithMessage("El identificador del municipio debe ser mayor que 0.");

        RuleFor(x => x.ReferenceAddress)
            .MaximumLength(300).WithMessage("La dirección de referencia no puede exceder 300 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceAddress));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("La latitud debe estar entre -90 y 90 grados.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("La longitud debe estar entre -180 y 180 grados.");

        When(x => x.CategoryIds is not null && x.CategoryIds.Count > 0, () =>
        {
            RuleForEach(x => x.CategoryIds!)
                .GreaterThan(0).WithMessage("Cada identificador de categoría debe ser mayor que 0.");
        });

        When(x => x.Images is not null && x.Images.Count > 0, () =>
        {
            RuleFor(x => x.Images!)
                .Must(images => images.Count(i => i.IsCover) <= 1)
                .WithMessage("Solo puede marcarse una imagen como portada (isCover = true).");

            RuleForEach(x => x.Images!).ChildRules(image =>
            {
                image.RuleFor(i => i.ObjectKey)
                    .MaximumLength(300).WithMessage("La clave del objeto no puede exceder 300 caracteres.")
                    .When(i => !string.IsNullOrWhiteSpace(i.ObjectKey));

                image.RuleFor(i => i.Url)
                    .MaximumLength(500).WithMessage("La URL de la imagen no puede exceder 500 caracteres.")
                    .When(i => !string.IsNullOrWhiteSpace(i.Url));

                image.RuleFor(i => i.ThumbnailUrl)
                    .MaximumLength(500).WithMessage("La URL de la miniatura no puede exceder 500 caracteres.")
                    .When(i => !string.IsNullOrWhiteSpace(i.ThumbnailUrl));

                image.RuleFor(i => i.AltText)
                    .MaximumLength(200).WithMessage("El texto alternativo no puede exceder 200 caracteres.")
                    .When(i => !string.IsNullOrWhiteSpace(i.AltText));

                image.RuleFor(i => i.SortOrder)
                    .GreaterThanOrEqualTo((short)0).WithMessage("El orden de la imagen no puede ser negativo.");

                image.RuleFor(i => i)
                    .Must(i => !string.IsNullOrWhiteSpace(i.ObjectKey) || !string.IsNullOrWhiteSpace(i.Url))
                    .WithMessage("Cada imagen debe especificar al menos un 'ObjectKey' o una 'Url'.");
            });
        });
    }

    private static bool BeValidDifficulty(string? difficulty)
    {
        return !string.IsNullOrWhiteSpace(difficulty)
            && Enum.TryParse<DifficultyLevel>(difficulty.Trim(), ignoreCase: true, out _);
    }
}
