using Microsoft.EntityFrameworkCore;

using RutaSV.Domain.Bookings.Model;
using RutaSV.Domain.Catalog.Model;
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
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<ExperienceCategory> ExperienceCategories => Set<ExperienceCategory>();
    public DbSet<ExperienceImage> ExperienceImages => Set<ExperienceImage>();
    public DbSet<ExperienceSchedule> ExperienceSchedules => Set<ExperienceSchedule>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationHistory> ReservationHistories => Set<ReservationHistory>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<ReservationIncident> ReservationIncidents => Set<ReservationIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
