using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.Security;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;
using RutaSV.IntegrationTests.Identity;

namespace RutaSV.IntegrationTests.Catalog;

public sealed class ExperiencesEndpointsIntegrationTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExperiencesEndpointsIntegrationTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCatalogLookups_ShouldReturnCategoriesDepartmentsAndMunicipalities()
    {
        // Act
        HttpResponseMessage categoriesResponse = await _client.GetAsync("/api/categories");
        HttpResponseMessage departmentsResponse = await _client.GetAsync("/api/departments");
        HttpResponseMessage municipalitiesResponse = await _client.GetAsync("/api/municipalities?departmentId=5");

        // Assert
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CategoryDto>? categories = await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().NotBeNullOrEmpty();

        departmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<DepartmentDto>? departments = await departmentsResponse.Content.ReadFromJsonAsync<List<DepartmentDto>>();
        departments.Should().NotBeNullOrEmpty();

        municipalitiesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        List<MunicipalityDto>? municipalities = await municipalitiesResponse.Content.ReadFromJsonAsync<List<MunicipalityDto>>();
        municipalities.Should().NotBeNullOrEmpty();
        municipalities!.Should().OnlyContain(m => m.DepartmentId == 5);
    }

    [Fact]
    public async Task CreateExperience_WhenVisitorRole_ShouldReturn403Forbidden()
    {
        // Arrange
        (_, string visitorToken) = CreateAuthenticatedUser("visitante.exp@rutasv.com", UserRole.VISITANTE);

        CreateExperienceRequest payload = new(
            "Clases de Surf",
            "Resumen",
            "Descripción completa de clases de surf en La Libertad.",
            30m,
            16,
            13.492900m,
            -89.383100m);

        using HttpRequestMessage msg = new(HttpMethod.Post, "/api/experiences")
        {
            Content = JsonContent.Create(payload)
        };
        msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", visitorToken);

        // Act
        HttpResponseMessage response = await _client.SendAsync(msg);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePublishUpdateAndDeleteExperience_FullLifecycle_ShouldWorkCorrectly()
    {
        // Arrange: crear operador con empresa APROBADA
        (User operatorUser, string operatorToken, Company company) = CreateApprovedOperatorWithCompany("operador.surf@rutasv.com");

        CreateExperienceRequest createPayload = new(
            "Surf Camp El Tunco",
            "Clases intensivas de surf con equipo incluido.",
            "Vive la experiencia completa de surfear en las mejores olas de El Salvador con instructores certificados.",
            45.00m,
            16,
            13.492900m,
            -89.383100m,
            HasLimitedCapacity: true,
            MinPeople: 1,
            MaxPeople: 6,
            CompanyId: company.Id,
            Includes: "Tabla de surf, rashguard e instructor",
            Excludes: "Transporte",
            WhatToBring: "Protector solar y toalla",
            DurationMinutes: 120,
            Difficulty: "MODERADA",
            MinAge: 10,
            ServiceLanguage: "Español / Inglés",
            ReferenceAddress: "Frente a Roca Sunzal, Playa El Tunco",
            AutoConfirmation: true,
            CategoryIds: [1, 3],
            PrimaryCategoryId: 3,
            Images:
            [
                new CreateExperienceImageRequest(
                    "experiences/surf-tunco-cover.jpg",
                    "https://minio.rutasv.local/public/surf-tunco-cover.jpg",
                    null,
                    "Olas en El Tunco",
                    0,
                    true)
            ],
            PublishImmediately: false);

        using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/experiences")
        {
            Content = JsonContent.Create(createPayload)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        // Act 1: Crear en estado BORRADOR
        HttpResponseMessage createResponse = await _client.SendAsync(createMsg);

        // Assert 1
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        ExperienceDto? created = await createResponse.Content.ReadFromJsonAsync<ExperienceDto>();
        created.Should().NotBeNull();
        created!.Status.Should().Be("BORRADOR");
        created.Title.Should().Be("Surf Camp El Tunco");
        created.Slug.Should().Be("surf-camp-el-tunco");
        created.Latitude.Should().Be(13.492900m);
        created.Longitude.Should().Be(-89.383100m);
        created.HasLimitedCapacity.Should().BeTrue();
        created.MaxPeople.Should().Be(6);
        created.Categories.Should().HaveCount(2);
        created.Images.Should().ContainSingle(i => i.IsCover);

        // Mientras está en BORRADOR, un visitante no autenticado no puede verla por slug (404)
        HttpResponseMessage publicDraftGet = await _client.GetAsync($"/api/experiences/{created.Slug}");
        publicDraftGet.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Pero el operador dueño sí puede previsualizar su propio BORRADOR
        using HttpRequestMessage ownerDraftGet = new(HttpMethod.Get, $"/api/experiences/{created.Slug}");
        ownerDraftGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        HttpResponseMessage ownerPreviewResponse = await _client.SendAsync(ownerDraftGet);
        ownerPreviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2: Publicar la experiencia
        using HttpRequestMessage publishMsg = new(HttpMethod.Post, $"/api/experiences/{created.Id}/publish");
        publishMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        HttpResponseMessage publishResponse = await _client.SendAsync(publishMsg);

        // Assert 2
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ExperienceDto? published = await publishResponse.Content.ReadFromJsonAsync<ExperienceDto>();
        published!.Status.Should().Be("PUBLICADA");
        published.PublishedAt.Should().NotBeNull();

        // Ahora sí aparece en el catálogo público y en la consulta pública por slug
        HttpResponseMessage publicCatalogResponse = await _client.GetAsync("/api/experiences?search=Surf&categoryId=3");
        publicCatalogResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<ExperienceSummaryDto>? catalogPage = await publicCatalogResponse.Content.ReadFromJsonAsync<PagedResponse<ExperienceSummaryDto>>();
        catalogPage!.Data.Should().Contain(e => e.Id == created.Id);

        HttpResponseMessage publicDetailResponse = await _client.GetAsync($"/api/experiences/{created.Slug}");
        publicDetailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 3: Actualizar la experiencia (cambiando coordenadas Leaflet y pasando a cupo ilimitado)
        UpdateExperienceRequest updatePayload = new(
            "Surf Camp & Sunset El Tunco",
            "Clases de surf y atardecer en la playa.",
            "Vive la experiencia completa de surfear y disfrutar del atardecer en Playa El Tunco.",
            50.00m,
            16,
            13.493500m,
            -89.384200m,
            HasLimitedCapacity: false,
            MinPeople: 1,
            MaxPeople: null,
            CategoryIds: [1, 3],
            PrimaryCategoryId: 3,
            Images:
            [
                new CreateExperienceImageRequest(
                    "experiences/surf-tunco-cover.jpg",
                    "https://minio.rutasv.local/public/surf-tunco-cover.jpg",
                    null,
                    "Olas en El Tunco",
                    0,
                    true)
            ]);

        using HttpRequestMessage updateMsg = new(HttpMethod.Put, $"/api/experiences/{created.Id}")
        {
            Content = JsonContent.Create(updatePayload)
        };
        updateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        HttpResponseMessage updateResponse = await _client.SendAsync(updateMsg);

        // Assert 3
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ExperienceDto? updated = await updateResponse.Content.ReadFromJsonAsync<ExperienceDto>();
        updated!.Title.Should().Be("Surf Camp & Sunset El Tunco");
        updated.Price.Should().Be(50.00m);
        updated.Latitude.Should().Be(13.493500m);
        updated.Longitude.Should().Be(-89.384200m);
        updated.HasLimitedCapacity.Should().BeFalse();
        updated.MaxPeople.Should().BeNull();

        // Act 4: Eliminar (archivar lógicamente) la experiencia
        using HttpRequestMessage deleteMsg = new(HttpMethod.Delete, $"/api/experiences/{created.Id}");
        deleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        HttpResponseMessage deleteResponse = await _client.SendAsync(deleteMsg);

        // Assert 4
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage afterDeleteGet = await _client.GetAsync($"/api/experiences/{created.Id}");
        afterDeleteGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublishExperience_WhenIncomplete_ShouldReturn422UnprocessableEntityWithMissingFields()
    {
        // Arrange
        (_, string operatorToken, Company company) = CreateApprovedOperatorWithCompany("operador.incompleto@rutasv.com");

        CreateExperienceRequest draftPayload = new(
            "Caminata Borrador",
            "Resumen",
            "Texto corto",
            15.00m,
            14,
            0m,
            0m,
            CompanyId: company.Id,
            CategoryIds: [],
            Images: [],
            PublishImmediately: false);

        using HttpRequestMessage createMsg = new(HttpMethod.Post, "/api/experiences")
        {
            Content = JsonContent.Create(draftPayload)
        };
        createMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);
        HttpResponseMessage createResponse = await _client.SendAsync(createMsg);
        ExperienceDto? draft = await createResponse.Content.ReadFromJsonAsync<ExperienceDto>();

        using HttpRequestMessage publishMsg = new(HttpMethod.Post, $"/api/experiences/{draft!.Id}/publish");
        publishMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", operatorToken);

        // Act
        HttpResponseMessage publishResponse = await _client.SendAsync(publishMsg);

        // Assert
        publishResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        ProblemDetails? problem = await publishResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.UnprocessableEntity);
        problem.Extensions.Should().ContainKey("missingFields");
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

    private (User User, string Token, Company Company) CreateApprovedOperatorWithCompany(string email)
    {
        (User user, string token) = CreateAuthenticatedUser(email, UserRole.OPERADOR);

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = user.Id,
            TradeName = "Aventuras SV",
            Status = CompanyStatus.APROBADA,
            MunicipalityId = 14,
            Operator = user
        };

        _factory.Store.Companies[company.Id] = company;

        return (user, token, company);
    }
}
