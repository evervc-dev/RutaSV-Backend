using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RutaSV.Application.Common.Storage;
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
            services.RemoveAll<ICompanyRepository>();
            services.RemoveAll<IOperatorRequestRepository>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IGoogleAuthService>();
            services.RemoveAll<IFileStorageService>();

            services.AddSingleton(Store);
            services.AddScoped<IUserRepository, FakeUserRepository>();
            services.AddScoped<IUserTokenRepository, FakeUserTokenRepository>();
            services.AddScoped<ICompanyRepository, FakeCompanyRepository>();
            services.AddScoped<IOperatorRequestRepository, FakeOperatorRequestRepository>();
            services.AddScoped<IEmailService, FakeEmailService>();
            services.AddScoped<IGoogleAuthService, FakeGoogleAuthService>();
            services.AddScoped<IFileStorageService, FakeFileStorageService>();
        });
    }
}

public sealed class InMemoryUserStore
{
    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<Guid, UserToken> Tokens { get; } = new();
    public ConcurrentDictionary<Guid, Company> Companies { get; } = new();
    public ConcurrentDictionary<Guid, OperatorRequest> OperatorRequests { get; } = new();
    public ConcurrentDictionary<Guid, OperatorRequestDocument> OperatorRequestDocuments { get; } = new();
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

    public Task InvalidateUserTokensAsync(
        Guid userId,
        TokenType type,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        foreach (UserToken token in store.Tokens.Values.Where(t => t.UserId == userId && t.Type == type && t.UsedAt is null))
        {
            token.UsedAt = now;
        }

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

internal sealed class FakeCompanyRepository(InMemoryUserStore store) : ICompanyRepository
{
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = store.Companies.TryGetValue(id, out Company? company);
        return Task.FromResult(company?.DeletedAt is null ? company : null);
    }

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeFileStorageService : IFileStorageService
{
    public Task UploadAsync(
        Stream stream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<StoredFileResult?> GetObjectAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<StoredFileResult?>(null);
    }
}

internal sealed class FakeOperatorRequestRepository(InMemoryUserStore store) : IOperatorRequestRepository
{
    public Task<OperatorRequest?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!store.OperatorRequests.TryGetValue(id, out OperatorRequest? request))
        {
            return Task.FromResult<OperatorRequest?>(null);
        }

        PopulateRelations(request);
        return Task.FromResult<OperatorRequest?>(request);
    }

    public Task<OperatorRequest?> GetLatestByUserIdWithDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        OperatorRequest? latest = store.OperatorRequests.Values
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        if (latest is null)
        {
            return Task.FromResult<OperatorRequest?>(null);
        }

        PopulateRelations(latest);
        return Task.FromResult<OperatorRequest?>(latest);
    }

    public Task<bool> HasPendingRequestByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        bool hasPending = store.OperatorRequests.Values.Any(
            r => r.UserId == userId && r.Status == OperatorRequestStatus.PENDIENTE);
        return Task.FromResult(hasPending);
    }

    public Task<(IReadOnlyList<OperatorRequest> Items, int TotalRecords)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        OperatorRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<OperatorRequest> query = store.OperatorRequests.Values;

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        List<OperatorRequest> filtered = query
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        int totalRecords = filtered.Count;
        List<OperatorRequest> items = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (OperatorRequest item in items)
        {
            PopulateRelations(item);
        }

        return Task.FromResult(((IReadOnlyList<OperatorRequest>)items, totalRecords));
    }

    public Task AddAsync(
        OperatorRequest operatorRequest,
        CancellationToken cancellationToken = default)
    {
        store.OperatorRequests[operatorRequest.Id] = operatorRequest;
        return Task.CompletedTask;
    }

    public Task AddDocumentsAsync(
        IEnumerable<OperatorRequestDocument> documents,
        CancellationToken cancellationToken = default)
    {
        foreach (OperatorRequestDocument doc in documents)
        {
            store.OperatorRequestDocuments[doc.Id] = doc;
        }

        return Task.CompletedTask;
    }

    public Task ReplaceDocumentsAsync(
        Guid requestId,
        IEnumerable<OperatorRequestDocument> documents,
        CancellationToken cancellationToken = default)
    {
        List<Guid> toRemove = store.OperatorRequestDocuments.Values
            .Where(d => d.RequestId == requestId)
            .Select(d => d.Id)
            .ToList();

        foreach (Guid docId in toRemove)
        {
            _ = store.OperatorRequestDocuments.TryRemove(docId, out _);
        }

        foreach (OperatorRequestDocument doc in documents)
        {
            store.OperatorRequestDocuments[doc.Id] = doc;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(
        OperatorRequest operatorRequest,
        CancellationToken cancellationToken = default)
    {
        store.OperatorRequests[operatorRequest.Id] = operatorRequest;
        return Task.CompletedTask;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await operation(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private void PopulateRelations(OperatorRequest request)
    {
        if (store.Users.TryGetValue(request.UserId, out User? user))
        {
            request.User = user;
        }

        if (store.Companies.TryGetValue(request.CompanyId, out Company? company))
        {
            request.Company = company;
        }

        request.Documents = store.OperatorRequestDocuments.Values
            .Where(d => d.RequestId == request.Id && d.DeletedAt is null)
            .ToList();
    }
}
