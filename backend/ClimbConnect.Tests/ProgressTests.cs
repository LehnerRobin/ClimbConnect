using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimbConnect.API.Data;
using ClimbConnect.API.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ClimbConnect.Tests;

/// <summary>Integrationstests für die Progress-Endpoints (Begehungen).</summary>
public class ProgressTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;
    private readonly ClimbConnectFactory _factory;

    public ProgressTests(ClimbConnectFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyProgress_OhneAuth_Gibt401()
    {
        var response = await _client.GetAsync("/api/progress/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyProgress_MitAuth_Gibt200UndLeeredListe()
    {
        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/progress/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateProgress_MitGültigemEintrag_Gibt201()
    {
        var routeId = await SeedRouteAsync();

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId,
            status        = "Rotpunkt",
            climbingStyle = "Vorstieg",
            attempts      = 3,
            date          = "2026-06-01",
            notes         = (string?)null,
            subjectiveGrade        = "6b",
            subjectiveGradeComment = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateProgress_UngültigerStatus_Gibt400()
    {
        var routeId = await SeedRouteAsync();

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId,
            status        = "UNGÜLTIG",
            climbingStyle = "Vorstieg",
            attempts      = 1,
            date          = "2026-06-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task CreateProgress_OhneRoute_Gibt400()
    {
        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId       = 99999,
            status        = "Flash",
            climbingStyle = "Vorstieg",
            attempts      = 1,
            date          = "2026-06-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeleteProgress_EigenerEintrag_Gibt204()
    {
        var routeId = await SeedRouteAsync();

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId, status = "Flash", climbingStyle = "Vorstieg", attempts = 1, date = "2026-06-01"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ProgressResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/progress/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    // Legt eine Test-Route direkt in der In-Memory-DB an und gibt die ID zurück
    private async Task<int> SeedRouteAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var area = new Area { Name = $"Testgebiet_{Guid.NewGuid()}" };
        db.Areas.Add(area);
        await db.SaveChangesAsync();

        var sector = new Sector { AreaId = area.Id, Name = "Testsektor" };
        db.Sectors.Add(sector);
        await db.SaveChangesAsync();

        var route = new ClimbConnect.API.Models.Route
        {
            SectorId = sector.Id,
            Name     = "Testroute",
            Grade    = "6b"
        };
        db.Routes.Add(route);
        await db.SaveChangesAsync();

        return route.Id;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body?.Token ?? "";
    }

    private record AuthResponse(string Token);
    private record ProgressResponse(int Id);
}
