using FluentAssertions;

using FluentValidation.Results;

using RutaSV.Application.Identity;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Validation;

namespace RutaSV.UnitTests.Identity;

public sealed class IdentityValidatorsTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly GoogleAuthRequestValidator _googleValidator = new();
    private readonly VerifyEmailRequestValidator _verifyEmailValidator = new();

    [Fact]
    public async Task RegisterRequestValidator_WhenValidRequest_ShouldNotHaveValidationErrors()
    {
        RegisterRequest request = new(
            "turista@rutasv.com",
            "Password123!",
            "Carlos",
            "Martínez",
            "+50370000000");

        ValidationResult result = await _registerValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "Password123!", "Carlos", "Martínez", "Email")]
    [InlineData("correo-invalido", "Password123!", "Carlos", "Martínez", "Email")]
    [InlineData("turista@rutasv.com", "Corta1!", "Carlos", "Martínez", "Password")]
    [InlineData("turista@rutasv.com", "sinmayusculas123", "Carlos", "Martínez", "Password")]
    [InlineData("turista@rutasv.com", "SINMINUSCULAS123", "Carlos", "Martínez", "Password")]
    [InlineData("turista@rutasv.com", "SinNumerosPassword", "Carlos", "Martínez", "Password")]
    [InlineData("turista@rutasv.com", "Password123!", "", "Martínez", "FirstName")]
    [InlineData("turista@rutasv.com", "Password123!", "Carlos", "", "LastName")]
    public async Task RegisterRequestValidator_WhenInvalidFields_ShouldFailValidation(
        string email,
        string password,
        string firstName,
        string lastName,
        string expectedProperty)
    {
        RegisterRequest request = new(email, password, firstName, lastName);

        ValidationResult result = await _registerValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task LoginRequestValidator_WhenValidRequest_ShouldPassValidation()
    {
        LoginRequest request = new("turista@rutasv.com", "Password123!");

        ValidationResult result = await _loginValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Password123!", "Email")]
    [InlineData("no-es-email", "Password123!", "Email")]
    [InlineData("turista@rutasv.com", "", "Password")]
    public async Task LoginRequestValidator_WhenInvalidRequest_ShouldFailValidation(
        string email,
        string password,
        string expectedProperty)
    {
        LoginRequest request = new(email, password);

        ValidationResult result = await _loginValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task GoogleAuthRequestValidator_WhenEmptyIdToken_ShouldFailValidation()
    {
        GoogleAuthRequest request = new("");

        ValidationResult result = await _googleValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "IdToken");
    }

    [Fact]
    public async Task VerifyEmailRequestValidator_WhenEmptyToken_ShouldFailValidation()
    {
        VerifyEmailRequest request = new("");

        ValidationResult result = await _verifyEmailValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Token");
    }
}
