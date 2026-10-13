using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimbConnect.API.Services;

namespace ClimbConnect.Tests;

/// <summary>Tests für die Grad-Tabelle, den Grades-Endpoint und die Grad-Validierung.</summary>
public class GradeTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public GradeTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetGrades_LiefertSortierteListeInAllenSkalen()
    {
        var grades = await _client.GetFromJsonAsync<JsonElement>("/api/grades");

        var french = grades.EnumerateArray().Select(g => g.GetProperty("french").GetString()!).ToList();
        Assert.Equal("3a", french.First());
        Assert.Equal("9c", french.Last());
        Assert.Contains("5b+", french);
        Assert.Equal(GradeConversionService.GetAllGrades(), french);
        Assert.Equal("VI", grades.EnumerateArray().Single(g => g.GetProperty("french").GetString() == "6a")
                                 .GetProperty("uiaa").GetString());
    }

    [Fact]
    public void RangUndUmrechnung_PassenZusammen()
    {
        // Jeder Grad ist schwerer als sein Vorgänger und lässt sich in alle Skalen umrechnen
        var all = GradeConversionService.GetAllGrades();
        for (var i = 0; i < all.Count; i++)
        {
            Assert.Equal(i, GradeConversionService.Rank(all[i]));
            Assert.NotNull(GradeConversionService.Convert(all[i], "uiaa"));
            Assert.NotNull(GradeConversionService.Convert(all[i], "american"));
        }
        Assert.Equal(-1, GradeConversionService.Rank("banane"));
        Assert.Equal("6a+", GradeConversionService.Convert(" 6A+ ", "french"));
    }

    [Fact]
    public async Task CreateRoute_UnbekannterGrad_Gibt400()
    {
        var token     = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var areas     = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        var areaId    = areas.GetProperty("items")[0].GetProperty("id").GetInt32();
        var sectors   = await _client.GetFromJsonAsync<JsonElement>($"/api/areas/{areaId}/sectors");
        var sectorId  = sectors[0].GetProperty("id").GetInt32();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sectors/{sectorId}/routes")
        {
            Content = JsonContent.Create(new { name = "Bananenroute", grade = "banane" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProgress_UnbekannterSubjektiverGrad_Gibt400()
    {
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var routes = await _client.GetFromJsonAsync<JsonElement>("/api/routes?pageSize=1");
        var routeId = routes.GetProperty("items")[0].GetProperty("id").GetInt32();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/progress")
        {
            Content = JsonContent.Create(new
            {
                routeId, status = "Rotpunkt", climbingStyle = "Vorstieg", attempts = 1,
                date = "2026-06-01", subjectiveGrade = "4+"
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private record AuthResponse(string Token);
}
