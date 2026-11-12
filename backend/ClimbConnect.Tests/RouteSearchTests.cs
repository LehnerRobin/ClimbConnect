using System.Net;
using System.Net.Http.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für die globale Routensuche (GET /api/routes?search=…), die das
/// Frontend auf der Seite „Routen" verwendet (#227).
/// </summary>
public class RouteSearchTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public RouteSearchTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>Ein Treffer enthält Name, Grad, Sektor und Gebiet.</summary>
    [Fact]
    public async Task SearchRoutes_NachName_LiefertGebietUndSektor()
    {
        // Seed-Route "Fingerkante" (6c) liegt im Sektor "Hauptwand" des Gebiets "Sandwand"
        var response = await _client.GetAsync("/api/routes?search=Fingerkante");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<RouteResponse>>();
        Assert.NotNull(paged);

        var route = Assert.Single(paged.Items, r => r.Name == "Fingerkante");
        Assert.Equal("6c", route.Grade);
        Assert.Equal("Hauptwand", route.SectorName);
        Assert.Equal("Sandwand", route.AreaName);
        Assert.True(route.AreaId > 0);
    }

    /// <summary>Der Grad kommt in der angefragten Skala zurück.</summary>
    [Fact]
    public async Task SearchRoutes_MitSkala_LiefertUmgerechnetenGrad()
    {
        var response = await _client.GetAsync("/api/routes?search=Fingerkante&scale=uiaa");
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<RouteResponse>>();

        var route = Assert.Single(paged!.Items, r => r.Name == "Fingerkante");
        Assert.Equal("VII+", route.Grade);
    }

    /// <summary>Ohne Treffer kommt eine leere Liste, kein Fehler.</summary>
    [Fact]
    public async Task SearchRoutes_OhneTreffer_LiefertLeereListe()
    {
        var response = await _client.GetAsync("/api/routes?search=GibtEsSicherNicht12345");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<RouteResponse>>();
        Assert.NotNull(paged);
        Assert.Equal(0, paged.Total);
        Assert.Empty(paged.Items);
    }

    private record RouteResponse(int Id, int SectorId, string? SectorName, int AreaId, string? AreaName, string Name, string? Grade);
    private record PagedResponse<T>(int Total, int Page, int PageSize, List<T> Items);
}
