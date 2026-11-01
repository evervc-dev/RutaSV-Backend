using FluentAssertions;

using Moq;

using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Repository;
using RutaSV.Application.Identity.Service;
using RutaSV.Application.Identity.Validation;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Exception;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.UnitTests.Identity;

public sealed class OperatorRequestServiceTests
{
    private readonly Mock<IOperatorRequestRepository> _operatorRequestRepositoryMock = new();
    private readonly Mock<ICompanyRepository> _companyRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly OperatorRequestService _sut;

    public OperatorRequestServiceTests()
    {
        _operatorRequestRepositoryMock
            .Setup(r => r.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

        _sut = new OperatorRequestService(
            _operatorRequestRepositoryMock.Object,
            _companyRepositoryMock.Object,
            _userRepositoryMock.Object,
            new CreateOperatorRequestRequestValidator(),
            new GetOperatorRequestsQueryValidator(),
            new ReviewOperatorRequestRequestValidator());
    }

    [Fact]
    public async Task CreateAsync_WhenValidAndNoPendingRequest_ShouldCreateCompanyRequestAndDocumentsInTransaction()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = new()
        {
            Id = userId,
            Email = "solicitante@rutasv.com",
            FirstName = "Mario",
            LastName = "Bolaños",
            Role = UserRole.VISITANTE,
            Status = UserStatus.ACTIVO
        };

        Company? capturedCompany = null;
        OperatorRequest? capturedRequest = null;
        List<OperatorRequestDocument>? capturedDocuments = null;

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _operatorRequestRepositoryMock
            .Setup(r => r.HasPendingRequestByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _companyRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, _) => capturedCompany = c)
            .Returns(Task.CompletedTask);

