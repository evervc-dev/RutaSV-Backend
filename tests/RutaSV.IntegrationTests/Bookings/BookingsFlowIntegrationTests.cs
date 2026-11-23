using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using RutaSV.Application.Bookings.DTO;
using RutaSV.Application.Catalog.DTO;
using RutaSV.Application.Common.DTO;
using RutaSV.Application.Identity.DTO;
using RutaSV.Domain.Identity.Enum;
using RutaSV.Domain.Identity.Model;
using RutaSV.IntegrationTests.Identity;

namespace RutaSV.IntegrationTests.Bookings;

public sealed class BookingsFlowIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task FullBookingFavoritesRefundAndIncidentFlow_ShouldEnforceAllBusinessRules()
    {
        using AuthWebApplicationFactory factory = new();
        HttpClient client = factory.CreateClient();

        // 1. Crear operador aprobado + empresa aprobada + experiencia publicada
        (string operatorToken, Guid operatorId, Company company) = await SeedOperatorAndCompanyAsync(factory, client);
        ExperienceDto exp = await CreatePublishedExperienceAsync(client, operatorToken, autoConfirmation: false, price: 45m);

        // 2. Operador actualiza perfil de empresa (CompaniesController)
        SetBearer(client, operatorToken);
        HttpResponseMessage updateCompanyResp = await client.PutAsJsonAsync($"/api/companies/{company.Id}", new UpdateCompanyRequest(
            TradeName: "Surf El Tunco Pro",
            Description: "Escuela certificada de surf",
            ContactEmail: "hola@surfeltunco.sv",
            ContactPhone: "7777-8888",
            MunicipalityId: 16));
        Assert.Equal(HttpStatusCode.OK, updateCompanyResp.StatusCode);

        HttpResponseMessage publicCompanyResp = await client.GetAsync($"/api/companies/{company.Id}");
        Assert.Equal(HttpStatusCode.OK, publicCompanyResp.StatusCode);
        CompanyProfileDto? publicCompany = await publicCompanyResp.Content.ReadFromJsonAsync<CompanyProfileDto>(JsonOptions);
        Assert.NotNull(publicCompany);
        Assert.Equal("Surf El Tunco Pro", publicCompany.TradeName);
        Assert.Single(publicCompany.Experiences);

        // 3. Crear dos sesiones de disponibilidad (>72h en el futuro para probar reembolso 100 % y reagendamiento RN-21)
        string slot1Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string slot2Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        HttpResponseMessage slot1Resp = await client.PostAsJsonAsync($"/api/experiences/{exp.Id}/availabilities", new CreateAvailabilityRequest(
            Date: slot1Date, StartTime: "09:00", EndTime: "12:00", TotalCapacity: 4));
        Assert.Equal(HttpStatusCode.Created, slot1Resp.StatusCode);
        AvailabilityDto slot1 = (await slot1Resp.Content.ReadFromJsonAsync<AvailabilityDto>(JsonOptions))!;

        HttpResponseMessage slot2Resp = await client.PostAsJsonAsync($"/api/experiences/{exp.Id}/availabilities", new CreateAvailabilityRequest(
            Date: slot2Date, StartTime: "10:00", EndTime: "13:00", TotalCapacity: 6));
        Assert.Equal(HttpStatusCode.Created, slot2Resp.StatusCode);
        AvailabilityDto slot2 = (await slot2Resp.Content.ReadFromJsonAsync<AvailabilityDto>(JsonOptions))!;

        // 4. Registrar turista SIN verificar correo -> RN-20 debe devolver 403 EMAIL_NO_VERIFICADO
        HttpResponseMessage regTouristResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "turista.flow@rutasv.com", "Turista123!", "Carlos", "Méndez", "7000-1111"));
        Assert.Equal(HttpStatusCode.OK, regTouristResp.StatusCode);
        AuthResponse touristAuth = (await regTouristResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;

        SetBearer(client, touristAuth.Token);
        HttpResponseMessage unverifiedBookResp = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(
            exp.Id, slot1.Id, 2, "Carlos Méndez", "turista.flow@rutasv.com", "7000-1111"));
        Assert.Equal(HttpStatusCode.Forbidden, unverifiedBookResp.StatusCode);
        string unverifiedBody = await unverifiedBookResp.Content.ReadAsStringAsync();
        Assert.Contains("EMAIL_NO_VERIFICADO", unverifiedBody);

        // 5. Favoritos: agregar, listar y quitar
        HttpResponseMessage addFavResp = await client.PostAsync($"/api/favorites/{exp.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, addFavResp.StatusCode);

        List<Guid> favIds = (await client.GetFromJsonAsync<List<Guid>>("/api/favorites/ids", JsonOptions))!;
        Assert.Contains(exp.Id, favIds);

        List<FavoriteItemDto> favs = (await client.GetFromJsonAsync<List<FavoriteItemDto>>("/api/favorites", JsonOptions))!;
        Assert.Single(favs);

        // 6. Verificar correo del turista y reservar 2 cupos -> PENDIENTE + PAGADO_DEMO + correos enviados
        string verifyToken = factory.Store.SentVerificationTokensByEmail["turista.flow@rutasv.com"];
        HttpResponseMessage verifyResp = await client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(verifyToken));
        Assert.Equal(HttpStatusCode.OK, verifyResp.StatusCode);

        HttpResponseMessage bookResp = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(
            exp.Id, slot1.Id, 2, "Carlos Méndez", "turista.flow@rutasv.com", "7000-1111", "Sin alergias"));
        Assert.Equal(HttpStatusCode.Created, bookResp.StatusCode);
        ReservationDto reservation1 = (await bookResp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;
        Assert.Equal("PENDIENTE", reservation1.Status);
        Assert.Equal("PAGADO_DEMO", reservation1.PaymentStatus);
        Assert.Equal(90m, reservation1.Total);
        Assert.Single(reservation1.History);
        Assert.True(factory.Store.SentNotifications.Count >= 2); // turista + operador

        // 7. Intentar sobreventa (quedan 2 cupos, pide 3 -> 409 Conflict)
        HttpResponseMessage oversellResp = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(
            exp.Id, slot1.Id, 3, "Carlos Méndez", "turista.flow@rutasv.com", "7000-1111"));
        Assert.Equal(HttpStatusCode.Conflict, oversellResp.StatusCode);

        // 8. Operador confirma la reserva -> CONFIRMADA
        SetBearer(client, operatorToken);
        HttpResponseMessage confirmResp = await client.PostAsync($"/api/reservations/{reservation1.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResp.StatusCode);
        ReservationDto confirmed = (await confirmResp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;
        Assert.Equal("CONFIRMADA", confirmed.Status);

        // 9. Turista consulta cancel-preview (>=48h -> 100%) y cancela -> crea Refund ticket y libera cupos
        SetBearer(client, touristAuth.Token);
        CancelPreviewDto preview = (await client.GetFromJsonAsync<CancelPreviewDto>(
            $"/api/reservations/{reservation1.Id}/cancel-preview", JsonOptions))!;
        Assert.True(preview.CanCancel);
        Assert.Equal((short)100, preview.RefundPercentage);
        Assert.Equal(90m, preview.RefundAmount);

        HttpResponseMessage cancelResp = await client.PostAsJsonAsync(
            $"/api/reservations/{reservation1.Id}/cancel",
            new CancelReservationRequest("Cambio de planes"));
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        ReservationDto cancelled = (await cancelResp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;
        Assert.Equal("CANCELADA", cancelled.Status);
        Assert.Equal("REEMBOLSADO_DEMO", cancelled.PaymentStatus);
        Assert.NotNull(cancelled.Refund);
        Assert.Equal((short)100, cancelled.Refund.Percentage);
        Assert.Equal("EN_PROCESO", cancelled.Refund.Status);

        // Operador marca ticket de reembolso como REALIZADO
        SetBearer(client, operatorToken);
        HttpResponseMessage completeRefundResp = await client.PostAsJsonAsync(
            $"/api/refunds/{cancelled.Refund.Id}/complete",
            new CompleteRefundRequest("Reembolso demo aplicado"));
        Assert.Equal(HttpStatusCode.OK, completeRefundResp.StatusCode);
        RefundDto completedRefund = (await completeRefundResp.Content.ReadFromJsonAsync<RefundDto>(JsonOptions))!;
        Assert.Equal("REALIZADO", completedRefund.Status);

        // 10. Probar RN-21: nueva reserva en slot1 -> operador cancela slot1 -> PENDIENTE_DECISION -> turista reagenda a slot2
        SetBearer(client, touristAuth.Token);
        HttpResponseMessage book2Resp = await client.PostAsJsonAsync("/api/reservations", new CreateReservationRequest(
            exp.Id, slot1.Id, 2, "Carlos Méndez", "turista.flow@rutasv.com", "7000-1111"));
        Assert.Equal(HttpStatusCode.Created, book2Resp.StatusCode);
        ReservationDto res2 = (await book2Resp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;

        SetBearer(client, operatorToken);
        HttpResponseMessage cancelSlotResp = await client.PostAsJsonAsync(
            $"/api/availabilities/{slot1.Id}/cancel",
            new CancelAvailabilityRequest("Clima adverso"));
        Assert.Equal(HttpStatusCode.OK, cancelSlotResp.StatusCode);

        SetBearer(client, touristAuth.Token);
        ReservationDto affectedRes = (await client.GetFromJsonAsync<ReservationDto>($"/api/reservations/{res2.Id}", JsonOptions))!;
        Assert.Equal("PENDIENTE_DECISION", affectedRes.Status);
        Assert.NotNull(affectedRes.ActiveIncident);
        Assert.Equal("PENDIENTE", affectedRes.ActiveIncident.Status);

        List<AvailabilityDto> options = (await client.GetFromJsonAsync<List<AvailabilityDto>>(
            $"/api/reservations/{res2.Id}/incident/options", JsonOptions))!;
        Assert.Contains(options, o => o.Id == slot2.Id);

        HttpResponseMessage resolveResp = await client.PostAsJsonAsync(
            $"/api/reservations/{res2.Id}/incident/resolve",
            new ResolveIncidentRequest("REAGENDAR", slot2.Id));
        Assert.Equal(HttpStatusCode.OK, resolveResp.StatusCode);
        ReservationDto rescheduled = (await resolveResp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;
        Assert.Equal("CONFIRMADA", rescheduled.Status);
        Assert.Equal(slot2.Id, rescheduled.AvailabilityId);

        // 11. Historial del turista muestra ambas reservas
        PagedResponse<ReservationDto> myRes = (await client.GetFromJsonAsync<PagedResponse<ReservationDto>>(
            "/api/reservations/my", JsonOptions))!;
        Assert.Equal(2, myRes.TotalRecords);

        // 12. Reserva presencial por operador (RF-O12 / RN-19)
        SetBearer(client, operatorToken);
        HttpResponseMessage inPersonResp = await client.PostAsJsonAsync("/api/reservations/in-person",
            new CreateInPersonReservationRequest(exp.Id, slot2.Id, 2, "Ana Walkin", "7123-4567"));
        Assert.Equal(HttpStatusCode.Created, inPersonResp.StatusCode);
        ReservationDto inPerson = (await inPersonResp.Content.ReadFromJsonAsync<ReservationDto>(JsonOptions))!;
        Assert.Equal("PRESENCIAL", inPerson.Source);
        Assert.Equal("CONFIRMADA", inPerson.Status);
        Assert.Null(inPerson.VisitorId);
    }

    private static async Task<(string Token, Guid UserId, Company Company)> SeedOperatorAndCompanyAsync(
        AuthWebApplicationFactory factory,
        HttpClient client)
    {
        HttpResponseMessage regResp = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "operador.bookings@rutasv.com", "Operador123!", "María", "López", "7888-9999"));
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);
        AuthResponse auth = (await regResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;

        User op = factory.Store.Users[auth.Id];
        op.Role = UserRole.OPERADOR;
        op.EmailVerified = true;
        op.Status = UserStatus.ACTIVO;

        Company company = new()
        {
            Id = Guid.NewGuid(),
            OperatorId = op.Id,
            TradeName = "Surf El Tunco",
            Status = CompanyStatus.APROBADA,
            MunicipalityId = 16,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        factory.Store.Companies[company.Id] = company;

        HttpResponseMessage loginResp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            "operador.bookings@rutasv.com", "Operador123!"));
        Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
        AuthResponse opAuth = (await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions))!;

        return (opAuth.Token, opAuth.Id, company);
    }

    private static async Task<ExperienceDto> CreatePublishedExperienceAsync(
        HttpClient client,
        string operatorToken,
        bool autoConfirmation,
        decimal price)
    {
        SetBearer(client, operatorToken);
        HttpResponseMessage resp = await client.PostAsJsonAsync("/api/experiences", new CreateExperienceRequest(
            Title: "Clases de Surf al Atardecer",
            ShortDescription: "Aprende surf con instructores locales en Playa El Tunco.",
            Description: "Disfruta de una clase personalizada de surf en Playa El Tunco con tabla y guía incluidos.",
            Price: price,
            MunicipalityId: 16,
            Latitude: 13.4926m,
            Longitude: -89.3831m,
            HasLimitedCapacity: true,
            MinPeople: 1,
            MaxPeople: 8,
            AutoConfirmation: autoConfirmation,
            CategoryIds: [1, 3],
            PrimaryCategoryId: 3,
            Images: [new CreateExperienceImageRequest(null, "https://example.com/surf.jpg", null, "Surf", 0, true)],
            PublishImmediately: true));

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<ExperienceDto>(JsonOptions))!;
    }

    private static void SetBearer(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
