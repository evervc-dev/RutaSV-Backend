using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Identity;

namespace RutaSV.Application.Identity.Service;

public sealed class AuthService(
    IUserRepository userRepository,
    IUserTokenRepository userTokenRepository,
    IJwtProvider jwtProvider,
    IGoogleAuthService googleAuthService,
    IEmailService emailService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<GoogleAuthRequest> googleAuthValidator,
    IValidator<VerifyEmailRequest> verifyEmailValidator) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        // Valida la solicitud de registro utilizando FluentValidation y lanza una excepción si hay errores de validación
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        bool exists = await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new UserAlreadyExistsException(normalizedEmail);
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        DateTime now = DateTime.UtcNow;

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone?.Trim(),
            Role = UserRole.VISITANTE,
            AuthProvider = AuthProvider.LOCAL,
            Status = UserStatus.PENDIENTE_VERIFICACION,
            EmailVerified = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        string rawToken = GenerateSecureToken();
        string tokenHash = ComputeSha256Hash(rawToken);

        UserToken verificationToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = TokenType.VERIFICAR_EMAIL,
            TokenHash = tokenHash,
            ExpiresAt = now.AddHours(24),
            CreatedAt = now
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userTokenRepository.AddAsync(verificationToken, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        await emailService.SendVerificationEmailAsync(
            user.Email,
            user.FirstName,
            rawToken,
            cancellationToken);

        string jwt = jwtProvider.GenerateToken(user);

        return MapToResponse(user, jwt);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException();
        }

        if (user.Status is UserStatus.BLOQUEADO or UserStatus.SUSPENDIDO or UserStatus.ELIMINADO)
        {
            throw new InvalidCredentialsException("La cuenta de usuario no se encuentra habilitada para iniciar sesión.");
        }

        DateTime now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        string jwt = jwtProvider.GenerateToken(user);

        return MapToResponse(user, jwt);
    }

    public async Task<AuthResponse> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        await googleAuthValidator.ValidateAndThrowAsync(request, cancellationToken);

        GoogleUserPayload googleUser = await googleAuthService.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        string normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
        DateTime now = DateTime.UtcNow;

        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FirstName = googleUser.FirstName,
                LastName = googleUser.LastName,
                PhotoUrl = googleUser.PhotoUrl,
                Role = UserRole.VISITANTE,
                AuthProvider = AuthProvider.GOOGLE,
                ProviderId = googleUser.ProviderId,
                Status = UserStatus.ACTIVO,
                EmailVerified = true,
                LastLoginAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await userRepository.AddAsync(user, cancellationToken);
        }
        else
        {
            if (user.Status is UserStatus.BLOQUEADO or UserStatus.SUSPENDIDO or UserStatus.ELIMINADO)
            {
                throw new InvalidCredentialsException("La cuenta de usuario no se encuentra habilitada para iniciar sesión.");
            }

            if (string.IsNullOrEmpty(user.ProviderId))
            {
                user.ProviderId = googleUser.ProviderId;
            }

            if (!user.EmailVerified)
            {
                user.EmailVerified = true;
                if (user.Status == UserStatus.PENDIENTE_VERIFICACION)
                {
                    user.Status = UserStatus.ACTIVO;
                }
            }

            user.LastLoginAt = now;
            user.UpdatedAt = now;

            await userRepository.UpdateAsync(user, cancellationToken);
        }

        await userRepository.SaveChangesAsync(cancellationToken);

        string jwt = jwtProvider.GenerateToken(user);

        return MapToResponse(user, jwt);
    }

    public async Task VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        await verifyEmailValidator.ValidateAndThrowAsync(request, cancellationToken);

        string tokenHash = ComputeSha256Hash(request.Token.Trim());
        UserToken? userToken = await userTokenRepository.GetByTokenHashAsync(
            tokenHash,
            TokenType.VERIFICAR_EMAIL,
            cancellationToken);

        DateTime now = DateTime.UtcNow;

        if (userToken is null || userToken.UsedAt.HasValue || userToken.ExpiresAt <= now)
        {
            throw new InvalidTokenException();
        }

        User? user = userToken.User ?? await userRepository.GetByIdAsync(userToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidTokenException("El usuario asociado al token no existe.");
        }

        userToken.UsedAt = now;
        user.EmailVerified = true;
        if (user.Status == UserStatus.PENDIENTE_VERIFICACION)
        {
            user.Status = UserStatus.ACTIVO;
        }
        user.UpdatedAt = now;

        await userTokenRepository.UpdateAsync(userToken, cancellationToken);
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private static AuthResponse MapToResponse(User user, string jwt)
    {
        return new AuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.EmailVerified,
            jwt);
    }

    private static string GenerateSecureToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexStringLower(bytes);
    }

    private static string ComputeSha256Hash(string rawValue)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));
        return Convert.ToHexStringLower(bytes);
    }
}
