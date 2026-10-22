using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Exception;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.IntegrationTests.Identity;

public sealed class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemoryUserStore Store { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IUserTokenRepository>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IGoogleAuthService>();

            services.AddSingleton(Store);
            services.AddScoped<IUserRepository, FakeUserRepository>();
            services.AddScoped<IUserTokenRepository, FakeUserTokenRepository>();
            services.AddScoped<IEmailService, FakeEmailService>();
            services.AddScoped<IGoogleAuthService, FakeGoogleAuthService>();
        });
    }
}

public sealed class InMemoryUserStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<Guid, UserToken> Tokens { get; } = new();
    public ConcurrentDictionary<string, string> SentVerificationTokensByEmail { get; } = new();
}

internal sealed class FakeUserRepository(InMemoryUserStore store) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = store.Users.TryGetValue(id, out User? user);
        return Task.FromResult(user?.DeletedAt is null ? user : null);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        User? user = store.Users.Values.FirstOrDefault(
            u => string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase) && u.DeletedAt is null);
        return Task.FromResult(user);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        bool exists = store.Users.Values.Any(
            u => string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        store.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        store.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeUserTokenRepository(InMemoryUserStore store) : IUserTokenRepository
{
    public Task<UserToken?> GetByTokenHashAsync(
        string tokenHash,
        TokenType type,
        CancellationToken cancellationToken = default)
    {
        UserToken? token = store.Tokens.Values.FirstOrDefault(
            t => t.TokenHash == tokenHash && t.Type == type);

        if (token is not null && store.Users.TryGetValue(token.UserId, out User? user))
        {
            token.User = user;
        }

        return Task.FromResult(token);
    }

    public Task AddAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        store.Tokens[userToken.Id] = userToken;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(UserToken userToken, CancellationToken cancellationToken = default)
    {
        store.Tokens[userToken.Id] = userToken;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeEmailService(InMemoryUserStore store) : IEmailService
{
    public Task SendVerificationEmailAsync(
        string recipientEmail,
        string firstName,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        store.SentVerificationTokensByEmail[recipientEmail.ToLowerInvariant()] = verificationToken;
        return Task.CompletedTask;
    }
}

internal sealed class FakeGoogleAuthService : IGoogleAuthService
{
    public Task<GoogleUserPayload> ValidateIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (idToken == "valid-google-token")
        {
            return Task.FromResult(new GoogleUserPayload(
                "google-provider-999",
                "oauth.turista@gmail.com",
                "Elena",
                "Rivas",
                "https://example.com/avatar.png"));
        }

        throw new InvalidCredentialsException("El token de Google proporcionado no es válido.");
    }
}
