using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        _ = builder.ToTable("category");

        _ = builder.HasKey(c => c.Id);

        _ = builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasColumnType("integer");

        _ = builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(60)
            .IsRequired();

        _ = builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("uq_category_name");

        _ = builder.Property(c => c.Slug)
            .HasColumnName("slug")
            .HasMaxLength(80)
            .IsRequired();

        _ = builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("uq_category_slug");

        _ = builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(300);

        _ = builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .HasMaxLength(50);

        _ = builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        _ = builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("smallint")
            .HasDefaultValue((short)0)
            .IsRequired();

        _ = builder.HasData(
            new Category { Id = 1, Name = "Playa", Slug = "playa", Description = "Experiencias frente al mar y costas salvadoreñas", Icon = "beach", IsActive = true, SortOrder = 1 },
            new Category { Id = 2, Name = "Montaña", Slug = "montana", Description = "Rutas de altura, volcanes y bosques nebulosos", Icon = "mountain", IsActive = true, SortOrder = 2 },
            new Category { Id = 3, Name = "Surf", Slug = "surf", Description = "Clases y campamentos de surf en Surf City", Icon = "waves", IsActive = true, SortOrder = 3 },
            new Category { Id = 4, Name = "Caminata", Slug = "caminata", Description = "Senderismo guiado y ecoturismo", Icon = "hiking", IsActive = true, SortOrder = 4 },
            new Category { Id = 5, Name = "Comida", Slug = "comida", Description = "Gastronomía local, café y experiencias culinarias", Icon = "utensils", IsActive = true, SortOrder = 5 },
            new Category { Id = 6, Name = "Cultura", Slug = "cultura", Description = "Pueblos vivos, historia y artesanías", Icon = "landmark", IsActive = true, SortOrder = 6 },
            new Category { Id = 7, Name = "Aventura", Slug = "aventura", Description = "Canopy, rapel, kayak y deportes extremos", Icon = "compass", IsActive = true, SortOrder = 7 },
            new Category { Id = 8, Name = "Paseo en lancha", Slug = "paseo-en-lancha", Description = "Recorridos por lagos, esteros y golfos", Icon = "ship", IsActive = true, SortOrder = 8 },
            new Category { Id = 9, Name = "Museo", Slug = "museo", Description = "Sitios arqueológicos y museos nacionales", Icon = "building", IsActive = true, SortOrder = 9 },
            new Category { Id = 10, Name = "Parque", Slug = "parque", Description = "Parques nacionales y reservas naturales", Icon = "trees", IsActive = true, SortOrder = 10 });
    }
}
