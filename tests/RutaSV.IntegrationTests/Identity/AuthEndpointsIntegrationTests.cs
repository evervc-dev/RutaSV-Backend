using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

using RutaSV.Application.Identity.DTO;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.IntegrationTests.Identity;

public sealed class AuthEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenValidPayload_ShouldReturn200OkWithAuthResponseAndSendVerificationToken()
    {
        // Arrange
        RegisterRequest request = new(
            "visitante.nuevo@rutasv.com",
            "RutaSV2026!",
            "Roberto",
            "Cañas",
            "+50379998888"
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("visitante.nuevo@rutasv.com");
        body.FirstName.Should().Be("Roberto");
        body.Role.Should().Be("VISITANTE");
        body.EmailVerified.Should().BeFalse();
        body.Token.Should().NotBeNullOrWhiteSpace();

        _factory.Store.SentVerificationTokensByEmail
            .ContainsKey("visitante.nuevo@rutasv.com")
            .Should().BeTrue();
    }

    [Fact]
    public async Task Register_WhenDuplicateEmail_ShouldReturn409ConflictWithProblemDetails()
    {
        // Arrange
        RegisterRequest request = new(
            "duplicado@rutasv.com",
            "RutaSV2026!",
            "Claudia",
            "Lars");

        _ = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act
        HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ProblemDetails? problem = await duplicateResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Detail.Should().Contain("duplicado@rutasv.com");
    }

    [Fact]
    public async Task Register_WhenInvalidPayload_ShouldReturn400BadRequestWithValidationProblemDetails()
    {
        // Arrange
        RegisterRequest invalidRequest = new(
            "email-invalido",
            "123",
            "",
            "");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ShouldReturn200OkWithJwt()
    {
        // Arrange
        RegisterRequest registerRequest = new(
            "login.exitoso@rutasv.com",
            "RutaSV2026!",
            "Salvador",
            "Sánchez");
        _ = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        LoginRequest loginRequest = new("login.exitoso@rutasv.com", "RutaSV2026!");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("login.exitoso@rutasv.com");
        body.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WhenPasswordIsWrong_ShouldReturn401UnauthorizedWithProblemDetails()
    {
        // Arrange
        RegisterRequest registerRequest = new(
            "login.fallido@rutasv.com",
            "RutaSV2026!",
            "Salvador",
            "Sánchez");
        _ = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        LoginRequest wrongLogin = new("login.fallido@rutasv.com", "PasswordIncorrecta999!");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", wrongLogin);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GoogleLogin_WhenValidIdToken_ShouldReturn200OkWithVerifiedUser()
    {
        // Arrange
        GoogleAuthRequest request = new("valid-google-token");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuthResponse? body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body.Should().NotBeNull();
        body!.Email.Should().Be("oauth.turista@gmail.com");
        body.EmailVerified.Should().BeTrue();
        body.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GoogleLogin_WhenInvalidIdToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        GoogleAuthRequest request = new("invalid-google-token");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/google", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VerifyEmail_WhenValidToken_ShouldActivateAccountAndRejectSecondUse()
    {
        // Arrange
        string email = "verificar.cuenta@rutasv.com";
        RegisterRequest registerRequest = new(
            email,
            "RutaSV2026!",
            "Alberto",
            "Masferrer");

        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        string rawToken = _factory.Store.SentVerificationTokensByEmail[email];
        VerifyEmailRequest verifyRequest = new(rawToken);

        // Act 1: Happy path
        HttpResponseMessage verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert 1
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        User updatedUser = _factory.Store.Users.Values.Single(u => u.Email == email);
        updatedUser.EmailVerified.Should().BeTrue();
        updatedUser.Status.Should().Be(UserStatus.ACTIVO);

        // Act 2: Sad path (reutilizar el mismo token ya consumido)
        HttpResponseMessage secondVerifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert 2
        secondVerifyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WhenNonExistentToken_ShouldReturn400BadRequest()
    {
        // Arrange
        VerifyEmailRequest request = new("token-inexistente-000000");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/verify-email", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
