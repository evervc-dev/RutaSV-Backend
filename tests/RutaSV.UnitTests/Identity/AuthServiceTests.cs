using System.Security.Cryptography;
using System.Text;

using FluentAssertions;
using FluentValidation;
using Moq;
using RutaSV.Application.Identity;
using RutaSV.Domain.Identity;

namespace RutaSV.UnitTests.Identity;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserTokenRepository> _userTokenRepositoryMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<IGoogleAuthService> _googleAuthServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepositoryMock.Object,
            _userTokenRepositoryMock.Object,
            _jwtProviderMock.Object,
            _googleAuthServiceMock.Object,
            _emailServiceMock.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            new GoogleAuthRequestValidator(),
            new VerifyEmailRequestValidator());
    }

    [Fact]
    public async Task RegisterAsync_WhenNewUser_ShouldHashPasswordCreateTokenSendEmailAndReturnJwt()
    {
        // Arrange
        RegisterRequest request = new(
            "NuevoTurista@RutaSV.com",
            "SecurePass123!",
            "Ana",
            "López",
            "+50371112222");

        User? capturedUser = null;
        UserToken? capturedToken = null;
        string? sentRawToken = null;

        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync("nuevoturista@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => capturedUser = u)
            .Returns(Task.CompletedTask);

        _userTokenRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<UserToken>(), It.IsAny<CancellationToken>()))
            .Callback<UserToken, CancellationToken>((t, _) => capturedToken = t)
            .Returns(Task.CompletedTask);

        _emailServiceMock
            .Setup(e => e.SendVerificationEmailAsync(
                "nuevoturista@rutasv.com",
                "Ana",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, raw, _) => sentRawToken = raw)
            .Returns(Task.CompletedTask);

        _jwtProviderMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("jwt-token-generado");

        // Act
        AuthResponse response = await _sut.RegisterAsync(request);

        // Assert
        response.Email.Should().Be("nuevoturista@rutasv.com");
        response.FirstName.Should().Be("Ana");
        response.LastName.Should().Be("López");
        response.Role.Should().Be("VISITANTE");
        response.EmailVerified.Should().BeFalse();
        response.Token.Should().Be("jwt-token-generado");

        capturedUser.Should().NotBeNull();
        capturedUser!.Status.Should().Be(UserStatus.PENDIENTE_VERIFICACION);
        capturedUser.AuthProvider.Should().Be(AuthProvider.LOCAL);
        BCrypt.Net.BCrypt.Verify("SecurePass123!", capturedUser.PasswordHash).Should().BeTrue();

        capturedToken.Should().NotBeNull();
        capturedToken!.Type.Should().Be(TokenType.VERIFICAR_EMAIL);
        sentRawToken.Should().NotBeNullOrWhiteSpace();
        capturedToken.TokenHash.Should().Be(ComputeSha256(sentRawToken!));

        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange
        RegisterRequest request = new(
            "existente@rutasv.com",
            "SecurePass123!",
            "Ana",
            "López");

        _userRepositoryMock
            .Setup(r => r.ExistsByEmailAsync("existente@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => _ = await _sut.RegisterAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<UserAlreadyExistsException>();
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(
            e => e.SendVerificationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenWeakPassword_ShouldThrowValidationException()
    {
        // Arrange
        RegisterRequest request = new("ana@rutasv.com", "123", "Ana", "López");

        // Act
        Func<Task> act = async () => _ = await _sut.RegisterAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<ValidationException>();
        _userRepositoryMock.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenValidCredentials_ShouldUpdateLastLoginAndReturnJwt()
    {
        // Arrange
        User existingUser = new()
        {
            Id = Guid.NewGuid(),
            Email = "turista@rutasv.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass123!"),
            FirstName = "Carlos",
            LastName = "Pérez",
            Role = UserRole.VISITANTE,
            Status = UserStatus.ACTIVO,
            EmailVerified = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("turista@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _jwtProviderMock
            .Setup(j => j.GenerateToken(existingUser))
            .Returns("valid-login-jwt");

        LoginRequest request = new("Turista@RutaSV.com", "CorrectPass123!");

        // Act
        AuthResponse response = await _sut.LoginAsync(request);

        // Assert
        response.Id.Should().Be(existingUser.Id);
        response.Token.Should().Be("valid-login-jwt");
        existingUser.LastLoginAt.Should().NotBeNull();
        _userRepositoryMock.Verify(r => r.UpdateAsync(existingUser, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("noexiste@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        LoginRequest request = new("noexiste@rutasv.com", "SomePassword123!");

        // Act
        Func<Task> act = async () => _ = await _sut.LoginAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        User existingUser = new()
        {
            Id = Guid.NewGuid(),
            Email = "turista@rutasv.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword123!"),
            Status = UserStatus.ACTIVO
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("turista@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        LoginRequest request = new("turista@rutasv.com", "WrongPassword123!");

        // Act
        Func<Task> act = async () => _ = await _sut.LoginAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsBlocked_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        User blockedUser = new()
        {
            Id = Guid.NewGuid(),
            Email = "bloqueado@rutasv.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass123!"),
            Status = UserStatus.BLOQUEADO
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("bloqueado@rutasv.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(blockedUser);

        LoginRequest request = new("bloqueado@rutasv.com", "CorrectPass123!");

        // Act
        Func<Task> act = async () => _ = await _sut.LoginAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenUserDoesNotExist_ShouldAutoRegisterAsActiveAndVerified()
    {
        // Arrange
        GoogleAuthRequest request = new("valid-google-id-token");
        GoogleUserPayload googlePayload = new(
            "google-sub-12345",
            "googleuser@gmail.com",
            "María",
            "Gómez",
            "https://lh3.googleusercontent.com/photo.jpg");

        User? createdUser = null;

        _googleAuthServiceMock
            .Setup(g => g.ValidateIdTokenAsync("valid-google-id-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(googlePayload);

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("googleuser@gmail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => createdUser = u)
            .Returns(Task.CompletedTask);

        _jwtProviderMock
            .Setup(j => j.GenerateToken(It.IsAny<User>()))
            .Returns("google-jwt-token");

        // Act
        AuthResponse response = await _sut.GoogleLoginAsync(request);

        // Assert
        response.Email.Should().Be("googleuser@gmail.com");
        response.EmailVerified.Should().BeTrue();
        response.Token.Should().Be("google-jwt-token");

        createdUser.Should().NotBeNull();
        createdUser!.AuthProvider.Should().Be(AuthProvider.GOOGLE);
        createdUser.ProviderId.Should().Be("google-sub-12345");
        createdUser.Status.Should().Be(UserStatus.ACTIVO);
        createdUser.EmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenGoogleTokenIsInvalid_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        GoogleAuthRequest request = new("invalid-google-token");

        _googleAuthServiceMock
            .Setup(g => g.ValidateIdTokenAsync("invalid-google-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidCredentialsException("Token de Google inválido."));

        // Act
        Func<Task> act = async () => _ = await _sut.GoogleLoginAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenIsValid_ShouldMarkEmailVerifiedAndStatusActive()
    {
        // Arrange
        string rawToken = "token-verificacion-valido";
        string tokenHash = ComputeSha256(rawToken);

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = "pendiente@rutasv.com",
            Status = UserStatus.PENDIENTE_VERIFICACION,
            EmailVerified = false
        };

        UserToken userToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = TokenType.VERIFICAR_EMAIL,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(12),
            UsedAt = null,
            User = user
        };

        _userTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(tokenHash, TokenType.VERIFICAR_EMAIL, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userToken);

        VerifyEmailRequest request = new(rawToken);

        // Act
        await _sut.VerifyEmailAsync(request);

        // Assert
        user.EmailVerified.Should().BeTrue();
        user.Status.Should().Be(UserStatus.ACTIVO);
        userToken.UsedAt.Should().NotBeNull();
        _userTokenRepositoryMock.Verify(r => r.UpdateAsync(userToken, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WhenTokenIsExpiredOrUsed_ShouldThrowInvalidTokenException()
    {
        // Arrange
        string rawToken = "token-expirado";
        string tokenHash = ComputeSha256(rawToken);

        UserToken expiredToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Type = TokenType.VERIFICAR_EMAIL,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            UsedAt = null
        };

        _userTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(tokenHash, TokenType.VERIFICAR_EMAIL, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        VerifyEmailRequest request = new(rawToken);

        // Act
        Func<Task> act = async () => await _sut.VerifyEmailAsync(request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidTokenException>();
    }

    private static string ComputeSha256(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
