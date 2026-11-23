using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        _ = builder.ToTable("department");

        _ = builder.HasKey(d => d.Id);

        _ = builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasColumnType("smallint")
            .ValueGeneratedNever();

        _ = builder.Property(d => d.Name)
            .HasColumnName("name")
            .HasMaxLength(60)
            .IsRequired();

        _ = builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("uq_department_name");

        _ = builder.HasData(
            new Department { Id = 1, Name = "Ahuachapán" },
            new Department { Id = 2, Name = "Santa Ana" },
            new Department { Id = 3, Name = "Sonsonate" },
            new Department { Id = 4, Name = "Chalatenango" },
            new Department { Id = 5, Name = "La Libertad" },
            new Department { Id = 6, Name = "San Salvador" },
            new Department { Id = 7, Name = "Cuscatlán" },
            new Department { Id = 8, Name = "La Paz" },
            new Department { Id = 9, Name = "Cabañas" },
            new Department { Id = 10, Name = "San Vicente" },
            new Department { Id = 11, Name = "Usulután" },
            new Department { Id = 12, Name = "San Miguel" },
            new Department { Id = 13, Name = "Morazán" },
            new Department { Id = 14, Name = "La Unión" });
    }
}
