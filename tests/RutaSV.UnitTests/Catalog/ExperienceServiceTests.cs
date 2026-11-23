using FluentAssertions;

using Moq;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Catalog.Repository;
using RutaSV.Application.Catalog.Service;
using RutaSV.Application.Catalog.Validation;
using RutaSV.Application.Identity.Repository;
using RutaSV.Domain.Catalog.Enum;
using RutaSV.Domain.Catalog.Exception;
using RutaSV.Domain.Catalog.Model;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.UnitTests.Catalog;

public sealed class ExperienceServiceTests
{
    private readonly Mock<IExperienceRepository> _experienceRepositoryMock = new();
    private readonly Mock<ICatalogLookupRepository> _catalogLookupRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly ExperienceService _sut;

    public ExperienceServiceTests()
    {
        _experienceRepositoryMock
            .Setup(r => r.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        _sut = new ExperienceService(
            _experienceRepositoryMock.Object,
            _catalogLookupRepositoryMock.Object,
            _companyRepositoryMock.Object,
            new CreateExperienceRequestValidator(),
            new UpdateExperienceRequestValidator(),
            new GetPublicExperiencesQueryValidator(),
            new GetMyExperiencesQueryValidator());
    }

    [Fact]
    public async Task CreateAsync_WhenUnlimitedCapacityAndDraft_ShouldSaveExperienceWithNullMaxPeopleAndCoordinates()
    {
        // Arrange
        Guid operatorId = Guid.NewGuid();
        Company approvedCompany = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = operatorId,
            TradeName = "Rutas Culturales SV",
            Status = CompanyStatus.APROBADA
        };

        Municipality municipality = new()
        {
            Id = 14,
            DepartmentId = 5,
            Name = "Santa Tecla",
            Department = new Department { Id = 5, Name = "La Libertad" }
        };

        Experience? capturedExperience = null;

        _companyRepositoryMock
            .Setup(r => r.GetByOperatorIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([approvedCompany]);

        _catalogLookupRepositoryMock
            .Setup(r => r.GetMunicipalityByIdAsync(14, It.IsAny<CancellationToken>()))
            .ReturnsAsync(municipality);

        _experienceRepositoryMock
            .Setup(r => r.ExistsBySlugAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _experienceRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Experience>(), It.IsAny<CancellationToken>()))
            .Callback<Experience, CancellationToken>((exp, _) => capturedExperience = exp)
            .Returns(Task.CompletedTask);

        CreateExperienceRequest request = new(
            "Tour del Café en El Boquerón",
            "Recorrido aromático por fincas cafetaleras.",
            "Conoce el proceso completo del café salvadoreño de altura en las faldas del volcán.",
            18.50m,
            14,
            13.733300m,
            -89.266700m,
            HasLimitedCapacity: false,
            MinPeople: 1,
            MaxPeople: 25,
            PublishImmediately: false);

        // Act
        ExperienceDto result = await _sut.CreateAsync(operatorId, isAdmin: false, request);

        // Assert
        result.Status.Should().Be("BORRADOR");
        result.Slug.Should().Be("tour-del-cafe-en-el-boqueron");
        result.HasLimitedCapacity.Should().BeFalse();
        result.MaxPeople.Should().BeNull();
        result.Latitude.Should().Be(13.733300m);
        result.Longitude.Should().Be(-89.266700m);

        capturedExperience.Should().NotBeNull();
        capturedExperience!.HasLimitedCapacity.Should().BeFalse();
        capturedExperience.MaxPeople.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WhenOperatorHasNoApprovedCompany_ShouldThrowExperienceAccessDeniedException()
    {
        // Arrange
        Guid operatorId = Guid.NewGuid();
        _companyRepositoryMock
            .Setup(r => r.GetByOperatorIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        CreateExperienceRequest request = new(
            "Kayak en Ilopango",
            "Paseo en kayak",
            "Descripción completa del paseo en kayak por el lago de Ilopango.",
            25.00m,
            20,
            13.670000m,
            -89.050000m);

        // Act
        Func<Task> act = async () => _ = await _sut.CreateAsync(operatorId, isAdmin: false, request);

        // Assert
        _ = await act.Should().ThrowAsync<ExperienceAccessDeniedException>();
    }

    [Fact]
    public async Task PublishAsync_WhenMissingCategoriesCoverImageAndInvalidElSalvadorCoords_ShouldThrowIncompleteExceptionWithMissingFields()
    {
        // Arrange
        Guid operatorId = Guid.NewGuid();
        Guid experienceId = Guid.NewGuid();

        Experience incompleteDraft = new()
        {
            Id = experienceId,
            CompanyId = Guid.NewGuid(),
            Title = "Borrador Incompleto",
            Slug = "borrador-incompleto",
            ShortDescription = "Corto",
            Description = "Muy corta",
            Price = 20m,
            HasLimitedCapacity = true,
            MinPeople = 1,
            MaxPeople = 10,
            MunicipalityId = 14,
            Latitude = 0m,
            Longitude = 0m,
            Status = ExperienceStatus.BORRADOR,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                OperatorId = operatorId,
                Status = CompanyStatus.APROBADA
            },
            Municipality = new Municipality { Id = 14, DepartmentId = 5, Name = "Santa Tecla" },
            Categories = [],
            Images = []
        };

        _experienceRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(experienceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incompleteDraft);

        // Act
        Func<Task> act = async () => _ = await _sut.PublishAsync(operatorId, isAdmin: false, experienceId);

        // Assert
        var exceptionAssertion = await act.Should().ThrowAsync<ExperienceIncompleteForPublicationException>();
        exceptionAssertion.Which.MissingFields.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task PublishAsync_WhenAllRequirementsMet_ShouldSetStatusPublicadaAndPublishedAt()
    {
        // Arrange
        Guid operatorId = Guid.NewGuid();
        Guid experienceId = Guid.NewGuid();

        Category surfCategory = new() { Id = 3, Name = "Surf", Slug = "surf", IsActive = true };
        Experience completeDraft = new()
        {
            Id = experienceId,
            CompanyId = Guid.NewGuid(),
            Title = "Clases de Surf en El Tunco",
            Slug = "clases-de-surf-en-el-tunco",
            ShortDescription = "Clases personalizadas en La Libertad.",
            Description = "Aprende las bases del surf con instructores locales certificados en Playa El Tunco.",
            Price = 35m,
            HasLimitedCapacity = true,
            MinPeople = 1,
            MaxPeople = 6,
            MunicipalityId = 16,
            Latitude = 13.492900m,
            Longitude = -89.383100m,
            Status = ExperienceStatus.BORRADOR,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                OperatorId = operatorId,
                Status = CompanyStatus.APROBADA
            },
            Municipality = new Municipality { Id = 16, DepartmentId = 5, Name = "Tamanique" },
            Categories =
            [
                new ExperienceCategory { ExperienceId = experienceId, CategoryId = 3, IsPrimary = true, Category = surfCategory }
            ],
            Images =
            [
                new ExperienceImage
                {
                    Id = Guid.NewGuid(),
                    ExperienceId = experienceId,
                    ObjectKey = "experiences/cover.jpg",
                    Url = "/api/experiences/images/file?objectKey=experiences%2Fcover.jpg",
                    IsCover = true
                }
            ]
        };

        _experienceRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(experienceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completeDraft);

        // Act
        ExperienceDto result = await _sut.PublishAsync(operatorId, isAdmin: false, experienceId);

        // Assert
        result.Status.Should().Be("PUBLICADA");
        result.PublishedAt.Should().NotBeNull();
        _experienceRepositoryMock.Verify(r => r.UpdateAsync(completeDraft, It.IsAny<CancellationToken>()), Times.Once);
        _experienceRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnerDeletes_ShouldArchiveAndSetDeletedAt()
    {
        // Arrange
        Guid operatorId = Guid.NewGuid();
        Guid experienceId = Guid.NewGuid();

        Experience existing = new()
        {
            Id = experienceId,
            CompanyId = Guid.NewGuid(),
            Title = "Experiencia a Eliminar",
            Status = ExperienceStatus.PUBLICADA,
            Company = new Company
            {
                Id = Guid.NewGuid(),
                OperatorId = operatorId,
                Status = CompanyStatus.APROBADA
            }
        };

        _experienceRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(experienceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        await _sut.DeleteAsync(operatorId, isAdmin: false, experienceId);

        // Assert
        existing.Status.Should().Be(ExperienceStatus.ARCHIVADA);
        existing.DeletedAt.Should().NotBeNull();
        _experienceRepositoryMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _experienceRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
