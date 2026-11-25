using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimbConnect.API.Data;
using ClimbConnect.API.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ClimbConnect.Tests;

/// <summary>
/// Integrationstests für das Melden von Pinnwand-Beiträgen und die Fälle des Admins (#264).
/// Jeder Test legt einen eigenen Termin an, damit sich die Fälle nicht in die Quere kommen.
/// </summary>
public class ModerationTests : IClassFixture<ClimbConnectFactory>
{
    private readonly ClimbConnectFactory _factory;
    private readonly HttpClient _client;

    public ModerationTests(ClimbConnectFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task ErsteMeldung_LegtFallAnUndKopiertGanzePinnwand()
    {
        var (creator, member, id) = await SetupAsync("mod-erste");
        var a = await CreatePostAsync(id, creator, "Treffpunkt 9 Uhr Parkplatz");
        var b = await CreatePostAsync(id, creator, "Unfreundlicher Beitrag");
        await CreatePostAsync(id, member, "Ich bin dabei");

        var report = await ReportAsync(b, member, "Beleidigung", null);
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        var detail = await GetCaseAsync(caseId);
        var posts  = detail.GetProperty("posts").EnumerateArray().ToList();
        Assert.Equal(3, posts.Count);
        Assert.Equal(1, detail.GetProperty("reportCount").GetInt32());
        Assert.Equal(1, detail.GetProperty("reportedPostCount").GetInt32());
        Assert.False(detail.GetProperty("appointmentDeleted").GetBoolean());

        var reported = posts.Single(p => p.GetProperty("originalPostId").GetInt32() == b);
        var meldung  = reported.GetProperty("reports")[0];
        Assert.Equal("Beleidigung", meldung.GetProperty("reason").GetString());
        Assert.Equal("mod-erste", meldung.GetProperty("reporterUsername").GetString());
        Assert.Empty(posts.Single(p => p.GetProperty("originalPostId").GetInt32() == a).GetProperty("reports").EnumerateArray());

        // Auf der Pinnwand steht jetzt "Gemeldet"
        var list = await GetPostsAsync(id, member);
        Assert.True(list.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == b).GetProperty("reportedByMe").GetBoolean());
        Assert.False(list.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == a).GetProperty("reportedByMe").GetBoolean());
    }

    [Fact]
    public async Task ZweiteMeldung_KopiertNichtsDoppelt_ErgaenztNeueBeitraege()
    {
        var (creator, member, id) = await SetupAsync("mod-zweite");
        var a = await CreatePostAsync(id, creator, "Erster Beitrag");
        var b = await CreatePostAsync(id, creator, "Zweiter Beitrag");

        var first  = await ReportAsync(a, member, "Beleidigung", null);
        var caseId = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        // Neuer Beitrag nach dem Anlegen des Falls
        var c = await CreatePostAsync(id, creator, "Dritter Beitrag");

        var second = await ReportAsync(b, member, "Belaestigung", null);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(caseId, (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32());

        var detail = await GetCaseAsync(caseId);
        var originals = detail.GetProperty("posts").EnumerateArray()
            .Select(p => p.GetProperty("originalPostId").GetInt32()).ToList();
        Assert.Equal(new[] { a, b, c }, originals);
        Assert.Equal(2, detail.GetProperty("reportCount").GetInt32());
    }

    [Fact]
    public async Task GeloeschterBeitrag_BleibtImFall()
    {
        var (creator, member, id) = await SetupAsync("mod-geloescht");
        var post = await CreatePostAsync(id, creator, "Wird gleich gelöscht");

        var report = await ReportAsync(post, member, "UnangemessenerInhalt", null);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        var delete = await SendAsync(HttpMethod.Delete, $"/api/posts/{post}", creator, null);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var copy = (await GetCaseAsync(caseId)).GetProperty("posts")[0];
        Assert.Equal("Wird gleich gelöscht", copy.GetProperty("text").GetString());
        Assert.True(copy.GetProperty("deletedByAuthor").GetBoolean());
    }

    [Fact]
    public async Task GeloeschterTermin_FallBleibtErhalten()
    {
        var (creator, member, id) = await SetupAsync("mod-termin-weg");
        var post = await CreatePostAsync(id, creator, "Beitrag im gelöschten Termin");

        var report = await ReportAsync(post, member, "Beleidigung", null);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        await SendAsync(HttpMethod.Delete, $"/api/appointments/{id}", creator, null);

        var detail = await GetCaseAsync(caseId);
        Assert.True(detail.GetProperty("appointmentDeleted").GetBoolean());
        Assert.Equal("mod-termin-weg-Termin", detail.GetProperty("appointmentTitle").GetString());
        Assert.Single(detail.GetProperty("posts").EnumerateArray());
    }

    [Fact]
    public async Task GleicherBeitragZweimal_Gibt409()
    {
        var (creator, member, id) = await SetupAsync("mod-doppelt");
        var post = await CreatePostAsync(id, creator, "Nur einmal melden");

        Assert.Equal(HttpStatusCode.Created, (await ReportAsync(post, member, "Beleidigung", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await ReportAsync(post, member, "Beleidigung", null)).StatusCode);
    }

    [Fact]
    public async Task UngueltigeMeldungen_Geben400()
    {
        var (creator, member, id) = await SetupAsync("mod-ungueltig");
        var foreign = await CreatePostAsync(id, creator, "Fremder Beitrag");
        var own     = await CreatePostAsync(id, member, "Eigener Beitrag");

        Assert.Equal(HttpStatusCode.BadRequest, (await ReportAsync(own, member, "Beleidigung", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReportAsync(foreign, member, "Quatsch", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReportAsync(foreign, member, "Sonstiges", "   ")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ReportAsync(foreign, member, "Sonstiges", new string('x', 501))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await ReportAsync(foreign, member, "Sonstiges", "Spam-Link")).StatusCode);
    }

    [Fact]
    public async Task NichtTeilnehmer_Gibt403_UnbekannterBeitrag_Gibt404()
    {
        var (creator, _, id) = await SetupAsync("mod-fremd");
        var stranger = await RegisterAsync("mod-fremd-x");
        var post = await CreatePostAsync(id, creator, "Beitrag");

        Assert.Equal(HttpStatusCode.Forbidden, (await ReportAsync(post, stranger, "Beleidigung", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ReportAsync(999_999, stranger, "Beleidigung", null)).StatusCode);
    }

    [Fact]
    public async Task Faelle_NurFuerAdmin()
    {
        var user = await LoginAsync("user@climbconnect.at", "User1234!");
        var list = await SendAsync(HttpMethod.Get, "/api/moderation/cases", user, null);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);

        var anonymous = await _client.GetAsync("/api/moderation/cases");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task FallErledigen_LoeschtFall_DanachWiederMeldbar()
    {
        var (creator, member, id) = await SetupAsync("mod-erledigt");
        var post = await CreatePostAsync(id, creator, "Beitrag");
        var admin = await AdminAsync();

        var report = await ReportAsync(post, member, "Beleidigung", null);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        var resolve = await SendAsync(HttpMethod.Delete, $"/api/moderation/cases/{caseId}", admin, null);
        Assert.Equal(HttpStatusCode.NoContent, resolve.StatusCode);

        var gone = await SendAsync(HttpMethod.Get, $"/api/moderation/cases/{caseId}", admin, null);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        var list = await GetPostsAsync(id, member);
        Assert.False(list[0].GetProperty("reportedByMe").GetBoolean());
    }

    [Fact]
    public async Task AbgelaufenerFall_WirdGeloescht()
    {
        var (creator, member, id) = await SetupAsync("mod-abgelaufen");
        var post = await CreatePostAsync(id, creator, "Alter Beitrag");

        var report = await ReportAsync(post, member, "Beleidigung", null);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // 29 Tage alt → bleibt
            var c = await db.ModerationCases.FindAsync(caseId);
            c!.LastReportAtUtc = DateTime.UtcNow.AddDays(-29);
            await db.SaveChangesAsync();
            await ModerationEndpoints.DeleteExpiredCasesAsync(db, DateTime.UtcNow);
            Assert.NotNull(await db.ModerationCases.FindAsync(caseId));

            // 31 Tage alt → wird gelöscht
            c.LastReportAtUtc = DateTime.UtcNow.AddDays(-31);
            await db.SaveChangesAsync();
            await ModerationEndpoints.DeleteExpiredCasesAsync(db, DateTime.UtcNow);
        }

        var gone = await SendAsync(HttpMethod.Get, $"/api/moderation/cases/{caseId}", await AdminAsync(), null);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task FallListe_ZeigtKennzahlenUndLoeschdatum()
    {
        var (creator, member, id) = await SetupAsync("mod-liste");
        var post = await CreatePostAsync(id, creator, "Beitrag");
        var report = await ReportAsync(post, member, "Beleidigung", null);
        var caseId = (await report.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("caseId").GetInt32();

        var response = await SendAsync(HttpMethod.Get, "/api/moderation/cases", await AdminAsync(), null);
        var entry = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Single(c => c.GetProperty("id").GetInt32() == caseId);

        Assert.Equal(1, entry.GetProperty("postCount").GetInt32());
        var last  = entry.GetProperty("lastReportAtUtc").GetDateTime();
        var until = entry.GetProperty("deleteAfterUtc").GetDateTime();
        Assert.Equal(30, (until - last).TotalDays, 3);
        Assert.EndsWith("Z", entry.GetProperty("lastReportAtUtc").GetString());
    }

    // Hilfsmethoden

    /// <summary>Ersteller (Seed-User), ein neues Mitglied mit Namen <paramref name="name"/> und ein Termin.</summary>
    private async Task<(string Creator, string Member, int AppointmentId)> SetupAsync(string name)
    {
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var member  = await RegisterAsync(name);

        var areas  = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        var areaId = areas.GetProperty("items")[0].GetProperty("id").GetInt32();
        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", creator,
            JsonContent.Create(new { title = $"{name}-Termin", date = "2030-07-01T18:00:00Z" }));
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        await SendAsync(HttpMethod.Post, $"/api/appointments/{id}/subscribe", member,
            JsonContent.Create(new { comment = (string?)null }));
        return (creator, member, id);
    }

    private async Task<HttpResponseMessage> ReportAsync(int postId, string token, string reason, string? comment) =>
        await SendAsync(HttpMethod.Post, $"/api/posts/{postId}/reports", token,
            JsonContent.Create(new { reason, comment }));

    private async Task<JsonElement> GetCaseAsync(int caseId)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/moderation/cases/{caseId}", await AdminAsync(), null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> CreatePostAsync(int appointmentId, string token, string text)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/posts", token,
            JsonContent.Create(new { text }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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

    private Task<string> AdminAsync() => LoginAsync("admin@climbconnect.at", "Admin1234!");

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
