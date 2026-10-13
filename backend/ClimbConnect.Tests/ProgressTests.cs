using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimbConnect.API.Data;
using ClimbConnect.API.Models;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task CreateProgress_FlashMitMehrerenVersuchen_Gibt400()
    {
        var routeId = await SeedRouteAsync();

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId, status = "Flash", climbingStyle = "Vorstieg", attempts = 3, date = "2026-06-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateProgress_OnsightMitMehrerenVersuchen_Gibt400()
    {
        var routeId = await SeedRouteAsync();

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Zuerst eine gültige Rotpunkt-Begehung mit 3 Versuchen anlegen
        var createResponse = await _client.PostAsJsonAsync("/api/progress", new
        {
            routeId, status = "Rotpunkt", climbingStyle = "Vorstieg", attempts = 3, date = "2026-06-01"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ProgressResponse>();

        // Auf Onsight ändern, aber 3 Versuche behalten → muss abgelehnt werden
        var updateResponse = await _client.PutAsJsonAsync($"/api/progress/{created!.Id}", new
        {
            status = "Onsight", climbingStyle = "Vorstieg", attempts = 3, date = "2026-06-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetStats_ZweiRoutenAmSelbenTag_ZählenAlsEinBesuch()
    {
        // Zwei Routen im selben Gebiet anlegen
        var routeId = await SeedRouteAsync();
        int secondRouteId, areaId, userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db    = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var first = await db.Routes.Include(r => r.Sector).FirstAsync(r => r.Id == routeId);
            var second = new ClimbConnect.API.Models.Route { SectorId = first.SectorId, Name = "Testroute 2", Grade = "6a" };
            db.Routes.Add(second);
            await db.SaveChangesAsync();

            secondRouteId = second.Id;
            areaId        = first.Sector.AreaId;
            userId        = (await db.Users.FirstAsync(u => u.Email == "user@climbconnect.at")).Id;
        }

        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Tag 1: zwei Routen, Tag 2: ein Projekt → 2 Besuche
        await _client.PostAsJsonAsync("/api/progress", new { routeId, status = "Flash", climbingStyle = "Vorstieg", attempts = 1, date = "2026-07-01" });
        await _client.PostAsJsonAsync("/api/progress", new { routeId = secondRouteId, status = "Rotpunkt", climbingStyle = "Vorstieg", attempts = 2, date = "2026-07-01" });
        await _client.PostAsJsonAsync("/api/progress", new { routeId = secondRouteId, status = "Projekt", climbingStyle = "Toprope", attempts = 3, date = "2026-07-08" });

        var stats = await _client.GetFromJsonAsync<StatsResponse>($"/api/users/{userId}/stats");
        var visit = stats!.AreaVisits.Single(v => v.AreaId == areaId);
        Assert.Equal(2, visit.Visits);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetStats_GleichstandBeiBesuchen_ZeigtBeideLieblingsgebiete()
    {
        // Eigener User, damit keine Einträge aus anderen Tests mitzählen
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email  = $"lieblingsgebiet_{suffix}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { username = $"fav_{suffix}", email, password = "Test1234!" });

        var routeA = await SeedRouteAsync();
        var routeB = await SeedRouteAsync();
        int userId;
        string areaA, areaB;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            userId = (await db.Users.FirstAsync(u => u.Email == email)).Id;
            areaA  = (await db.Routes.Include(r => r.Sector).ThenInclude(s => s.Area).FirstAsync(r => r.Id == routeA)).Sector.Area.Name;
            areaB  = (await db.Routes.Include(r => r.Sector).ThenInclude(s => s.Area).FirstAsync(r => r.Id == routeB)).Sector.Area.Name;
        }

        var token = await LoginAsync(email, "Test1234!");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Je ein Besuch in Gebiet A und B → Gleichstand
        await _client.PostAsJsonAsync("/api/progress", new { routeId = routeA, status = "Flash", climbingStyle = "Vorstieg", attempts = 1, date = "2026-07-01" });
        await _client.PostAsJsonAsync("/api/progress", new { routeId = routeB, status = "Flash", climbingStyle = "Vorstieg", attempts = 1, date = "2026-07-02" });

        var stats = await _client.GetFromJsonAsync<StatsResponse>($"/api/users/{userId}/stats");
        Assert.Contains(areaA, stats!.FavoriteArea);
        Assert.Contains(areaB, stats.FavoriteArea);

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
    private record AreaVisitResponse(int AreaId, string AreaName, int Visits);
    private record StatsResponse(string? FavoriteArea, List<AreaVisitResponse> AreaVisits);
}
