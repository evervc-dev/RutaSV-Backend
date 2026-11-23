using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RutaSV.Application.Bookings.Repository;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Application.Common.Storage;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Service;
using RutaSV.Domain.Bookings.Enum;
using RutaSV.Domain.Bookings.Model;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Model;
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
            services.RemoveAll<ICatalogLookupRepository>();
            services.RemoveAll<IExperienceRepository>();
            services.RemoveAll<IAvailabilityRepository>();
            services.RemoveAll<IFavoriteRepository>();
            services.RemoveAll<IReservationRepository>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IGoogleAuthService>();
            services.RemoveAll<IFileStorageService>();

            services.AddSingleton(Store);
            services.AddScoped<IUserRepository, FakeUserRepository>();
            services.AddScoped<IUserTokenRepository, FakeUserTokenRepository>();
            services.AddScoped<ICompanyRepository, FakeCompanyRepository>();
            services.AddScoped<IOperatorRequestRepository, FakeOperatorRequestRepository>();
            services.AddScoped<ICatalogLookupRepository, FakeCatalogLookupRepository>();
            services.AddScoped<IExperienceRepository, FakeExperienceRepository>();
            services.AddScoped<IAvailabilityRepository, FakeAvailabilityRepository>();
            services.AddScoped<IFavoriteRepository, FakeFavoriteRepository>();
            services.AddScoped<IReservationRepository, FakeReservationRepository>();
            services.AddScoped<IEmailService, FakeEmailService>();
            services.AddScoped<IGoogleAuthService, FakeGoogleAuthService>();
            services.AddScoped<IFileStorageService, FakeFileStorageService>();
        });
    }
}

public sealed class InMemoryUserStore
{
    public InMemoryUserStore()
    {
        Departments[5] = new Department { Id = 5, Name = "La Libertad" };
        Departments[6] = new Department { Id = 6, Name = "San Salvador" };

        Municipalities[14] = new Municipality
        {
            Id = 14,
            DepartmentId = 5,
            Name = "Santa Tecla",
            Department = Departments[5]
        };

        Municipalities[16] = new Municipality
        {
            Id = 16,
            DepartmentId = 5,
            Name = "Tamanique",
            Department = Departments[5]
        };

        Categories[1] = new Category { Id = 1, Name = "Playa", Slug = "playa", IsActive = true, SortOrder = 1 };
        Categories[2] = new Category { Id = 2, Name = "Montaña", Slug = "montana", IsActive = true, SortOrder = 2 };
        Categories[3] = new Category { Id = 3, Name = "Surf", Slug = "surf", IsActive = true, SortOrder = 3 };
        Categories[4] = new Category { Id = 4, Name = "Caminata", Slug = "caminata", IsActive = true, SortOrder = 4 };
    }

    public ConcurrentDictionary<Guid, User> Users { get; } = new();
    public ConcurrentDictionary<Guid, UserToken> Tokens { get; } = new();
    public ConcurrentDictionary<Guid, Company> Companies { get; } = new();
    public ConcurrentDictionary<Guid, OperatorRequest> OperatorRequests { get; } = new();
    public ConcurrentDictionary<Guid, OperatorRequestDocument> OperatorRequestDocuments { get; } = new();
    public ConcurrentDictionary<short, Department> Departments { get; } = new();
    public ConcurrentDictionary<int, Municipality> Municipalities { get; } = new();
    public ConcurrentDictionary<int, Category> Categories { get; } = new();
    public ConcurrentDictionary<Guid, Experience> Experiences { get; } = new();
    public ConcurrentDictionary<(Guid ExperienceId, int CategoryId), ExperienceCategory> ExperienceCategories { get; } = new();
    public ConcurrentDictionary<Guid, ExperienceImage> ExperienceImages { get; } = new();
    public ConcurrentDictionary<Guid, ExperienceSchedule> ExperienceSchedules { get; } = new();
    public ConcurrentDictionary<Guid, Availability> Availabilities { get; } = new();
    public ConcurrentDictionary<(Guid UserId, Guid ExperienceId), Favorite> Favorites { get; } = new();
    public ConcurrentDictionary<Guid, Reservation> Reservations { get; } = new();
    public ConcurrentDictionary<Guid, ReservationHistory> ReservationHistories { get; } = new();
    public ConcurrentDictionary<Guid, Refund> Refunds { get; } = new();
    public ConcurrentDictionary<Guid, ReservationIncident> ReservationIncidents { get; } = new();
    public ConcurrentDictionary<string, string> SentVerificationTokensByEmail { get; } = new();
    public ConcurrentBag<EmailNotificationRequest> SentNotifications { get; } = [];
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

