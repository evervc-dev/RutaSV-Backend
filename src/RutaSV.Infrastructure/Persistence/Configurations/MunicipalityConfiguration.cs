using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RutaSV.Domain.Catalog.Model;

namespace RutaSV.Infrastructure.Persistence.Configurations;

public sealed class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        _ = builder.ToTable("municipality");

        _ = builder.HasKey(m => m.Id);

        _ = builder.Property(m => m.Id)
            .HasColumnName("id")
            .HasColumnType("integer")
            .ValueGeneratedNever();

        _ = builder.Property(m => m.DepartmentId)
            .HasColumnName("department_id")
            .HasColumnType("smallint")
            .IsRequired();

        _ = builder.Property(m => m.Name)
            .HasColumnName("name")
            .HasMaxLength(80)
            .IsRequired();

        _ = builder.HasIndex(m => new { m.DepartmentId, m.Name })
            .IsUnique()
            .HasDatabaseName("uq_municipality_department_id_name");

        _ = builder.HasOne(m => m.Department)
            .WithMany(d => d.Municipalities)
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        _ = builder.HasData(
            new Municipality { Id = 1, DepartmentId = 1, Name = "Ahuachapán" },
            new Municipality { Id = 2, DepartmentId = 1, Name = "Apaneca" },
            new Municipality { Id = 3, DepartmentId = 1, Name = "Concepción de Ataco" },
            new Municipality { Id = 4, DepartmentId = 2, Name = "Santa Ana" },
            new Municipality { Id = 5, DepartmentId = 2, Name = "Chalchuapa" },
            new Municipality { Id = 6, DepartmentId = 2, Name = "Metapán" },
            new Municipality { Id = 7, DepartmentId = 3, Name = "Sonsonate" },
            new Municipality { Id = 8, DepartmentId = 3, Name = "Juayúa" },
            new Municipality { Id = 9, DepartmentId = 3, Name = "Nahuizalco" },
            new Municipality { Id = 10, DepartmentId = 3, Name = "Acajutla" },
            new Municipality { Id = 11, DepartmentId = 4, Name = "Chalatenango" },
            new Municipality { Id = 12, DepartmentId = 4, Name = "La Palma" },
            new Municipality { Id = 13, DepartmentId = 4, Name = "San Ignacio" },
            new Municipality { Id = 14, DepartmentId = 5, Name = "Santa Tecla" },
            new Municipality { Id = 15, DepartmentId = 5, Name = "La Libertad" },
            new Municipality { Id = 16, DepartmentId = 5, Name = "Tamanique" },
            new Municipality { Id = 17, DepartmentId = 5, Name = "Antiguo Cuscatlán" },
            new Municipality { Id = 18, DepartmentId = 6, Name = "San Salvador" },
            new Municipality { Id = 19, DepartmentId = 6, Name = "Panchimalco" },
            new Municipality { Id = 20, DepartmentId = 6, Name = "Ilopango" },
            new Municipality { Id = 21, DepartmentId = 7, Name = "Cojutepeque" },
            new Municipality { Id = 22, DepartmentId = 7, Name = "Suchitoto" },
            new Municipality { Id = 23, DepartmentId = 8, Name = "Zacatecoluca" },
            new Municipality { Id = 24, DepartmentId = 8, Name = "San Luis Talpa" },
            new Municipality { Id = 25, DepartmentId = 9, Name = "Sensuntepeque" },
            new Municipality { Id = 26, DepartmentId = 9, Name = "Ilobasco" },
            new Municipality { Id = 27, DepartmentId = 10, Name = "San Vicente" },
            new Municipality { Id = 28, DepartmentId = 11, Name = "Usulután" },
            new Municipality { Id = 29, DepartmentId = 11, Name = "Alegría" },
            new Municipality { Id = 30, DepartmentId = 11, Name = "Jiquilisco" },
            new Municipality { Id = 31, DepartmentId = 12, Name = "San Miguel" },
            new Municipality { Id = 32, DepartmentId = 13, Name = "San Francisco Gotera" },
            new Municipality { Id = 33, DepartmentId = 13, Name = "Perquín" },
            new Municipality { Id = 34, DepartmentId = 14, Name = "La Unión" },
            new Municipality { Id = 35, DepartmentId = 14, Name = "Conchagua" });
    }
}
