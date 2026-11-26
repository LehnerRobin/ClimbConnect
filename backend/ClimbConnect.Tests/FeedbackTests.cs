using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für Feedback an das Team (#244): Schreiben für eingeloggte User,
/// Lesen und Verwalten nur für den Admin, keine öffentlichen Kommentare mehr.
/// </summary>
public class FeedbackTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public FeedbackTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task User_SchreibtFeedback_AdminSiehtEsMitBezug()
    {
        var user   = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin  = await AdminAsync();
        var areas  = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        var area   = areas.GetProperty("items")[0];
        var areaId = area.GetProperty("id").GetInt32();

        var create = await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "  Zustieg ist neu markiert  ", areaId }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var entry = await FindAsync(admin, id);
        Assert.Equal("Zustieg ist neu markiert", entry.GetProperty("text").GetString());
        Assert.Equal("testuser", entry.GetProperty("username").GetString());
        Assert.Equal(areaId, entry.GetProperty("areaId").GetInt32());
        Assert.Equal(area.GetProperty("name").GetString(), entry.GetProperty("areaName").GetString());
        Assert.False(entry.GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task FeedbackOhneBezug_IstErlaubt()
    {
        var user = await LoginAsync("user@climbconnect.at", "User1234!");
        var create = await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "Coole App!" }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task Feedback_NurFuerAdminLesbar()
    {
        var user = await LoginAsync("user@climbconnect.at", "User1234!");

        var asUser = await SendAsync(HttpMethod.Get, "/api/feedback", user, null);
        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);

        var anonymous = await _client.GetAsync("/api/feedback");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task OhneLogin_KeinFeedback()
    {
        var response = await _client.PostAsJsonAsync("/api/feedback", new { text = "Hallo" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UngueltigesFeedback_Gibt400()
    {
        var user = await LoginAsync("user@climbconnect.at", "User1234!");

        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "   " }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = new string('x', 2001) }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "Gebiet fehlt", areaId = 999_999 }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "Route fehlt", routeId = 999_999 }))).StatusCode);
    }

    [Fact]
    public async Task Admin_MarkiertGelesen_UndLoescht()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();

        var create = await SendAsync(HttpMethod.Post, "/api/feedback", user,
            JsonContent.Create(new { text = "Bitte lesen" }));
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // User darf nicht verwalten
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Put, $"/api/feedback/{id}/read", user,
            JsonContent.Create(new { isRead = true }))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"/api/feedback/{id}", user, null)).StatusCode);

        var read = await SendAsync(HttpMethod.Put, $"/api/feedback/{id}/read", admin, JsonContent.Create(new { isRead = true }));
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.True((await FindAsync(admin, id)).GetProperty("isRead").GetBoolean());

        var delete = await SendAsync(HttpMethod.Delete, $"/api/feedback/{id}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await (await SendAsync(HttpMethod.Get, "/api/feedback", admin, null)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(list.EnumerateArray(), f => f.GetProperty("id").GetInt32() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, $"/api/feedback/{id}", admin, null)).StatusCode);
    }

    [Fact]
    public async Task UngeleseneStehenOben()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();

        var first = await SendAsync(HttpMethod.Post, "/api/feedback", user, JsonContent.Create(new { text = "Alt und gelesen" }));
        var firstId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await SendAsync(HttpMethod.Put, $"/api/feedback/{firstId}/read", admin, JsonContent.Create(new { isRead = true }));
        await SendAsync(HttpMethod.Post, "/api/feedback", user, JsonContent.Create(new { text = "Neu und ungelesen" }));

        var list = await (await SendAsync(HttpMethod.Get, "/api/feedback", admin, null)).Content.ReadFromJsonAsync<JsonElement>();
        var flags = list.EnumerateArray().Select(f => f.GetProperty("isRead").GetBoolean()).ToList();
        Assert.Equal(flags.OrderBy(x => x), flags);   // alle false vor allen true
    }

    [Fact]
    public async Task OeffentlicheKommentare_GibtEsNichtMehr()
    {
        var areas  = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        var areaId = areas.GetProperty("items")[0].GetProperty("id").GetInt32();

        var response = await _client.GetAsync($"/api/areas/{areaId}/comments");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Hilfsmethoden

    private async Task<JsonElement> FindAsync(string adminToken, int id)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/feedback", adminToken, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();
        return list.EnumerateArray().Single(f => f.GetProperty("id").GetInt32() == id);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private Task<string> AdminAsync() => LoginAsync("admin@climbconnect.at", "Admin1234!");

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
}
