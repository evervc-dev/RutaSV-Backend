using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.DTO;
using RutaSV.Application.Identity.Security;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;

namespace RutaSV.IntegrationTests.Identity;

public sealed class OperatorRequestsEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OperatorRequestsEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOperatorRequest_WhenUnauthenticated_ShouldReturn401Unauthorized()
    {
        // Arrange
        CreateOperatorRequestRequest payload = new(
            new CreateOperatorCompanyRequest("Volcán Tours SV"),
            "Solicitud sin token",
            [new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui.jpg")]);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/operator-requests", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOperatorRequest_WhenAuthenticatedAndDuplicatePending_ShouldReturn201Then409Conflict()
    {
        // Arrange
        (User visitor, string token) = CreateAuthenticatedUser("visitante.op@rutasv.com", UserRole.VISITANTE);

        CreateOperatorRequestRequest payload = new(
            new CreateOperatorCompanyRequest(
                "Kayak Lago de Coatepeque",
                "Kayak Coatepeque S.A. de C.V.",
                "Recorridos guiados en kayak por el lago",
                "reservas@kayakcoatepeque.sv",
                "+50378889999"),
            "Solicito alta como operador turístico.",
            [
                new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui-frente.jpg"),
                new CreateOperatorRequestDocumentRequest("FOTO_NEGOCIO", "private/kayaks.jpg")
            ]);

        using HttpRequestMessage firstRequest = new(HttpMethod.Post, "/api/operator-requests")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act 1: Primera solicitud exitosa
        HttpResponseMessage firstResponse = await _client.SendAsync(firstRequest);

        // Assert 1
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        OperatorRequestDto? created = await firstResponse.Content.ReadFromJsonAsync<OperatorRequestDto>();
        created.Should().NotBeNull();
        created!.UserId.Should().Be(visitor.Id);
        created.Status.Should().Be("PENDIENTE");
        created.Company.TradeName.Should().Be("Kayak Lago de Coatepeque");
        created.Company.Status.Should().Be("PENDIENTE");
        created.Documents.Should().HaveCount(2);

        // Act 2: Segunda solicitud mientras la primera sigue PENDIENTE
        using HttpRequestMessage duplicateRequest = new(HttpMethod.Post, "/api/operator-requests")
        {
            Content = JsonContent.Create(payload)
        };
        duplicateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage duplicateResponse = await _client.SendAsync(duplicateRequest);

        // Assert 2
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPaged_WhenVisitorRole_ShouldReturn403Forbidden_AndWhenAdminRole_ShouldReturn200WithPagedData()
    {
        // Arrange
        (_, string visitorToken) = CreateAuthenticatedUser("visitante.lectura@rutasv.com", UserRole.VISITANTE);
        (_, string adminToken) = CreateAuthenticatedUser("admin.lectura@rutasv.com", UserRole.ADMIN);

        CreateOperatorRequestRequest payload = new(
            new CreateOperatorCompanyRequest("Senderismo El Pital"),
            "Solicitud para revisión del admin",
            [new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui-pital.jpg")]);

        using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/operator-requests")
        {
            Content = JsonContent.Create(payload)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);
        _ = await _client.SendAsync(createMsg);

        // Act 1: Visitante intenta listar solicitudes
        using HttpRequestMessage visitorGet = new(HttpMethod.Get, "/api/operator-requests?pageNumber=1&pageSize=10");
        visitorGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);
        HttpResponseMessage forbiddenResponse = await _client.SendAsync(visitorGet);

        // Assert 1
        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 2: Admin lista solicitudes filtradas por estado PENDIENTE
        using HttpRequestMessage adminGet = new(HttpMethod.Get, "/api/operator-requests?pageNumber=1&pageSize=10&status=PENDIENTE");
        adminGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        HttpResponseMessage okResponse = await _client.SendAsync(adminGet);

        // Assert 2
        okResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<OperatorRequestDto>? paged = await okResponse.Content.ReadFromJsonAsync<PagedResponse<OperatorRequestDto>>();
        paged.Should().NotBeNull();
        paged!.CurrentPage.Should().Be(1);
        paged.PageSize.Should().Be(10);
        paged.TotalRecords.Should().BeGreaterThanOrEqualTo(1);
        paged.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Review_WhenAdminApproves_ShouldPromoteUserToOperadorAndApproveCompany()
    {
        // Arrange
        (User visitor, string visitorToken) = CreateAuthenticatedUser("visitante.aprobar@rutasv.com", UserRole.VISITANTE);
        (_, string adminToken) = CreateAuthenticatedUser("admin.aprobar@rutasv.com", UserRole.ADMIN);

        CreateOperatorRequestRequest createPayload = new(
            new CreateOperatorCompanyRequest("Buceo Los Cóbanos"),
            "Empresa de buceo certificado",
            [new CreateOperatorRequestDocumentRequest("REGISTRO_TURISMO", "private/mitur-cobanos.pdf")]);

        using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/operator-requests")
        {
            Content = JsonContent.Create(createPayload)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);
        HttpResponseMessage createResponse = await _client.SendAsync(createMsg);
        OperatorRequestDto? created = await createResponse.Content.ReadFromJsonAsync<OperatorRequestDto>();

        ReviewOperatorRequestRequest reviewPayload = new(true);
        using HttpRequestMessage reviewMsg = new(HttpMethod.Post, $"/api/operator-requests/{created!.Id}/review")
        {
            Content = JsonContent.Create(reviewPayload)
        };
        reviewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        HttpResponseMessage reviewResponse = await _client.SendAsync(reviewMsg);

        // Assert
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        OperatorRequestDto? reviewed = await reviewResponse.Content.ReadFromJsonAsync<OperatorRequestDto>();
        reviewed.Should().NotBeNull();
        reviewed!.Status.Should().Be("APROBADA");
        reviewed.Company.Status.Should().Be("APROBADA");
        reviewed.User.Role.Should().Be("OPERADOR");

        _factory.Store.Users[visitor.Id].Role.Should().Be(UserRole.OPERADOR);
        _factory.Store.Companies[created.CompanyId].Status.Should().Be(CompanyStatus.APROBADA);
    }

    [Fact]
    public async Task Review_WhenAdminRejects_ShouldSaveRejectionReasonAndMarkRejected()
    {
        // Arrange
        (User visitor, string visitorToken) = CreateAuthenticatedUser("visitante.rechazar@rutasv.com", UserRole.VISITANTE);
        (_, string adminToken) = CreateAuthenticatedUser("admin.rechazar@rutasv.com", UserRole.ADMIN);

        CreateOperatorRequestRequest createPayload = new(
            new CreateOperatorCompanyRequest("Tours Incompletos SV"),
            "Solicitud con documento borroso",
            [new CreateOperatorRequestDocumentRequest("DUI_FRENTE", "private/dui-borroso.jpg")]);

        using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/operator-requests")
        {
            Content = JsonContent.Create(createPayload)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);
        HttpResponseMessage createResponse = await _client.SendAsync(createMsg);
        OperatorRequestDto? created = await createResponse.Content.ReadFromJsonAsync<OperatorRequestDto>();

        ReviewOperatorRequestRequest reviewPayload = new(false, "El documento DUI adjunto no es legible.");
        using HttpRequestMessage reviewMsg = new(HttpMethod.Post, $"/api/operator-requests/{created!.Id}/review")
        {
            Content = JsonContent.Create(reviewPayload)
        };
        reviewMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        HttpResponseMessage reviewResponse = await _client.SendAsync(reviewMsg);

        // Assert
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        OperatorRequestDto? reviewed = await reviewResponse.Content.ReadFromJsonAsync<OperatorRequestDto>();
        reviewed.Should().NotBeNull();
        reviewed!.Status.Should().Be("RECHAZADA");
        reviewed.RejectionReason.Should().Be("El documento DUI adjunto no es legible.");
        reviewed.Company.Status.Should().Be("RECHAZADA");
        reviewed.Company.RejectionReason.Should().Be("El documento DUI adjunto no es legible.");
        reviewed.User.Role.Should().Be("VISITANTE");

        _factory.Store.Users[visitor.Id].Role.Should().Be(UserRole.VISITANTE);
    }

    private (User User, string Token) CreateAuthenticatedUser(string email, UserRole role)
    {
        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Usuario",
            LastName = "Prueba",
            Role = role,
            Status = UserStatus.ACTIVO,
            EmailVerified = true
        };

        _factory.Store.Users[user.Id] = user;

        using IServiceScope scope = _factory.Services.CreateScope();
        IJwtProvider jwtProvider = scope.ServiceProvider.GetRequiredService<IJwtProvider>();
        string token = jwtProvider.GenerateToken(user);

        return (user, token);
    }
}
