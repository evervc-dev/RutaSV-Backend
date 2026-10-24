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
            cancellationToken
        );

        string jwt = jwtProvider.GenerateToken(user); // Genera un token JWT para el usuario registrado

        return MapToResponse(user, jwt); // Mapea la información del usuario y el token JWT a un objeto AuthResponse y lo retorna
    }

    public async Task<AuthResponse> LoginAsync(
       LoginRequest request,
       CancellationToken cancellationToken = default)
    {
        // Valida la solicitud de inicio de sesión utilizando FluentValidation y lanza una excepción si hay errores de validación
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Normaliza el correo electrónico eliminando espacios y convirtiéndolo a minúsculas
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken); // Obtiene el usuario de la base de datos por correo electrónico

        // Verifica si el usuario existe y si la contraseña proporcionada coincide con el hash almacenado en la base de datos
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new InvalidCredentialsException(); // Lanza una excepción personalizada si el usuario no existe o no tiene una contraseña válida
        }

        // Verifica si la contraseña proporcionada coincide con el hash almacenado en la base de datos utilizando BCrypt
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidCredentialsException(); // Lanza una excepción personalizada si la contraseña es incorrecta
        }

        // Verifica si el estado del usuario es BLOQUEADO, SUSPENDIDO o ELIMINADO y lanza una excepción si es así
        if (user.Status is UserStatus.BLOQUEADO or UserStatus.SUSPENDIDO or UserStatus.ELIMINADO)
        {
            throw new InvalidCredentialsException("La cuenta de usuario no se encuentra habilitada para iniciar sesión.");
        }

        // Actualiza la fecha y hora del último inicio de sesión y la fecha de actualización del usuario
        DateTime now = DateTime.UtcNow;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        // Guarda los cambios en la base de datos para reflejar la actualización del último inicio de sesión y la fecha de actualización del usuario
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        string jwt = jwtProvider.GenerateToken(user); // Genera un token JWT para el usuario que ha iniciado sesión correctamente

        return MapToResponse(user, jwt); // Mapea la información del usuario y el token JWT a un objeto AuthResponse y lo retorna
    }

    public async Task<AuthResponse> GoogleLoginAsync(
        GoogleAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        // Valida la solicitud de inicio de sesión con Google utilizando FluentValidation y lanza una excepción si hay errores de validación
        await googleAuthValidator.ValidateAndThrowAsync(request, cancellationToken);

        // Valida el token de ID de Google y obtiene la información del usuario desde el servicio de autenticación de Google
        GoogleUserPayload googleUser = await googleAuthService.ValidateIdTokenAsync(request.IdToken, cancellationToken);
        string normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();
        DateTime now = DateTime.UtcNow; // Obtiene la fecha y hora para registrar el último inicio de sesión y la fecha de actualización del usuario

        // Verifica si el usuario ya existe en la base de datos
        User? user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        // Si el usuario no existe, crea un nuevo usuario con la información obtenida del token de Google y lo agrega a la base de datos
        if (user is null)
        {
            // Crea un nuevo objeto User con la información del usuario obtenido del token de Google
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

            await userRepository.AddAsync(user, cancellationToken); // Agrega el nuevo usuario al repositorio
        }
        else
        {
            //  Si el usuario ya existe, verifica si su estado es BLOQUEADO, SUSPENDIDO o ELIMINADO y lanza una excepción si es así
            if (user.Status is UserStatus.BLOQUEADO or UserStatus.SUSPENDIDO or UserStatus.ELIMINADO)
            {
                // Lanza una excepción personalizada si la cuenta de usuario no está habilitada para iniciar sesión
                throw new InvalidCredentialsException("La cuenta de usuario no se encuentra habilitada para iniciar sesión.");
            }

            // Actualiza la información del usuario existente con los datos obtenidos del token de Google, si es necesario  
            if (string.IsNullOrEmpty(user.ProviderId))
            {
                user.ProviderId = googleUser.ProviderId;
            }

            // Si el correo electrónico del usuario no está verificado, lo marca como verificado y cambia el estado a ACTIVO si estaba pendiente de verificación
            if (!user.EmailVerified)
            {
                user.EmailVerified = true;
                if (user.Status == UserStatus.PENDIENTE_VERIFICACION)
                {
                    user.Status = UserStatus.ACTIVO;
                }
            }

            // Actualiza la fecha y hora del último inicio de sesión y la fecha de actualización del usuario
            user.LastLoginAt = now;
            user.UpdatedAt = now;

            await userRepository.UpdateAsync(user, cancellationToken); // Actualiza el usuario existente en el repositorio
        }

        // Guarda los cambios en la base de datos para reflejar la creación o actualización del usuario
        await userRepository.SaveChangesAsync(cancellationToken);

        string jwt = jwtProvider.GenerateToken(user); // Genera un token JWT para el usuario que ha iniciado sesión correctamente con Google

        return MapToResponse(user, jwt); // Mapea la información del usuario y el token JWT a un objeto AuthResponse y lo retorna
    }

    public async Task VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        // Valida la solicitud de verificación de correo electrónico utilizando FluentValidation y lanza una excepción si hay errores de validación
        await verifyEmailValidator.ValidateAndThrowAsync(request, cancellationToken);

        string tokenHash = ComputeSha256Hash(request.Token.Trim()); // Calcula el hash SHA-256 del token proporcionado en la solicitud de verificación de correo electrónico

        // Obtiene el token de verificación utilizando el hash del token, el tipo de token y el token de cancelación
        UserToken? userToken = await userTokenRepository.GetByTokenHashAsync(
            tokenHash,
            TokenType.VERIFICAR_EMAIL,
            cancellationToken
        );

        // Obtiene la fecha y hora actual para comparar con la fecha de expiración del token
        DateTime now = DateTime.UtcNow;

        // Verifica si el token es nulo, ya ha sido utilizado o ha expirado, y lanza una excepción personalizada si alguna de estas condiciones se cumple
        if (userToken is null || userToken.UsedAt.HasValue || userToken.ExpiresAt <= now)
        {
            throw new InvalidTokenException();
        }

        // Obtiene el usuario asociado al token o lo busca por su ID si no está disponible
        User? user = userToken.User ?? await userRepository.GetByIdAsync(userToken.UserId, cancellationToken);

        // Verifica si el usuario es nulo y lanza una excepción personalizada si no se encuentra el usuario asociado al token
        if (user is null)
        {
            throw new InvalidTokenException("El usuario asociado al token no existe.");
        }

        // Actualiza el token como utilizado, marca el correo electrónico del usuario como verificado y cambia el estado del usuario a ACTIVO si estaba pendiente de verificación
        userToken.UsedAt = now;
        user.EmailVerified = true;
        if (user.Status == UserStatus.PENDIENTE_VERIFICACION)
        {
            user.Status = UserStatus.ACTIVO;
        }
        user.UpdatedAt = now; // Actualiza la fecha de actualización del usuario

        await userTokenRepository.UpdateAsync(userToken, cancellationToken); // Actualiza el token de verificación en el repositorio
        await userRepository.UpdateAsync(user, cancellationToken); // Actualiza el usuario en el repositorio
        await userRepository.SaveChangesAsync(cancellationToken); // Guarda los cambios en la base de datos
    }

    /// <summary>
    /// Mapea la información del usuario y el token JWT a un objeto AuthResponse que se retornará al cliente.
    /// </summary>
    /// <param name="user">El usuario cuya información se va a mapear.</param>
    /// <param name="jwt">El token JWT a incluir en la respuesta.</param>
    /// <returns>Un objeto AuthResponse con la información mapeada.</returns>
    private static AuthResponse MapToResponse(User user, string jwt)
    {
        return new AuthResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.EmailVerified,
            jwt
        );
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
