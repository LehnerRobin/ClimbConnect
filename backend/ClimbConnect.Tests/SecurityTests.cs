using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für sicherheitsrelevantes Verhalten: keine sensiblen Daten in
/// Antworten, DTO-Validierung und Prüfung hochgeladener Dateien.
/// </summary>
public class SecurityTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public SecurityTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFeedback_EnthältWederPasswortHashNochEmail()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin  = await LoginAsync("admin@climbconnect.at", "Admin1234!");

        var create = await SendAsync(HttpMethod.Post, "/api/feedback", token,
            JsonContent.Create(new { text = "Schöner Fels", areaId }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        // Auch der Admin bekommt vom Verfasser nur den Benutzernamen
        var response = await SendAsync(HttpMethod.Get, "/api/feedback", admin, null);
        var json = await response.Content.ReadAsStringAsync();

        Assert.Contains("testuser", json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user@climbconnect.at", json);
    }

    [Fact]
    public async Task CreateAppointment_AntwortEnthältKeinenPasswortHash()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var response = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Feierabendrunde", date = "2030-06-01T18:00:00" }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user@climbconnect.at", json);
    }

    [Fact]
    public async Task ChangePassword_ZuKurzesPasswort_Gibt400()
    {
        var token = await RegisterAsync("kurzpasswort", "kurzpasswort@example.com", "Test1234!");

        var response = await SendAsync(HttpMethod.Put, "/api/users/me/password", token,
            JsonContent.Create(new { currentPassword = "Test1234!", newPassword = "a" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UngültigeEmail_Gibt400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ohneemail",
            email    = "keine-email",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ZuKurzerUsername_Gibt400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ab",
            email    = "kurz@example.com",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_HtmlAlsBildGetarnt_Gibt400()
    {
        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        var bytes = Encoding.UTF8.GetBytes("<script>alert(1)</script>");

        var response = await SendAsync(HttpMethod.Post, "/api/upload", token,
            FileContent(bytes, "bild.html", "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_EchtesPng_WirdMitPngEndungGespeichert()
    {
        var token = await LoginAsync("user@climbconnect.at", "User1234!");
        // PNG-Signatur + etwas Inhalt; der Dateiname behauptet absichtlich ".html"
        byte[] bytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];

        var response = await SendAsync(HttpMethod.Post, "/api/upload", token,
            FileContent(bytes, "bild.html", "image/png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith(".png", body.GetProperty("url").GetString());
    }

    [Theory]
    [InlineData("https://tracker.example.com/pixel.png")]
    [InlineData("/uploads/gibt-es-nicht.png")]
    [InlineData("/uploads/../appsettings.json")]
    public async Task Feedback_FremderFotoLink_Gibt400(string photoUrl)
    {
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var response = await SendAsync(HttpMethod.Post, "/api/feedback", token,
            JsonContent.Create(new { text = "Mit fremdem Foto", photoUrl }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_FremderFotoLink_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var response = await SendAsync(HttpMethod.Post, "/api/reports", token,
            JsonContent.Create(new { areaId, text = "Lockerer Haken", severity = "High",
                                     photoUrl = "https://tracker.example.com/pixel.png" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_EigenesHochgeladenesFoto_WirdAngenommen()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];
        var upload = await SendAsync(HttpMethod.Post, "/api/upload", token, FileContent(png, "foto.png", "image/png"));
        var url    = (await upload.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString();

        var response = await SendAsync(HttpMethod.Post, "/api/feedback", token,
            JsonContent.Create(new { text = "Mit eigenem Foto", photoUrl = url, areaId }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // Hilfsmethoden

    /// <summary>Schickt eine Anfrage mit Bearer-Token, ohne den gemeinsamen Client zu verändern.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    private async Task<int> GetFirstAreaIdAsync()
    {
        var areas = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        return areas.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private async Task<string> RegisterAsync(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private record AuthResponse(string Token);
}
