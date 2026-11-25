using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für die Pinnwand beim Termin (#245): nur Teilnehmer lesen und schreiben,
/// Verfasser und Admin löschen, Beiträge verschwinden mit dem Termin.
/// </summary>
public class PinnwandTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public PinnwandTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ersteller_KannSchreibenUndLesen()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Ersteller");

        var post = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/posts", creator,
            JsonContent.Create(new { text = "  Bringt jemand ein zweites Seil mit?  " }));
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var created = await post.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bringt jemand ein zweites Seil mit?", created.GetProperty("text").GetString());
        Assert.Equal("testuser", created.GetProperty("authorUsername").GetString());
        Assert.False(created.GetProperty("reportedByMe").GetBoolean());

        var list = await GetPostsAsync(id, creator);
        Assert.Single(list.EnumerateArray());
    }

    [Fact]
    public async Task NichtTeilnehmer_Bekommt403BeimLesenUndSchreiben()
    {
        var creator  = await LoginAsync("user@climbconnect.at", "User1234!");
        var stranger = await RegisterAsync("pinnwand-fremd", "pinnwand-fremd@example.com", "Test1234!");
        var id       = await CreateAppointmentAsync(creator, "Pinnwand-Privat");

        var read = await SendAsync(HttpMethod.Get, $"/api/appointments/{id}/posts", stranger, null);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var write = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/posts", stranger,
            JsonContent.Create(new { text = "Hallo" }));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task OhneLogin_Gibt401()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Anonym");

        var response = await _client.GetAsync($"/api/appointments/{id}/posts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Teilnehmer_SiehtPinnwand_NachAustretenNichtMehr()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var member  = await RegisterAsync("pinnwand-mitglied", "pinnwand-mitglied@example.com", "Test1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Austreten");

        await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/subscribe", member,
            JsonContent.Create(new { comment = (string?)null }));
        var write = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/posts", member,
            JsonContent.Create(new { text = "Ich fahre ab Linz, 2 Plätze frei" }));
        Assert.Equal(HttpStatusCode.Created, write.StatusCode);

        await SendAsync(HttpMethod.Delete, $"/api/appointments/{id}/subscribe", member, null);

        var read = await SendAsync(HttpMethod.Get, $"/api/appointments/{id}/posts", member, null);
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
    }

    [Fact]
    public async Task Loeschen_NurVerfasserOderAdmin()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var member  = await RegisterAsync("pinnwand-loeschen", "pinnwand-loeschen@example.com", "Test1234!");
        var admin   = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Löschen");

        await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/subscribe", member,
            JsonContent.Create(new { comment = (string?)null }));
        var first  = await CreatePostAsync(id, creator, "Beitrag vom Ersteller");
        var second = await CreatePostAsync(id, creator, "Noch ein Beitrag");

        // Fremder Teilnehmer darf nicht löschen
        var foreign = await SendAsync(HttpMethod.Delete, $"/api/posts/{first}", member, null);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);

        // Verfasser darf
        var own = await SendAsync(HttpMethod.Delete, $"/api/posts/{first}", creator, null);
        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);

        // Admin darf alle
        var byAdmin = await SendAsync(HttpMethod.Delete, $"/api/posts/{second}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);

        var list = await GetPostsAsync(id, creator);
        Assert.Empty(list.EnumerateArray());
    }

    [Fact]
    public async Task TerminLoeschen_LoeschtBeitraege()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Termin-weg");
        var postId  = await CreatePostAsync(id, creator, "Wird mitgelöscht");

        var delete = await SendAsync(HttpMethod.Delete, $"/api/appointments/{id}", creator, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Der Beitrag existiert nicht mehr
        var deletePost = await SendAsync(HttpMethod.Delete, $"/api/posts/{postId}", creator, null);
        Assert.Equal(HttpStatusCode.NotFound, deletePost.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LeererText_Gibt400(string text)
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Leer");

        var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/posts", creator,
            JsonContent.Create(new { text }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ZuLangerText_Gibt400()
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var id      = await CreateAppointmentAsync(creator, "Pinnwand-Lang");

        var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/posts", creator,
            JsonContent.Create(new { text = new string('a', 1001) }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Hilfsmethoden

    private async Task<int> CreateAppointmentAsync(string token, string title)
    {
        var areas  = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        var areaId = areas.GetProperty("items")[0].GetProperty("id").GetInt32();
        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title, date = "2030-07-01T18:00:00Z" }));
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<int> CreatePostAsync(int appointmentId, string token, string text)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/posts", token,
            JsonContent.Create(new { text }));
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> GetPostsAsync(int appointmentId, string token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/appointments/{appointmentId}/posts", token, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private async Task<string> RegisterAsync(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
}
