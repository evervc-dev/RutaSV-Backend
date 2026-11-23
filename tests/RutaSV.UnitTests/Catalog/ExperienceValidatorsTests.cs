using FluentAssertions;

using FluentValidation.TestHelper;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Validation;

namespace RutaSV.UnitTests.Catalog;

public sealed class ExperienceValidatorsTests
{
    private readonly CreateExperienceRequestValidator _createValidator = new();
    private readonly UpdateExperienceRequestValidator _updateValidator = new();
    private readonly GetPublicExperiencesQueryValidator _publicQueryValidator = new();
    private readonly GetMyExperiencesQueryValidator _myQueryValidator = new();

    [Fact]
    public void CreateValidator_WhenValidLimitedCapacityRequest_ShouldNotHaveValidationErrors()
    {
        CreateExperienceRequest request = new(
            "Clases de Surf en Playa El Tunco",
            "Aprende a surfear con instructores locales certificados.",
            "Disfruta de una sesión completa de surf de 2 horas con tabla incluida en Playa El Tunco.",
            35.00m,
            16,
            13.492900m,
            -89.383100m,
            HasLimitedCapacity: true,
            MinPeople: 1,
            MaxPeople: 8,
            Difficulty: "FACIL",
            CategoryIds: [1, 3],
            Images: [new CreateExperienceImageRequest("experiences/surf-cover.jpg", null, null, "Portada surf", 0, true)]);

        TestValidationResult<CreateExperienceRequest> result = _createValidator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_WhenUnlimitedCapacityAndNullMaxPeople_ShouldNotHaveValidationErrors()
    {
        CreateExperienceRequest request = new(
            "Recorrido Cultural por Suchitoto",
            "Caminata abierta por el centro histórico.",
            "Recorrido guiado por las calles empedradas e iglesia colonial de Suchitoto sin límite de cupo.",
            15.00m,
            22,
            13.938100m,
            -89.027800m,
            HasLimitedCapacity: false,
            MinPeople: 1,
            MaxPeople: null);

        TestValidationResult<CreateExperienceRequest> result = _createValidator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_WhenLimitedCapacityAndMaxPeopleIsNull_ShouldHaveValidationError()
    {
        CreateExperienceRequest request = new(
            "Clases de Surf",
            "Resumen corto",
            "Descripción completa de la clase de surf.",
            35.00m,
            16,
            13.492900m,
            -89.383100m,
            HasLimitedCapacity: true,
            MinPeople: 2,
            MaxPeople: null);

        TestValidationResult<CreateExperienceRequest> result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxPeople);
    }

    [Fact]
    public void CreateValidator_WhenLimitedCapacityAndMaxPeopleLessThanMinPeople_ShouldHaveValidationError()
    {
        CreateExperienceRequest request = new(
            "Tour Volcán Ilamatepec",
            "Ascenso al volcán de Santa Ana",
            "Caminata guiada hacia el cráter del volcán de Santa Ana.",
            25.00m,
            4,
            13.853100m,
            -89.630000m,
            HasLimitedCapacity: true,
            MinPeople: 5,
            MaxPeople: 3);

        TestValidationResult<CreateExperienceRequest> result = _createValidator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("MaxPeople"));
    }

    [Theory]
    [InlineData(-95.0, -89.2, "Latitude")]
    [InlineData(95.0, -89.2, "Latitude")]
    [InlineData(13.7, -185.0, "Longitude")]
    [InlineData(13.7, 185.0, "Longitude")]
    public void CreateValidator_WhenCoordinatesOutOfRange_ShouldHaveValidationError(
        double lat,
        double lng,
        string expectedProperty)
    {
        CreateExperienceRequest request = new(
            "Experiencia con Coordenadas Inválidas",
            "Resumen",
            "Descripción válida para prueba de coordenadas.",
            20.00m,
            14,
            (decimal)lat,
            (decimal)lng);

        TestValidationResult<CreateExperienceRequest> result = _createValidator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public void UpdateValidator_WhenMultipleCoverImages_ShouldHaveValidationError()
    {
        UpdateExperienceRequest request = new(
            "Tour actualizado",
            "Resumen actualizado",
            "Descripción actualizada con suficiente detalle.",
            40.00m,
            14,
            13.676900m,
            -89.279700m,
            Images:
            [
                new CreateExperienceImageRequest("experiences/img1.jpg", IsCover: true),
                new CreateExperienceImageRequest("experiences/img2.jpg", IsCover: true)
            ]);

        TestValidationResult<UpdateExperienceRequest> result = _updateValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Images);
    }

    [Fact]
    public void PublicQueryValidator_WhenMaxPriceLessThanMinPriceOrInvalidSort_ShouldHaveValidationErrors()
    {
        GetPublicExperiencesQuery query = new(
            PageNumber: 1,
            PageSize: 10,
            MinPrice: 50m,
            MaxPrice: 20m,
            SortBy: "orden_invalido");

        TestValidationResult<GetPublicExperiencesQuery> result = _publicQueryValidator.TestValidate(query);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void MyQueryValidator_WhenInvalidStatus_ShouldHaveValidationError()
    {
        GetMyExperiencesQuery query = new(1, 10, "ESTADO_INVALIDO");

        TestValidationResult<GetMyExperiencesQuery> result = _myQueryValidator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
