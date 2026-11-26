using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für #238: Bilder verschwinden mit den Einträgen, zu denen sie gehören –
/// auch bei kaskadierenden Löschungen (Gebiet → Sektor → Route → Feedback/Reports).
/// Laufen gegen eine echte SQLite-Datei, weil nur dort die Kaskaden der Datenbank greifen.
/// </summary>
public class PhotoCleanupTests : IClassFixture<SqliteFactory>
{
    private readonly SqliteFactory _factory;
    private readonly HttpClient _client;

    public PhotoCleanupTests(SqliteFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task FeedbackLoeschen_LoeschtFoto()
    {
        var user   = await LoginAsync("user@climbconnect.at", "User1234!");
        var areaId = (await CreateTreeAsync("Foto-Feedback")).AreaId;

        var photo      = await UploadPngAsync(user);
        var feedbackId = await FeedbackAsync(user, photo, areaId: areaId);
        Assert.True(PhotoExists(photo));

        var delete = await SendAsync(HttpMethod.Delete, $"/api/feedback/{feedbackId}", await AdminAsync(), null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(PhotoExists(photo));
    }

    [Fact]
    public async Task RouteLoeschen_LoeschtFotosVonFeedbackUndReports()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();
        var tree  = await CreateTreeAsync("Foto-Route");

        var feedbackPhoto = await UploadPngAsync(user);
        var reportPhoto  = await UploadPngAsync(user);
        await FeedbackAsync(user, feedbackPhoto, routeId: tree.RouteId);
        await ReportAsync(user, new { routeId = tree.RouteId, text = "Haken locker", severity = "High", photoUrl = reportPhoto });

        var delete = await SendAsync(HttpMethod.Delete, $"/api/routes/{tree.RouteId}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(PhotoExists(feedbackPhoto));
        Assert.False(PhotoExists(reportPhoto));
    }

    [Fact]
    public async Task SektorLoeschen_LoeschtFotosDerRouten()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();
        var tree  = await CreateTreeAsync("Foto-Sektor");

        var photo = await UploadPngAsync(user);
        await FeedbackAsync(user, photo, routeId: tree.RouteId);

        var delete = await SendAsync(HttpMethod.Delete, $"/api/sectors/{tree.SectorId}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(PhotoExists(photo));
    }

    [Fact]
    public async Task GebietLoeschen_LoeschtTitelbildUndAlleFotos()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();

        var titleImage = await UploadPngAsync(admin);
        var tree = await CreateTreeAsync("Foto-Gebiet", titleImage);

        var areaPhoto  = await UploadPngAsync(user);
        var routePhoto = await UploadPngAsync(user);
        var reportPhoto = await UploadPngAsync(user);
        await FeedbackAsync(user, areaPhoto, areaId: tree.AreaId);
        await FeedbackAsync(user, routePhoto, routeId: tree.RouteId);
        await ReportAsync(user, new { areaId = tree.AreaId, text = "Steinschlag", severity = "Medium", photoUrl = reportPhoto });

        var delete = await SendAsync(HttpMethod.Delete, $"/api/areas/{tree.AreaId}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(PhotoExists(titleImage));
        Assert.False(PhotoExists(areaPhoto));
        Assert.False(PhotoExists(routePhoto));
        Assert.False(PhotoExists(reportPhoto));
    }

    [Fact]
    public async Task GebietBearbeiten_NeuesTitelbild_LoeschtAltes()
    {
        var admin    = await AdminAsync();
        var oldImage = await UploadPngAsync(admin);
        var newImage = await UploadPngAsync(admin);
        var tree     = await CreateTreeAsync("Foto-Titelbild", oldImage);

        // Unverändert speichern → Bild bleibt
        var same = await SendAsync(HttpMethod.Put, $"/api/areas/{tree.AreaId}", admin,
            JsonContent.Create(new { name = "Foto-Titelbild", imageUrl = oldImage }));
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.True(PhotoExists(oldImage));

        // Neues Bild → altes ist weg, neues bleibt
        var update = await SendAsync(HttpMethod.Put, $"/api/areas/{tree.AreaId}", admin,
            JsonContent.Create(new { name = "Foto-Titelbild", imageUrl = newImage }));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.False(PhotoExists(oldImage));
        Assert.True(PhotoExists(newImage));
    }

    [Fact]
    public async Task FotoInAnderemGebietVerwendet_BleibtErhalten()
    {
        var user  = await LoginAsync("user@climbconnect.at", "User1234!");
        var admin = await AdminAsync();
        var gone  = await CreateTreeAsync("Foto-Weg");
        var stays = await CreateTreeAsync("Foto-Bleibt");

        var photo = await UploadPngAsync(user);
        await FeedbackAsync(user, photo, areaId: gone.AreaId);
        await FeedbackAsync(user, photo, areaId: stays.AreaId);

        await SendAsync(HttpMethod.Delete, $"/api/areas/{gone.AreaId}", admin, null);
        Assert.True(PhotoExists(photo));
    }

    // Hilfsmethoden

    private bool PhotoExists(string url) =>
        File.Exists(Path.Combine(_factory.TempDir, "uploads", Path.GetFileName(url)));

    /// <summary>Legt Gebiet → Sektor → Route an (als Admin) und gibt die Ids zurück.</summary>
    private async Task<(int AreaId, int SectorId, int RouteId)> CreateTreeAsync(string name, string? imageUrl = null)
    {
        var admin = await AdminAsync();

        var area = await SendAsync(HttpMethod.Post, "/api/areas", admin, JsonContent.Create(new { name, imageUrl }));
        Assert.Equal(HttpStatusCode.Created, area.StatusCode);
        var areaId = (await area.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var sector = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/sectors", admin,
            JsonContent.Create(new { name = $"{name}-Sektor" }));
        Assert.Equal(HttpStatusCode.Created, sector.StatusCode);
        var sectorId = (await sector.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var route = await SendAsync(HttpMethod.Post, $"/api/sectors/{sectorId}/routes", admin,
            JsonContent.Create(new { name = $"{name}-Route", grade = "6a" }));
        Assert.Equal(HttpStatusCode.Created, route.StatusCode);
        var routeId = (await route.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        return (areaId, sectorId, routeId);
    }

    private async Task<int> FeedbackAsync(string token, string photoUrl, int? areaId = null, int? routeId = null)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/feedback", token,
            JsonContent.Create(new { text = "Feedback mit Foto", photoUrl, areaId, routeId }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task ReportAsync(string token, object body)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/reports", token, JsonContent.Create(body));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

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
