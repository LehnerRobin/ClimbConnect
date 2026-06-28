using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

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

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body?.Token ?? "";
    }

    private record AuthResponse(string Token);
    private record AreaResponse(int Id, string Name, string? Location);
    private record PagedResponse<T>(int Total, int Page, int PageSize, List<T> Items);
}
