using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für „Eigenes Konto löschen" (#232). Laufen gegen eine echte SQLite-Datei,
/// damit auch die Fremdschlüssel der Datenbank geprüft werden (In-Memory kennt keine).
/// </summary>
public class AccountDeletionTests : IClassFixture<SqliteFactory>
{
    private readonly SqliteFactory _factory;
    private readonly HttpClient _client;

    public AccountDeletionTests(SqliteFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task KontoLoeschen_EntferntAlleDatenUndBilder()
    {
        var token   = await RegisterAsync("loesch-mich");
        var other   = await RegisterAsync("loesch-andere");
        var areaId  = await GetFirstAreaIdAsync();
        var routeId = await GetFirstRouteIdAsync();

        // Foto hochladen und in einem Kommentar verwenden
        var photoUrl  = await UploadPngAsync(token);
        var photoPath = Path.Combine(_factory.TempDir, "uploads", Path.GetFileName(photoUrl));
        Assert.True(File.Exists(photoPath));
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/comments", token,
            JsonContent.Create(new { text = "Kommentar mit Foto", photoUrl }))).StatusCode);

        // Report, Begehung, eigener Termin mit fremdem Teilnehmer und Pinnwand-Beitrag
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(HttpMethod.Post, "/api/reports", token,
            JsonContent.Create(new { areaId, text = "Lockerer Haken", severity = "High" }))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await SendAsync(HttpMethod.Post, "/api/progress", token,
            JsonContent.Create(new { routeId, status = "Rotpunkt", climbingStyle = "Vorstieg", attempts = 2, date = "2026-11-01" }))).StatusCode);

        var ownAppointment = await CreateAppointmentAsync(token, areaId, "Eigener Termin");
        await SendAsync(HttpMethod.Post, $"/api/appointments/{ownAppointment}/subscribe", other,
            JsonContent.Create(new { comment = (string?)null }));
        await CreatePostAsync(ownAppointment, other, "Beitrag im fremden Termin");

        // Teilnahme und Beitrag beim Termin eines anderen
        var foreignAppointment = await CreateAppointmentAsync(other, areaId, "Termin der anderen");
        await SendAsync(HttpMethod.Post, $"/api/appointments/{foreignAppointment}/subscribe", token,
            JsonContent.Create(new { comment = (string?)null }));
        await CreatePostAsync(foreignAppointment, token, "Ich komme auch");

        // Löschen
        var delete = await DeleteAccountAsync(token, "Test1234!");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        // Login geht nicht mehr, Bild ist weg
        var login = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = "loesch-mich@example.com", password = "Test1234!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.False(File.Exists(photoPath));

        // Eigener Termin ist weg, der Termin der anderen bleibt – ohne Teilnahme und Beitrag
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Get, $"/api/appointments/{ownAppointment}", other, null)).StatusCode);
        var posts = await SendAsync(HttpMethod.Get, $"/api/appointments/{foreignAppointment}/posts", other, null);
        Assert.Equal(HttpStatusCode.OK, posts.StatusCode);
        Assert.Empty((await posts.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());

        // Kommentar und Report sind aus dem Gebiet verschwunden
        var comments = await _client.GetStringAsync($"/api/areas/{areaId}/comments");
        Assert.DoesNotContain("Kommentar mit Foto", comments);

        // In der Benutzerliste taucht der Name nicht mehr auf
        var users = await _client.GetStringAsync("/api/users");
        Assert.DoesNotContain("\"loesch-mich\"", users);
    }

    [Fact]
    public async Task FalschesPasswort_Gibt400_KontoBleibt()
    {
        var token = await RegisterAsync("loesch-falsch");

        var delete = await DeleteAccountAsync(token, "Falsch1234!");
        Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);

        var me = await SendAsync(HttpMethod.Get, "/api/users/me", token, null);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task LetzterAdmin_KannNichtLoeschen()
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");

        var delete = await DeleteAccountAsync(admin, "Admin1234!");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task OhneLogin_Gibt401()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me")
        {
            Content = JsonContent.Create(new { password = "egal" })
        };
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GemeinsamVerwendetesFoto_BleibtErhalten()
    {
        var owner  = await RegisterAsync("loesch-foto1");
        var keeper = await RegisterAsync("loesch-foto2");
        var areaId = await GetFirstAreaIdAsync();

        // Beide Kommentare zeigen auf dasselbe Bild
        var photoUrl = await UploadPngAsync(owner);
        await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/comments", owner,
            JsonContent.Create(new { text = "Original", photoUrl }));
        await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/comments", keeper,
            JsonContent.Create(new { text = "Gleiches Foto", photoUrl }));

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAccountAsync(owner, "Test1234!")).StatusCode);

        var photoPath = Path.Combine(_factory.TempDir, "uploads", Path.GetFileName(photoUrl));
        Assert.True(File.Exists(photoPath));
    }

    // Hilfsmethoden

    private async Task<HttpResponseMessage> DeleteAccountAsync(string token, string password) =>
        await SendAsync(HttpMethod.Delete, "/api/users/me", token, JsonContent.Create(new { password }));

    private async Task<string> UploadPngAsync(string token)
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var upload = await SendAsync(HttpMethod.Post, "/api/upload", token,
            new MultipartFormDataContent { { file, "file", "foto.png" } });
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        return (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!;
    }

    private async Task<int> CreateAppointmentAsync(string token, int areaId, string title)
    {
        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title, date = "2030-07-01T18:00:00Z" }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task CreatePostAsync(int appointmentId, string token, string text)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/posts", token,
            JsonContent.Create(new { text }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<int> GetFirstAreaIdAsync()
    {
        var areas = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        return areas.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    private async Task<int> GetFirstRouteIdAsync()
    {
        var routes = await _client.GetFromJsonAsync<JsonElement>("/api/routes");
        return routes.GetProperty("items")[0].GetProperty("id").GetInt32();
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

    private async Task<string> RegisterAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, email = $"{username}@example.com", password = "Test1234!" });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
}
