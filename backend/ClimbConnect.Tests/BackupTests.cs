using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClimbConnect.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimbConnect.Tests;

/// <summary>
/// Factory mit echter SQLite-Datei statt In-Memory-Datenbank – das Backup braucht SQLite.
/// Datenbank und Upload-Ordner liegen in einem eigenen Temp-Ordner.
/// </summary>
public class SqliteFactory : WebApplicationFactory<Program>
{
    public string TempDir { get; } = Directory.CreateTempSubdirectory("climbconnect-test-").FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimit:AuthPermitLimit", "10000");
        builder.UseSetting("Storage:UploadsPath", Path.Combine(TempDir, "uploads"));
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var d in toRemove) services.Remove(d);
            services.AddDbContext<AppDbContext>(o =>
                o.UseSqlite($"Data Source={Path.Combine(TempDir, "test.db")};Pooling=False"));
        });
        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { Directory.Delete(TempDir, recursive: true); } catch (IOException) { /* Temp-Ordner, egal */ }
    }
}

/// <summary>Integrationstests für den Backup-Download (#236).</summary>
public class BackupTests : IClassFixture<SqliteFactory>
{
    private readonly HttpClient _client;

    public BackupTests(SqliteFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Backup_OhneLogin_Gibt401()
    {
        var response = await _client.GetAsync("/api/admin/backup");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Backup_AlsUser_Gibt403()
    {
        var token    = await LoginAsync("user@climbconnect.at", "User1234!");
        var response = await GetAsync("/api/admin/backup", token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Backup_AlsAdmin_LiefertZipMitDatenbankUndBildern()
    {
        var token = await LoginAsync("admin@climbconnect.at", "Admin1234!");

        // Ein Bild hochladen, damit der uploads-Ordner nicht leer ist
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/upload")
        {
            Content = new MultipartFormDataContent { { file, "file", "foto.png" } }
        };
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(uploadRequest)).StatusCode);

        var response = await GetAsync("/api/admin/backup", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        // ZIP auspacken und prüfen, ob die Datenbank-Kopie lesbar ist und Daten enthält
        var extractDir = Directory.CreateTempSubdirectory("climbconnect-restore-").FullName;
        try
        {
            using (var zip = new ZipArchive(await response.Content.ReadAsStreamAsync()))
            {
                Assert.Contains(zip.Entries, e => e.FullName.StartsWith("uploads/") && e.FullName.EndsWith(".png"));
                zip.ExtractToDirectory(extractDir);
            }

            await using var conn = new SqliteConnection($"Data Source={Path.Combine(extractDir, "ClimbConnect.db")};Pooling=False");
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users";
            var users = (long)(await cmd.ExecuteScalarAsync())!;
            Assert.True(users >= 2);
        }
        finally
        {
            Directory.Delete(extractDir, recursive: true);
        }
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private record AuthResponse(string Token);
}