    public Task SendNotificationEmailAsync(
        EmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        store.SentNotifications.Add(request);
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

    public Task<IReadOnlyList<Company>> GetByOperatorIdAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Company> companies = store.Companies.Values
            .Where(c => c.OperatorId == operatorId && c.DeletedAt is null)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
        return Task.FromResult(companies);
    }

    public Task<IReadOnlyList<Company>> GetApprovedCompaniesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Company> companies = store.Companies.Values
            .Where(c => c.DeletedAt is null && c.Status == CompanyStatus.APROBADA)
            .OrderBy(c => c.TradeName)
            .ToList();
        return Task.FromResult(companies);
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

internal sealed class FakeCatalogLookupRepository(InMemoryUserStore store) : ICatalogLookupRepository
{
    public Task<IReadOnlyList<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Category> categories = store.Categories.Values
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();
        return Task.FromResult(categories);
    }

    public Task<IReadOnlyList<Category>> GetCategoriesByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        HashSet<int> idSet = ids.ToHashSet();
        IReadOnlyList<Category> categories = store.Categories.Values
            .Where(c => idSet.Contains(c.Id))
            .OrderBy(c => c.SortOrder)
            .ToList();
        return Task.FromResult(categories);
    }

    public Task<IReadOnlyList<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Department> departments = store.Departments.Values
            .OrderBy(d => d.Id)
            .ToList();
        return Task.FromResult(departments);
    }

    public Task<IReadOnlyList<Municipality>> GetMunicipalitiesAsync(
        short? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Municipality> query = store.Municipalities.Values;
        if (departmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == departmentId.Value);
        }

        IReadOnlyList<Municipality> items = query
            .OrderBy(m => m.DepartmentId)
            .ThenBy(m => m.Name)
            .ToList();
        return Task.FromResult(items);
    }

    public Task<Municipality?> GetMunicipalityByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = store.Municipalities.TryGetValue(id, out Municipality? municipality);
        return Task.FromResult(municipality);
    }
}

