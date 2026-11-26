using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für "Mehrere Admins verwalten" (#237): Benutzerliste mit Rolle,
/// Admin-Rechte vergeben und entziehen, letzter Admin bleibt Admin.
/// Dazu die Prüfung, dass die Rolle bei jeder Anfrage aus der Datenbank kommt – sonst
/// wäre ein entzogenes Admin-Recht mit dem alten Token noch bis zu 24 Stunden gültig.
/// </summary>
public class AdminUserTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public AdminUserTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Liste_OhneLogin401_AlsUser403()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/admin/users", null, null)).StatusCode);

        var user = await LoginAsync("user@climbconnect.at", "User1234!");
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/admin/users", user, null)).StatusCode);
    }

    [Fact]
    public async Task Liste_AlsAdmin_ZeigtRolle_OhneEmailUndPasswort()
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var users = await GetUsersAsync(admin);

        Assert.Equal("admin", users.Single(u => u.GetProperty("username").GetString() == "admin").GetProperty("role").GetString());
        Assert.Equal("user",  users.Single(u => u.GetProperty("username").GetString() == "testuser").GetProperty("role").GetString());
        Assert.All(users, u =>
        {
            Assert.False(u.TryGetProperty("email", out _));
            Assert.False(u.TryGetProperty("passwordHash", out _));
        });
    }

    [Fact]
    public async Task Vergeben_AlterTokenHatSofortAdminRechte_OhneNeuAnmelden()
    {
        var admin   = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var newbie  = await RegisterAsync("neuer-admin");   // Token mit Rolle "user"
        var newbieId = await GetUserIdAsync(admin, "neuer-admin");

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/admin/users", newbie, null)).StatusCode);

        var promote = await SetRoleAsync(admin, newbieId, "admin");
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        Assert.Equal("admin", (await promote.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("role").GetString());

        // Gleicher Token wie vorher – die Rolle kommt jetzt aus der Datenbank
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/admin/users", newbie, null)).StatusCode);
    }

    [Fact]
    public async Task Entziehen_AlterAdminTokenVerliertSofortDieRechte()
    {
        var admin  = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        await RegisterAsync("ex-admin");
        var exId   = await GetUserIdAsync(admin, "ex-admin");
        Assert.Equal(HttpStatusCode.OK, (await SetRoleAsync(admin, exId, "admin")).StatusCode);

        // Neu anmelden: Dieser Token enthält jetzt die Rolle "admin"
        var exAdminToken = await LoginAsync("ex-admin@example.com", "Test1234!");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/admin/users", exAdminToken, null)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SetRoleAsync(admin, exId, "user")).StatusCode);

        // Trotz "admin" im Token: kein Admin-Zugriff mehr, normale User-Funktionen gehen weiter
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/admin/users", exAdminToken, null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/users/me", exAdminToken, null)).StatusCode);
    }

    [Fact]
    public async Task LetzterAdmin_KannSichDieRechteNichtEntziehen()
    {
        var admin   = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var adminId = await GetUserIdAsync(admin, "admin");

        // Andere Tests dieser Klasse machen evtl. weitere Admins – die zuerst zurückstufen
        foreach (var other in (await GetUsersAsync(admin))
                     .Where(u => u.GetProperty("role").GetString() == "admin" && u.GetProperty("id").GetInt32() != adminId))
            Assert.Equal(HttpStatusCode.OK, (await SetRoleAsync(admin, other.GetProperty("id").GetInt32(), "user")).StatusCode);

        var self = await SetRoleAsync(admin, adminId, "user");
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);

        // Immer noch Admin
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/admin/users", admin, null)).StatusCode);
    }

    [Fact]
    public async Task ZweiterAdmin_DarfSichSelbstZurueckstufen()
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        await RegisterAsync("selbst-zurueck");
        var id = await GetUserIdAsync(admin, "selbst-zurueck");
        Assert.Equal(HttpStatusCode.OK, (await SetRoleAsync(admin, id, "admin")).StatusCode);

        var self = await LoginAsync("selbst-zurueck@example.com", "Test1234!");
        Assert.Equal(HttpStatusCode.OK, (await SetRoleAsync(self, id, "user")).StatusCode);
    }

    [Theory]
    [InlineData("superuser")]
    [InlineData("")]
    public async Task UngueltigeRolle_Gibt400(string role)
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        var id    = await GetUserIdAsync(admin, "testuser");
        Assert.Equal(HttpStatusCode.BadRequest, (await SetRoleAsync(admin, id, role)).StatusCode);
    }

    [Fact]
    public async Task UnbekannterUser_Gibt404_UndUserDarfKeineRollenAendern()
    {
        var admin = await LoginAsync("admin@climbconnect.at", "Admin1234!");
        Assert.Equal(HttpStatusCode.NotFound, (await SetRoleAsync(admin, 999_999, "admin")).StatusCode);

        var user   = await LoginAsync("user@climbconnect.at", "User1234!");
        var userId = await GetUserIdAsync(admin, "testuser");
        Assert.Equal(HttpStatusCode.Forbidden, (await SetRoleAsync(user, userId, "admin")).StatusCode);
    }

    [Fact]
    public async Task GeloeschtesKonto_TokenWirdUngueltig()
    {
        var token = await RegisterAsync("weg-damit");
        var delete = await SendAsync(HttpMethod.Delete, "/api/users/me", token, JsonContent.Create(new { password = "Test1234!" }));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/users/me", token, null)).StatusCode);
    }

    // Hilfsmethoden

    private async Task<List<JsonElement>> GetUsersAsync(string adminToken)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/admin/users", adminToken, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
    }

    private async Task<int> GetUserIdAsync(string adminToken, string username) =>
        (await GetUsersAsync(adminToken)).Single(u => u.GetProperty("username").GetString() == username).GetProperty("id").GetInt32();

    private Task<HttpResponseMessage> SetRoleAsync(string token, int userId, string role) =>
        SendAsync(HttpMethod.Put, $"/api/admin/users/{userId}/role", token, JsonContent.Create(new { role }));

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> RegisterAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { username, email = $"{username}@example.com", password = "Test1234!" });
        Assert.True(response.IsSuccessStatusCode, $"Registrierung von {username} fehlgeschlagen");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.True(response.IsSuccessStatusCode, $"Login {email} fehlgeschlagen");
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }
}
