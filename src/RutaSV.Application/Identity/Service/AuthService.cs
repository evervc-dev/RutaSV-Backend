using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Security;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Exception;
using RutaSV.Domain.Identity.Model;

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

        // Elimina espacios del correo y lo convierte a minúsculas para evitar duplicados
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // Verifica si ya existe un usuario con el mismo correo electrónico
        bool exists = await userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken);
        if (exists)
        {
            // Lanza una excepción personalizada si el usuario ya existe
            throw new UserAlreadyExistsException(normalizedEmail);
        }

        // Genera un hash seguro de la contraseña utilizando BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        DateTime now = DateTime.UtcNow;

        // Crea un nuevo objeto User con la información del usuario registrado
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

        // Genera un token seguro para la verificación de correo electrónico y su hash
        string rawToken = GenerateSecureToken();
        string tokenHash = ComputeSha256Hash(rawToken);

        // Crea un objeto UserToken para almacenar el token de verificación de correo electrónico
        UserToken verificationToken = new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = TokenType.VERIFICAR_EMAIL,
            TokenHash = tokenHash,
            ExpiresAt = now.AddHours(24),
            CreatedAt = now
        };

        // Agrega el usuario y el token de verificación a los repositorios y guarda los cambios en la base de datos
        await userRepository.AddAsync(user, cancellationToken);
        await userTokenRepository.AddAsync(verificationToken, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        // Envía un correo electrónico de verificación al usuario con el token generado
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

    /// <summary>
    /// Genera un token seguro utilizando RandomNumberGenerator y lo convierte a una cadena hexadecimal en minúsculas.
    /// </summary>
    /// <returns>Retorna el token seguro en formato hexadecimal en minúsculas.</returns>
    private static string GenerateSecureToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32); // Genera 32 bytes de datos aleatorios
        return Convert.ToHexStringLower(bytes); // Convierte los bytes a una cadena hexadecimal en minúsculas
    }

    /// <summary>
    /// Calcula el hash SHA-256 de un valor de entrada y lo convierte a una cadena hexadecimal en minúsculas.
    /// </summary>
    /// <param name="rawValue">El valor de entrada para calcular el hash.</param>
    /// <returns>Retorna el hash SHA-256 en formato hexadecimal en minúsculas.</returns>
    private static string ComputeSha256Hash(string rawValue)
    {
        // Calcula el hash SHA-256 del valor de entrada
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawValue));
        return Convert.ToHexStringLower(bytes); // Convierte los bytes del hash a una cadena hexadecimal en minúsculas y la retorna
    }
}