internal sealed class FakeExperienceRepository(InMemoryUserStore store) : IExperienceRepository
{
    public Task<Experience?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!store.Experiences.TryGetValue(id, out Experience? experience) || experience.DeletedAt is not null)
        {
            return Task.FromResult<Experience?>(null);
        }

        PopulateRelations(experience);
        return Task.FromResult<Experience?>(experience);
    }

    public Task<Experience?> GetBySlugWithDetailsAsync(string slug, CancellationToken cancellationToken = default)
    {
        Experience? experience = store.Experiences.Values.FirstOrDefault(
            e => string.Equals(e.Slug, slug, StringComparison.OrdinalIgnoreCase) && e.DeletedAt is null);

        if (experience is null)
        {
            return Task.FromResult<Experience?>(null);
        }

        PopulateRelations(experience);
        return Task.FromResult<Experience?>(experience);
    }

    public Task<bool> ExistsBySlugAsync(
        string slug,
        Guid? excludeExperienceId = null,
        CancellationToken cancellationToken = default)
    {
        bool exists = store.Experiences.Values.Any(e =>
            string.Equals(e.Slug, slug, StringComparison.OrdinalIgnoreCase)
            && (!excludeExperienceId.HasValue || e.Id != excludeExperienceId.Value));

        return Task.FromResult(exists);
    }

    public Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetPublicPagedAsync(
        GetPublicExperiencesQuery query,
        CancellationToken cancellationToken = default)
    {
        List<Experience> all = store.Experiences.Values.ToList();
        foreach (Experience exp in all)
        {
            PopulateRelations(exp);
        }

        IEnumerable<Experience> filtered = all.Where(e =>
            e.DeletedAt is null
            && e.Status == ExperienceStatus.PUBLICADA
            && e.Company is not null
            && e.Company.DeletedAt is null
            && e.Company.Status == CompanyStatus.APROBADA
            && (e.Company.Operator is null || e.Company.Operator.Status == UserStatus.ACTIVO));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search.Trim();
            filtered = filtered.Where(e =>
                e.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || e.ShortDescription.Contains(search, StringComparison.OrdinalIgnoreCase)
                || e.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.CategoryId.HasValue)
        {
            filtered = filtered.Where(e => e.Categories.Any(ec => ec.CategoryId == query.CategoryId.Value));
        }

        if (query.MunicipalityId.HasValue)
        {
            filtered = filtered.Where(e => e.MunicipalityId == query.MunicipalityId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            filtered = filtered.Where(e => e.Municipality?.DepartmentId == query.DepartmentId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            filtered = filtered.Where(e => e.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            filtered = filtered.Where(e => e.Price <= query.MaxPrice.Value);
        }

        if (query.HasLimitedCapacity.HasValue)
        {
            filtered = filtered.Where(e => e.HasLimitedCapacity == query.HasLimitedCapacity.Value);
        }

        string sort = query.SortBy?.Trim().ToLowerInvariant() ?? "recent";
        filtered = sort switch
        {
            "price_asc" => filtered.OrderBy(e => e.Price),
            "price_desc" => filtered.OrderByDescending(e => e.Price),
            "rating" => filtered.OrderByDescending(e => e.AverageRating),
            _ => filtered.OrderByDescending(e => e.PublishedAt ?? e.CreatedAt)
        };

        List<Experience> materialized = filtered.ToList();
        int totalRecords = materialized.Count;
        List<Experience> items = materialized
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Experience>)items, totalRecords));
    }

    public Task<(IReadOnlyList<Experience> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ExperienceStatus? status = null,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        List<Experience> all = store.Experiences.Values.ToList();
        foreach (Experience exp in all)
        {
            PopulateRelations(exp);
        }

        IEnumerable<Experience> filtered = all.Where(e => e.DeletedAt is null);

        if (operatorId.HasValue)
        {
            filtered = filtered.Where(e => e.Company?.OperatorId == operatorId.Value);
        }

        if (companyId.HasValue)
        {
            filtered = filtered.Where(e => e.CompanyId == companyId.Value);
        }

        if (status.HasValue)
        {
            filtered = filtered.Where(e => e.Status == status.Value);
        }

        List<Experience> materialized = filtered
            .OrderByDescending(e => e.UpdatedAt)
            .ToList();

        int totalRecords = materialized.Count;
        List<Experience> items = materialized
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Experience>)items, totalRecords));
    }

    public Task AddAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        store.Experiences[experience.Id] = experience;
        return Task.CompletedTask;
    }

    public Task AddCategoriesAsync(IEnumerable<ExperienceCategory> categories, CancellationToken cancellationToken = default)
    {
        foreach (ExperienceCategory cat in categories)
        {
            store.ExperienceCategories[(cat.ExperienceId, cat.CategoryId)] = cat;
        }

        return Task.CompletedTask;
    }

    public Task ReplaceCategoriesAsync(
        Guid experienceId,
        IEnumerable<ExperienceCategory> categories,
        CancellationToken cancellationToken = default)
    {
        List<(Guid ExperienceId, int CategoryId)> keys = store.ExperienceCategories.Keys
            .Where(k => k.ExperienceId == experienceId)
            .ToList();

        foreach ((Guid ExperienceId, int CategoryId) key in keys)
        {
            _ = store.ExperienceCategories.TryRemove(key, out _);
        }

        return AddCategoriesAsync(categories, cancellationToken);
    }

    public Task AddImagesAsync(IEnumerable<ExperienceImage> images, CancellationToken cancellationToken = default)
    {
        foreach (ExperienceImage img in images)
        {
            store.ExperienceImages[img.Id] = img;
        }

        return Task.CompletedTask;
    }

    public Task ReplaceImagesAsync(
        Guid experienceId,
        IEnumerable<ExperienceImage> images,
        CancellationToken cancellationToken = default)
    {
        List<Guid> toRemove = store.ExperienceImages.Values
            .Where(i => i.ExperienceId == experienceId && i.RemovedAt is null)
            .Select(i => i.Id)
            .ToList();

        foreach (Guid id in toRemove)
        {
            _ = store.ExperienceImages.TryRemove(id, out _);
        }

        return AddImagesAsync(images, cancellationToken);
    }

    public Task UpdateAsync(Experience experience, CancellationToken cancellationToken = default)
    {
        store.Experiences[experience.Id] = experience;
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

    private void PopulateRelations(Experience experience)
    {
        if (store.Companies.TryGetValue(experience.CompanyId, out Company? company))
        {
            if (store.Users.TryGetValue(company.OperatorId, out User? op))
            {
                company.Operator = op;
            }

            experience.Company = company;
        }

        if (store.Municipalities.TryGetValue(experience.MunicipalityId, out Municipality? municipality))
        {
            if (store.Departments.TryGetValue(municipality.DepartmentId, out Department? dept))
            {
                municipality.Department = dept;
            }

            experience.Municipality = municipality;
        }

        List<ExperienceCategory> cats = store.ExperienceCategories.Values
            .Where(ec => ec.ExperienceId == experience.Id)
            .ToList();

        foreach (ExperienceCategory ec in cats)
        {
            if (store.Categories.TryGetValue(ec.CategoryId, out Category? cat))
            {
                ec.Category = cat;
            }
        }

        experience.Categories = cats;
        experience.Images = store.ExperienceImages.Values
            .Where(i => i.ExperienceId == experience.Id && i.RemovedAt is null)
            .OrderBy(i => i.SortOrder)
            .ToList();
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

internal sealed class FakeAvailabilityRepository(InMemoryUserStore store) : IAvailabilityRepository
{
    public Task<Availability?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!store.Availabilities.TryGetValue(id, out Availability? a))
        {
            return Task.FromResult<Availability?>(null);
        }

        PopulateExperience(a);
        return Task.FromResult<Availability?>(a);
    }

    public Task<Availability?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Availability>> GetByExperienceAsync(
        Guid experienceId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool includeUnavailable = false,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Availability> query = store.Availabilities.Values
            .Where(a => a.ExperienceId == experienceId);

        if (!includeUnavailable)
        {
            query = query.Where(a => a.Status == AvailabilityStatus.ABIERTA || a.Status == AvailabilityStatus.AGOTADA);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value);
        }

        List<Availability> items = query.OrderBy(a => a.Date).ThenBy(a => a.StartTime).ToList();
        foreach (Availability item in items)
        {
            PopulateExperience(item);
        }

        return Task.FromResult<IReadOnlyList<Availability>>(items);
    }

    public Task<IReadOnlyList<Availability>> GetByOperatorAsync(
        Guid? operatorId,
        Guid? experienceId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        List<Availability> all = store.Availabilities.Values.ToList();
        foreach (Availability item in all)
        {
            PopulateExperience(item);
        }

        IEnumerable<Availability> query = all.Where(a => a.Experience != null && a.Experience.DeletedAt == null);

        if (operatorId.HasValue)
        {
            query = query.Where(a => a.Experience?.Company?.OperatorId == operatorId.Value);
        }

        if (experienceId.HasValue)
        {
            query = query.Where(a => a.ExperienceId == experienceId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.Date >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.Date <= toDate.Value);
        }

        return Task.FromResult<IReadOnlyList<Availability>>(
            query.OrderBy(a => a.Date).ThenBy(a => a.StartTime).ToList());
    }

    public Task<bool> ExistsSlotAsync(
        Guid experienceId,
        DateOnly date,
        TimeOnly startTime,
        Guid? excludeAvailabilityId = null,
        CancellationToken cancellationToken = default)
    {
        bool exists = store.Availabilities.Values.Any(a =>
            a.ExperienceId == experienceId
            && a.Date == date
            && a.StartTime == startTime
            && (!excludeAvailabilityId.HasValue || a.Id != excludeAvailabilityId.Value));

        return Task.FromResult(exists);
    }

    public Task AddAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        store.Availabilities[availability.Id] = availability;
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Availability> availabilities, CancellationToken cancellationToken = default)
    {
        foreach (Availability a in availabilities)
        {
            store.Availabilities[a.Id] = a;
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Availability availability, CancellationToken cancellationToken = default)
    {
        store.Availabilities[availability.Id] = availability;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExperienceSchedule>> GetSchedulesByExperienceAsync(
        Guid experienceId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExperienceSchedule> list = store.ExperienceSchedules.Values
            .Where(s => s.ExperienceId == experienceId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<ExperienceSchedule?> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _ = store.ExperienceSchedules.TryGetValue(id, out ExperienceSchedule? s);
        return Task.FromResult(s);
    }

    public Task AddScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default)
    {
        store.ExperienceSchedules[schedule.Id] = schedule;
        return Task.CompletedTask;
    }

    public Task DeleteScheduleAsync(ExperienceSchedule schedule, CancellationToken cancellationToken = default)
    {
        _ = store.ExperienceSchedules.TryRemove(schedule.Id, out _);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private void PopulateExperience(Availability availability)
    {
        if (store.Experiences.TryGetValue(availability.ExperienceId, out Experience? exp))
        {
            if (store.Companies.TryGetValue(exp.CompanyId, out Company? comp))
            {
                if (store.Users.TryGetValue(comp.OperatorId, out User? op))
                {
                    comp.Operator = op;
                }

                exp.Company = comp;
            }

            availability.Experience = exp;
        }
    }
}

internal sealed class FakeFavoriteRepository(InMemoryUserStore store) : IFavoriteRepository
{
    public Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        List<Favorite> items = store.Favorites.Values
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Favorite>>(items);
    }

    public Task<IReadOnlyList<Guid>> GetExperienceIdsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> ids = store.Favorites.Values
            .Where(f => f.UserId == userId)
            .Select(f => f.ExperienceId)
            .ToList();

        return Task.FromResult(ids);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(store.Favorites.ContainsKey((userId, experienceId)));
    }

    public Task AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        store.Favorites[(favorite.UserId, favorite.ExperienceId)] = favorite;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid userId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        _ = store.Favorites.TryRemove((userId, experienceId), out _);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class FakeReservationRepository(InMemoryUserStore store) : IReservationRepository
{
    public Task<Reservation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!store.Reservations.TryGetValue(id, out Reservation? r))
        {
            return Task.FromResult<Reservation?>(null);
        }

        PopulateRelations(r);
        return Task.FromResult<Reservation?>(r);
    }

    public Task<Reservation?> GetByCodeWithDetailsAsync(string code, CancellationToken cancellationToken = default)
    {
        Reservation? r = store.Reservations.Values.FirstOrDefault(
            x => string.Equals(x.ReservationCode, code.Trim(), StringComparison.OrdinalIgnoreCase));

        if (r is not null)
        {
            PopulateRelations(r);
        }

        return Task.FromResult(r);
    }

    public Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByVisitorPagedAsync(
        Guid visitorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Reservation> query = store.Reservations.Values.Where(r => r.VisitorId == visitorId);
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        List<Reservation> all = query.OrderByDescending(r => r.CreatedAt).ToList();
        List<Reservation> page = all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        foreach (Reservation item in page)
        {
            PopulateRelations(item);
        }

        return Task.FromResult(((IReadOnlyList<Reservation>)page, all.Count));
    }

    public Task<(IReadOnlyList<Reservation> Items, int TotalRecords)> GetByOperatorPagedAsync(
        Guid? operatorId,
        int pageNumber,
        int pageSize,
        ReservationStatus? status = null,
        Guid? experienceId = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        List<Reservation> all = store.Reservations.Values.ToList();
        foreach (Reservation r in all)
        {
            PopulateRelations(r);
        }

        IEnumerable<Reservation> query = all;
        if (operatorId.HasValue)
        {
            query = query.Where(r => r.Experience?.Company?.OperatorId == operatorId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (experienceId.HasValue)
        {
            query = query.Where(r => r.ExperienceId == experienceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            query = query.Where(r =>
                r.ReservationCode.Contains(s, StringComparison.OrdinalIgnoreCase)
                || r.ContactName.Contains(s, StringComparison.OrdinalIgnoreCase)
                || (r.Experience?.Title.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        List<Reservation> filtered = query.OrderByDescending(r => r.CreatedAt).ToList();
        List<Reservation> page = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(((IReadOnlyList<Reservation>)page, filtered.Count));
    }

    public Task<IReadOnlyList<Reservation>> GetActiveFutureByAvailabilityAsync(
        Guid availabilityId,
        CancellationToken cancellationToken = default)
    {
        List<Reservation> list = store.Reservations.Values
            .Where(r => r.AvailabilityId == availabilityId
                && (r.Status == ReservationStatus.PENDIENTE || r.Status == ReservationStatus.CONFIRMADA))
            .ToList();

        foreach (Reservation r in list)
        {
            PopulateRelations(r);
        }

        return Task.FromResult<IReadOnlyList<Reservation>>(list);
    }

    public Task<IReadOnlyList<Reservation>> GetActiveFutureByExperienceAsync(
        Guid experienceId,
        DateOnly minDate,
        CancellationToken cancellationToken = default)
    {
        List<Reservation> list = store.Reservations.Values
            .Where(r => r.ExperienceId == experienceId
                && (r.Status == ReservationStatus.PENDIENTE || r.Status == ReservationStatus.CONFIRMADA))
            .ToList();

        List<Reservation> matched = [];
        foreach (Reservation r in list)
        {
            PopulateRelations(r);
            if (r.Availability is not null && r.Availability.Date >= minDate)
            {
                matched.Add(r);
            }
        }

        return Task.FromResult<IReadOnlyList<Reservation>>(matched);
    }

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        store.Reservations[reservation.Id] = reservation;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        store.Reservations[reservation.Id] = reservation;
        return Task.CompletedTask;
    }

    public Task AddHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default)
    {
        store.ReservationHistories[history.Id] = history;
        return Task.CompletedTask;
    }

    public Task AddRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        store.Refunds[refund.Id] = refund;
        return Task.CompletedTask;
    }

    public Task UpdateRefundAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        store.Refunds[refund.Id] = refund;
        return Task.CompletedTask;
    }

    public Task AddIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default)
    {
        store.ReservationIncidents[incident.Id] = incident;
        return Task.CompletedTask;
    }

    public Task UpdateIncidentAsync(ReservationIncident incident, CancellationToken cancellationToken = default)
    {
        store.ReservationIncidents[incident.Id] = incident;
        return Task.CompletedTask;
    }

    public Task<Refund?> GetRefundByIdOrCodeAsync(string idOrCode, CancellationToken cancellationToken = default)
    {
        string trimmed = idOrCode.Trim();
        Refund? refund = Guid.TryParse(trimmed, out Guid id)
            ? store.Refunds.GetValueOrDefault(id)
            : store.Refunds.Values.FirstOrDefault(rf =>
                string.Equals(rf.RefundCode, trimmed, StringComparison.OrdinalIgnoreCase));

        if (refund is not null && store.Reservations.TryGetValue(refund.ReservationId, out Reservation? res))
        {
            PopulateRelations(res);
            refund.Reservation = res;
        }

        return Task.FromResult(refund);
    }

    public Task<IReadOnlyList<Refund>> GetRefundsByOperatorAsync(Guid? operatorId, CancellationToken cancellationToken = default)
    {
        List<Refund> items = [];
        foreach (Refund rf in store.Refunds.Values.OrderByDescending(x => x.RequestedAt))
        {
            if (store.Reservations.TryGetValue(rf.ReservationId, out Reservation? res))
            {
                PopulateRelations(res);
                rf.Reservation = res;
            }

            if (!operatorId.HasValue || rf.Reservation?.Experience?.Company?.OperatorId == operatorId.Value)
            {
                items.Add(rf);
            }
        }

        return Task.FromResult<IReadOnlyList<Refund>>(items);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await operation(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    private void PopulateRelations(Reservation r)
    {
        if (store.Experiences.TryGetValue(r.ExperienceId, out Experience? exp))
        {
            if (store.Companies.TryGetValue(exp.CompanyId, out Company? comp))
            {
                if (store.Users.TryGetValue(comp.OperatorId, out User? op))
                {
                    comp.Operator = op;
                }

                exp.Company = comp;
            }

            if (store.Municipalities.TryGetValue(exp.MunicipalityId, out Municipality? mun))
            {
                if (store.Departments.TryGetValue(mun.DepartmentId, out Department? dept))
                {
                    mun.Department = dept;
                }

                exp.Municipality = mun;
            }

            exp.Images = store.ExperienceImages.Values
                .Where(i => i.ExperienceId == exp.Id && i.RemovedAt == null)
                .OrderBy(i => i.SortOrder)
                .ToList();

            r.Experience = exp;
        }

        if (store.Availabilities.TryGetValue(r.AvailabilityId, out Availability? avail))
        {
            r.Availability = avail;
        }

        if (r.VisitorId.HasValue && store.Users.TryGetValue(r.VisitorId.Value, out User? visitor))
        {
            r.Visitor = visitor;
        }

        r.Refund = store.Refunds.Values.FirstOrDefault(rf => rf.ReservationId == r.Id);
        if (r.Refund is not null)
        {
            r.Refund.Reservation = r;
        }

        r.History = store.ReservationHistories.Values
            .Where(h => h.ReservationId == r.Id)
            .OrderByDescending(h => h.CreatedAt)
            .ToList();

        r.Incidents = store.ReservationIncidents.Values
            .Where(i => i.ReservationId == r.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }
}