        _operatorRequestRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<OperatorRequest>(), It.IsAny<CancellationToken>()))
            .Callback<OperatorRequest, CancellationToken>((req, _) => capturedRequest = req)
            .Returns(Task.CompletedTask);

        _operatorRequestRepositoryMock
            .Setup(r => r.AddDocumentsAsync(It.IsAny<IEnumerable<OperatorRequestDocument>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<OperatorRequestDocument>, CancellationToken>((docs, _) => capturedDocuments = docs.ToList())
            .Returns(Task.CompletedTask);

        CreateOperatorRequestRequest request = new(
            new CreateOperatorCompanyRequest(
                "Surf El Tunco SV",
                "Surf El Tunco S.A. de C.V.",
                "Clases profesionales de surf en La Libertad",
                "info@surfeltunco.sv",
                "+50370001111"),
            "Adjunto documentación de respaldo.",
            [
                new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui-frente-123.jpg"),
                new CreateOperatorRequestDocumentRequest("FOTO_NEGOCIO", "private/local-playa.jpg")
            ]);

        // Act
        OperatorRequestDto result = await _sut.CreateAsync(userId, request);

        // Assert
        result.Status.Should().Be("PENDIENTE");
        result.UserId.Should().Be(userId);
        result.Company.TradeName.Should().Be("Surf El Tunco SV");
        result.Company.Status.Should().Be("PENDIENTE");
        result.Documents.Should().HaveCount(2);

        capturedCompany.Should().NotBeNull();
        capturedCompany!.Status.Should().Be(CompanyStatus.PENDIENTE);
        capturedCompany.OperatorId.Should().Be(userId);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Status.Should().Be(OperatorRequestStatus.PENDIENTE);
        capturedRequest.CompanyId.Should().Be(capturedCompany.Id);

        capturedDocuments.Should().NotBeNull();
        capturedDocuments!.Should().HaveCount(2);

        _operatorRequestRepositoryMock.Verify(
            r => r.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUserAlreadyHasPendingRequest_ShouldThrowPendingOperatorRequestAlreadyExistsException()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        User user = new()
        {
            Id = userId,
            Email = "solicitante@rutasv.com",
            FirstName = "Mario",
            LastName = "Bolaños"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _operatorRequestRepositoryMock
            .Setup(r => r.HasPendingRequestByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CreateOperatorRequestRequest request = new(
            new CreateOperatorCompanyRequest("Empresa Duplicada"),
            "Solicitud repetida",
            [new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui.jpg")]);

        // Act
        Func<Task> act = async () => _ = await _sut.CreateAsync(userId, request);

        // Assert
        _ = await act.Should().ThrowAsync<PendingOperatorRequestAlreadyExistsException>();
        _companyRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_WhenCalled_ShouldReturnPagedResponseWithCalculatedTotalPages()
    {
        // Arrange
        User applicant = new()
        {
            Id = Guid.NewGuid(),
            Email = "turista@rutasv.com",
            FirstName = "Sofía",
            LastName = "Méndez",
            Role = UserRole.VISITANTE,
            Status = UserStatus.ACTIVO
        };

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = applicant.Id,
            TradeName = "Café de Altura Apaneca",
            Status = CompanyStatus.PENDIENTE
        };

        OperatorRequest item = new()
        {
            Id = Guid.NewGuid(),
            UserId = applicant.Id,
            CompanyId = company.Id,
            Status = OperatorRequestStatus.PENDIENTE,
            User = applicant,
            Company = company,
            Documents = []
        };

        _operatorRequestRepositoryMock
            .Setup(r => r.GetPagedAsync(2, 10, OperatorRequestStatus.PENDIENTE, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([item], 25));

        GetOperatorRequestsQuery query = new(2, 10, "PENDIENTE");

        // Act
        PagedResponse<OperatorRequestDto> response = await _sut.GetPagedAsync(query);

        // Assert
        response.CurrentPage.Should().Be(2);
        response.PageSize.Should().Be(10);
        response.TotalRecords.Should().Be(25);
        response.TotalPages.Should().Be(3);
        response.Data.Should().ContainSingle();
    }

    [Fact]
    public async Task ReviewAsync_WhenApproved_ShouldSetRequestAndCompanyApprovedAndUserRoleOperador()
    {
        // Arrange
        Guid adminId = Guid.NewGuid();
        User applicant = new()
        {
            Id = Guid.NewGuid(),
            Email = "aspirante@rutasv.com",
            FirstName = "Ricardo",
            LastName = "Poma",
            Role = UserRole.VISITANTE,
            Status = UserStatus.ACTIVO
        };

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = applicant.Id,
            TradeName = "Rutas de Suchitoto",
            Status = CompanyStatus.PENDIENTE
        };

        OperatorRequest pendingRequest = new()
        {
            Id = Guid.NewGuid(),
            UserId = applicant.Id,
            CompanyId = company.Id,
            Status = OperatorRequestStatus.PENDIENTE,
            User = applicant,
            Company = company,
            Documents = []
        };

        _operatorRequestRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(pendingRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRequest);

        ReviewOperatorRequestRequest reviewRequest = new(true);

        // Act
        OperatorRequestDto result = await _sut.ReviewAsync(pendingRequest.Id, adminId, reviewRequest);

        // Assert
        result.Status.Should().Be("APROBADA");
        result.Company.Status.Should().Be("APROBADA");
        result.User.Role.Should().Be("OPERADOR");
        result.ReviewedBy.Should().Be(adminId);

        pendingRequest.Status.Should().Be(OperatorRequestStatus.APROBADA);
        company.Status.Should().Be(CompanyStatus.APROBADA);
        applicant.Role.Should().Be(UserRole.OPERADOR);

        _userRepositoryMock.Verify(r => r.UpdateAsync(applicant, It.IsAny<CancellationToken>()), Times.Once);
        _companyRepositoryMock.Verify(r => r.UpdateAsync(company, It.IsAny<CancellationToken>()), Times.Once);
        _operatorRequestRepositoryMock.Verify(r => r.UpdateAsync(pendingRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_WhenRejected_ShouldSetRequestAndCompanyRejectedWithReasonAndKeepUserRole()
    {
        // Arrange
        Guid adminId = Guid.NewGuid();
        User applicant = new()
        {
            Id = Guid.NewGuid(),
            Email = "aspirante@rutasv.com",
            FirstName = "Ricardo",
            LastName = "Poma",
            Role = UserRole.VISITANTE,
            Status = UserStatus.ACTIVO
        };

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = applicant.Id,
            TradeName = "Rutas de Suchitoto",
            Status = CompanyStatus.PENDIENTE
        };

        OperatorRequest pendingRequest = new()
        {
            Id = Guid.NewGuid(),
            UserId = applicant.Id,
            CompanyId = company.Id,
            Status = OperatorRequestStatus.PENDIENTE,
            User = applicant,
            Company = company,
            Documents = []
        };

        _operatorRequestRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(pendingRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingRequest);

        ReviewOperatorRequestRequest reviewRequest = new(
            false,
            "La imagen del DUI es ilegible.");

        // Act
        OperatorRequestDto result = await _sut.ReviewAsync(pendingRequest.Id, adminId, reviewRequest);

        // Assert
        result.Status.Should().Be("RECHAZADA");
        result.RejectionReason.Should().Be("La imagen del DUI es ilegible.");
        result.Company.Status.Should().Be("RECHAZADA");
        result.Company.RejectionReason.Should().Be("La imagen del DUI es ilegible.");
        result.User.Role.Should().Be("VISITANTE");

        applicant.Role.Should().Be(UserRole.VISITANTE);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _companyRepositoryMock.Verify(r => r.UpdateAsync(company, It.IsAny<CancellationToken>()), Times.Once);
        _operatorRequestRepositoryMock.Verify(r => r.UpdateAsync(pendingRequest, It.IsAny<CancellationToken>()), Times.Once);
    }
}
