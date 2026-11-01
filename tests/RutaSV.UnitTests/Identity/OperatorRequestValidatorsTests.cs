using FluentAssertions;

using FluentValidation.TestHelper;

using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Validation;

namespace RutaSV.UnitTests.Identity;

public sealed class OperatorRequestValidatorsTests
{
    private readonly CreateOperatorRequestRequestValidator _createValidator = new();
    private readonly GetOperatorRequestsQueryValidator _queryValidator = new();
    private readonly ReviewOperatorRequestRequestValidator _reviewValidator = new();

    [Fact]
    public void CreateValidator_WhenValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        CreateOperatorRequestRequest request = new(
            new CreateOperatorCompanyRequest(
                "Aventuras El Boquerón",
                "Aventuras El Boquerón S.A. de C.V.",
                "Tours guiados al volcán de San Salvador",
                "contacto@boqueron.sv",
                "+50322113344",
                "+50377889900",
                "https://boqueron.sv",
                "https://minio.local/logos/boqueron.png",
                14,
                "Km 18 Carretera al Boquerón"),
            "Deseo registrar mi empresa turística.",
            [
                new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui-frente.jpg"),
                new CreateOperatorRequestDocumentRequest("ENLACE_REDES", null, "https://instagram.com/boqueron.sv")
            ]);

        // Act
        TestValidationResult<CreateOperatorRequestRequest> result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_WhenTradeNameEmptyOrNoDocuments_ShouldHaveValidationErrors()
    {
        // Arrange
        CreateOperatorRequestRequest request = new(
            new CreateOperatorCompanyRequest(""),
            "Mensaje",
            []);

        // Act
        TestValidationResult<CreateOperatorRequestRequest> result = _createValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Company.TradeName);
        result.ShouldHaveValidationErrorFor(x => x.Documents);
    }

    [Fact]
    public void CreateValidator_WhenDocumentHasInvalidTypeOrMissingKeyAndUrl_ShouldHaveValidationErrors()
    {
        // Arrange
        CreateOperatorRequestRequest request = new(
            new CreateOperatorCompanyRequest("Ruta Maya SV"),
            null,
            [
                new CreateOperatorRequestDocumentRequest("TIPO_INVALIDO", null, null)
            ]);

        // Act
        TestValidationResult<CreateOperatorRequestRequest> result = _createValidator.TestValidate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void QueryValidator_WhenValidPaginationAndStatus_ShouldNotHaveValidationErrors()
    {
        // Arrange
        GetOperatorRequestsQuery query = new(1, 15, "PENDIENTE");

        // Act
        TestValidationResult<GetOperatorRequestsQuery> result = _queryValidator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void QueryValidator_WhenInvalidPageNumberOrInvalidStatus_ShouldHaveValidationErrors()
    {
        // Arrange
        GetOperatorRequestsQuery query = new(0, 200, "ESTADO_INEXISTENTE");

        // Act
        TestValidationResult<GetOperatorRequestsQuery> result = _queryValidator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void ReviewValidator_WhenApprovedWithoutReason_ShouldNotHaveValidationErrors()
    {
        // Arrange
        ReviewOperatorRequestRequest request = new(true);

        // Act
        TestValidationResult<ReviewOperatorRequestRequest> result = _reviewValidator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReviewValidator_WhenRejectedWithoutReason_ShouldHaveValidationError()
    {
        // Arrange
        ReviewOperatorRequestRequest request = new(false, "");

        // Act
        TestValidationResult<ReviewOperatorRequestRequest> result = _reviewValidator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason);
    }
}
