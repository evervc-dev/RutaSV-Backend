using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.Domain.Catalog.Model;

public class Experience
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Includes { get; set; }
    public string? Excludes { get; set; }
    public string? WhatToBring { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int? DurationMinutes { get; set; }
    public DifficultyLevel? Difficulty { get; set; }
    public short? MinAge { get; set; }
    public string? ServiceLanguage { get; set; }
    public bool HasLimitedCapacity { get; set; } = true;
    public short MinPeople { get; set; } = 1;
    public short? MaxPeople { get; set; } = 10;
    public int MunicipalityId { get; set; }
    public string? ReferenceAddress { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public bool AutoConfirmation { get; set; }
    public ExperienceStatus Status { get; set; } = ExperienceStatus.BORRADOR;
    public Guid? WithdrawnBy { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public string? WithdrawalReason { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public Company? Company { get; set; }
    public Municipality? Municipality { get; set; }
    public User? WithdrawnByUser { get; set; }
    public ICollection<ExperienceCategory> Categories { get; set; } = [];
    public ICollection<ExperienceImage> Images { get; set; } = [];
    public ICollection<ExperienceSchedule> Schedules { get; set; } = [];
    public ICollection<Availability> Availabilities { get; set; } = [];
    public ICollection<Favorite> Favorites { get; set; } = [];
}
