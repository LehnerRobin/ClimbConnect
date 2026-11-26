using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimbConnect.API.Services;

namespace ClimbConnect.Tests;

/// <summary>Integrationstests für die Areas-Endpoints.</summary>
public class AreaTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public AreaTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAreas_OhneAuth_Gibt200()
    {
        var response = await _client.GetAsync("/api/areas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAreas_EnthältSeedGebiete()
    {
        // Seed-Daten enthalten "Sandwand" — API gibt jetzt Paginierungs-Wrapper zurück
        var response = await _client.GetAsync("/api/areas?search=Sandwand");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<AreaResponse>>();
        Assert.NotNull(paged);
        Assert.Contains(paged.Items, a => a.Name.Contains("Sandwand"));
    }

    [Fact]
    public async Task CreateArea_OhneAuth_Gibt401()
    {
        var response = await _client.PostAsJsonAsync("/api/areas", new
        {
            name     = "Neues Gebiet",
            location = (string?)null,
            description = (string?)null,
            imageUrl    = (string?)null
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateArea_AlsAdmin_Gibt201()
    {
        // Seed-Admin: admin@climbconnect.at / Admin1234!
        var token = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/areas", new
        {
            name        = "Admin-Testgebiet",
            location    = "Oberösterreich",
            description = (string?)null,
            imageUrl    = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateArea_AlsNormalerUser_Gibt403()
    {
        // Seed-User: user@climbconnect.at / User1234!
        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/areas", new
        {
            name        = "Darf nicht klappen",
            location    = (string?)null,
            description = (string?)null,
            imageUrl    = (string?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetAreaById_NichtVorhanden_Gibt404()
    {
        var response = await _client.GetAsync("/api/areas/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteArea_AlsAdmin_Gibt204()
    {
        var token = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Gebiet anlegen
        var createResponse = await _client.PostAsJsonAsync("/api/areas", new
        {
            name = "Löschbares Gebiet", location = (string?)null, description = (string?)null, imageUrl = (string?)null
        });
        var created = await createResponse.Content.ReadFromJsonAsync<AreaResponse>();

        // Löschen
        var deleteResponse = await _client.DeleteAsync($"/api/areas/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    // Leute in den nächsten 7 Tagen (#239)

    [Fact]
    public async Task GetAreas_ZaehltLeuteUndTermineDerNaechsten7Tage_AbMorgen()
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var other = await RegisterAsync("woche-test", "woche-test@example.com", "Test1234!");

        // Eigenes Gebiet, damit Termine anderer Tests nicht mitzählen
        var create = await SendAsync(HttpMethod.Post, "/api/areas", admin, JsonContent.Create(new
        {
            name = "Wochen-Testgebiet", location = (string?)null, description = (string?)null, imageUrl = (string?)null
        }));
        var areaId = (await create.Content.ReadFromJsonAsync<AreaResponse>())!.Id;

        // Mittag des Wiener Tages "in N Tagen" – so liegt der Termin sicher mitten im Tag
        static string NoonInDays(int days) =>
            AppTime.ViennaDaysUtc(days, 1).StartUtc.AddHours(12).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var tomorrow = await CreateAppointmentAsync(areaId, user, "Morgen", NoonInDays(1));
        await CreateAppointmentAsync(areaId, user, "Letzter Tag", NoonInDays(7));
        await CreateAppointmentAsync(areaId, user, "Zu spät",     NoonInDays(8));

        var join = await SendAsync(HttpMethod.Post, $"/api/appointments/{tomorrow}/subscribe", other,
            JsonContent.Create(new { comment = (string?)null }));
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var areas = await _client.GetFromJsonAsync<PagedResponse<AreaVisitors>>("/api/areas?search=Wochen-Testgebiet");
        var area  = Assert.Single(areas!.Items);

        // Morgen: Ersteller + Beigetretener = 2, letzter Tag: 1 → 3 Leute in 2 Terminen; Tag 8 zählt nicht
        Assert.Equal(3, area.NextWeekVisitors);
        Assert.Equal(2, area.NextWeekAppointments);
        Assert.Equal(0, area.TodayVisitors);
        Assert.Equal(0, area.TodayAppointments);
    }

    private async Task<int> CreateAppointmentAsync(int areaId, string token, string title, string date)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title, date }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AreaResponse>())!.Id;
    }

    /// <summary>Anfrage mit Bearer-Token, ohne den gemeinsamen Client zu verändern.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> RegisterAsync(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body?.Token ?? "";
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body?.Token ?? "";
    }

    private record AuthResponse(string Token);
    private record AreaResponse(int Id, string Name, string? Location);
    private record PagedResponse<T>(int Total, int Page, int PageSize, List<T> Items);
    private record AreaVisitors(int Id, int TodayVisitors, int TodayAppointments, int NextWeekVisitors, int NextWeekAppointments);
}
