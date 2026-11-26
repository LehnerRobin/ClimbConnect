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

    // Filter nach Grad, Stil und Länge (#240) – Seed-Routen, siehe SeedData.cs

    /// <summary>Lädt alle Treffer (eine große Seite) für eine Filter-URL.</summary>
    private async Task<List<RouteResponse>> GetAllAsync(string query)
    {
        var response = await _client.GetAsync($"/api/routes?pageSize=100&{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<RouteResponse>>();
        Assert.Equal(paged!.Total, paged.Items.Count); // alles auf einer Seite
        return paged.Items;
    }

    [Fact]
    public async Task FilterGrad_VonBis_LiefertNurRoutenImBereich_PlusZaehltDazwischen()
    {
        var routes = await GetAllAsync("minGrade=6a&maxGrade=6b");
        var names  = routes.Select(r => r.Name).ToList();

        Assert.NotEmpty(routes);
        Assert.All(routes, r => Assert.Contains(r.Grade, new[] { "6a", "6a+", "6b" }));
        Assert.Contains("Sandwandverschneidung", names); // 6a
        Assert.Contains("Klamm-Klassiker", names);       // 6a+ liegt zwischen 6a und 6b
        Assert.Contains("Riss links", names);            // 6b
        Assert.DoesNotContain("Steilplatte", names);     // 6b+
        Assert.DoesNotContain("Einsteigerroute", names); // 5c
    }

    [Fact]
    public async Task FilterGrad_NurMinimum_UndAntwortInAndererSkala()
    {
        // Filterwerte sind immer französisch, die Antwort kommt in der gewünschten Skala
        var routes = await GetAllAsync("minGrade=7a&scale=uiaa");
        var names  = routes.Select(r => r.Name).ToList();

        Assert.Contains("Überhang direkt", names); // 7a = VIII- (UIAA)
        Assert.Contains("Projektroute", names);    // 7b+
        Assert.DoesNotContain("Fingerkante", names);
        Assert.Equal("VIII-", routes.Single(r => r.Name == "Überhang direkt").Grade);
    }

    [Fact]
    public async Task FilterStil_IgnoriertGrossKleinschreibung()
    {
        var routes = await GetAllAsync("style=toprope");

        Assert.NotEmpty(routes);
        Assert.All(routes, r => Assert.Equal("Toprope", r.Style));
        Assert.Contains(routes, r => r.Name == "Linke Platte");
    }

    [Fact]
    public async Task FilterLaenge_VonBis_InMetern()
    {
        var routes = await GetAllAsync("minLength=18&maxLength=20");

        Assert.NotEmpty(routes);
        Assert.All(routes, r => Assert.InRange(r.LengthMeters!.Value, 18, 20));
        Assert.Contains(routes, r => r.Name == "Fingerkante");          // 20 m
        Assert.DoesNotContain(routes, r => r.Name == "Überhang direkt"); // 15 m
    }

    [Fact]
    public async Task Filter_KombiniertMitNamenssuche()
    {
        // "Platte" + Toprope: Linke Platte, Leichte Platte, Reibungsplatte – nicht Steilplatte (Vorstieg)
        var routes = await GetAllAsync("search=Platte&style=Toprope");
        var names  = routes.Select(r => r.Name).OrderBy(n => n).ToList();

        Assert.Equal(new[] { "Leichte Platte", "Linke Platte", "Reibungsplatte" }, names);
    }

    [Theory]
    [InlineData("minGrade=9z")]                 // unbekannter Grad
    [InlineData("maxGrade=abc")]
    [InlineData("minGrade=7a&maxGrade=6a")]     // von > bis
    [InlineData("minLength=30&maxLength=10")]   // von > bis
    [InlineData("minLength=-5")]                // negative Länge
    public async Task Filter_Ungueltig_Gibt400(string query)
    {
        var response = await _client.GetAsync($"/api/routes?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Stile_LiefertVorhandeneStileEinmalUndSortiert()
    {
        var styles = await _client.GetFromJsonAsync<List<string>>("/api/routes/styles");

        Assert.NotNull(styles);
        Assert.Contains("Toprope", styles);
        Assert.Contains("Vorstieg", styles);
        Assert.Equal(styles.Distinct().Count(), styles.Count);
        Assert.Equal(styles.OrderBy(s => s, StringComparer.Create(new System.Globalization.CultureInfo("de-AT"), true)), styles);
    }

    private record RouteResponse(int Id, int SectorId, string? SectorName, int AreaId, string? AreaName, string Name, string? Grade,
        string? Style = null, int? LengthMeters = null);
    private record PagedResponse<T>(int Total, int Page, int PageSize, List<T> Items);
}
