using Microsoft.EntityFrameworkCore;

using RutaSV.Domain.Identity.Model;

namespace RutaSV.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<EmailNotification> EmailNotifications => Set<EmailNotification>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<OperatorRequest> OperatorRequests => Set<OperatorRequest>();
    public DbSet<OperatorRequestDocument> OperatorRequestDocuments => Set<OperatorRequestDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
