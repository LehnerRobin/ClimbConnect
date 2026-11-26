using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClimbConnect.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimbConnect.Tests;

/// <summary>Integrationstests für die Terminliste eines Gebiets (Teilnehmerzahl und Beitritts-Status).</summary>
public class AppointmentTests : IClassFixture<ClimbConnectFactory>
{
    private readonly ClimbConnectFactory _factory;
    private readonly HttpClient _client;

    public AppointmentTests(ClimbConnectFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task GetAppointments_NachBeitritt_ZeigtTeilnehmerzahlUndStatus()
    {
        var areaId  = await GetFirstAreaIdAsync();
        var creator = await LoginAsync("user@climbconnect.at", "User1234!");
        var other   = await RegisterAsync("beitritt-test", "beitritt-test@example.com", "Test1234!");

        var appointmentId = await CreateAsync(areaId, creator, "Teilnehmer-Test", "2030-06-01T18:00:00");

        var join = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", other,
            JsonContent.Create(new { comment = (string?)null }));
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        // Eingeloggt: Ersteller + Beigetretener = 2, als beigetreten markiert
        var own = await FindAppointmentAsync(areaId, appointmentId, other);
        Assert.Equal(2, own.GetProperty("participantCount").GetInt32());
        Assert.True(own.GetProperty("isSubscribed").GetBoolean());
        Assert.False(own.GetProperty("isCreator").GetBoolean());

        // Ohne Login: gleiche Teilnehmerzahl, aber nicht beigetreten
        var anonymous = await FindAppointmentAsync(areaId, appointmentId, token: null);
        Assert.Equal(2, anonymous.GetProperty("participantCount").GetInt32());
        Assert.False(anonymous.GetProperty("isSubscribed").GetBoolean());
    }

    [Fact]
    public async Task Create_ErstellerIstAutomatischDabei()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Send-Dudes-Test", date = "2030-06-07T18:00:00Z" }));
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("participantCount").GetInt32());

        var listed = await FindAppointmentAsync(areaId, body.GetProperty("id").GetInt32(), token);
        Assert.Equal(1, listed.GetProperty("participantCount").GetInt32());
        Assert.True(listed.GetProperty("isSubscribed").GetBoolean());
        Assert.True(listed.GetProperty("isCreator").GetBoolean());
    }

    [Fact]
    public async Task Unsubscribe_AlsErsteller_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var id     = await CreateAsync(areaId, token, "Nicht austreten", "2030-06-08T18:00:00Z");

        var response = await SendAsync(HttpMethod.Delete, $"/api/appointments/{id}/subscribe", token, null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_OhneMinimum_SetztStandard2UndOpenEnd()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Standard-Test", date = "2030-06-05T18:00:00Z" }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var body = await create.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("minParticipants").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("maxParticipants").ValueKind);
    }

    [Theory]
    [InlineData(1, null)]   // Minimum unter 2
    [InlineData(null, 1)]   // Maximum unter 2
    [InlineData(null, 0)]
    public async Task Create_MinimumOderMaximumUnter2_Gibt400(int? min, int? max)
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var response = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Zu klein", date = "2030-06-05T18:00:00Z",
                                     minParticipants = min, maxParticipants = max }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_MinimumUnter2_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Update-Min", date = "2030-06-06T18:00:00Z" }));
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var response = await SendAsync(HttpMethod.Put, $"/api/appointments/{id}", token,
            JsonContent.Create(new { title = "Update-Min", date = "2030-06-06T18:00:00Z", minParticipants = 1 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Subscribe_ZweimalBeitreten_Gibt409()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var other  = await RegisterAsync("doppelt-test", "doppelt-test@example.com", "Test1234!");
        var appointmentId = await CreateAsync(areaId, token, "Doppelt-Test", "2030-06-02T18:00:00");

        var body = new { comment = (string?)null };
        await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", other, JsonContent.Create(body));
        var second = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", other, JsonContent.Create(body));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_InDerVergangenheit_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Vergangen", date = "2020-01-01T18:00:00Z" }));

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task Update_MinGroesserAlsMax_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var id     = await CreateAsync(areaId, token, "Update-Test", "2030-06-03T18:00:00Z");

        var update = await SendAsync(HttpMethod.Put, $"/api/appointments/{id}", token,
            JsonContent.Create(new { title = "Update-Test", date = "2030-06-03T18:00:00Z", minParticipants = 5, maxParticipants = 2 }));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task Update_DatumInDieVergangenheit_Gibt400()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var id     = await CreateAsync(areaId, token, "Verschieben", "2030-06-04T18:00:00Z");

        var update = await SendAsync(HttpMethod.Put, $"/api/appointments/{id}", token,
            JsonContent.Create(new { title = "Verschieben", date = "2020-06-04T18:00:00Z" }));

        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
    }

    [Fact]
    public async Task Create_MitZeitzone_WirdInUtcGespeichertUndMitZAusgegeben()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        // 18:00 Wiener Sommerzeit (+02:00) entspricht 16:00 UTC
        var id    = await CreateAsync(areaId, token, "Zeitzone", "2030-06-05T18:00:00+02:00");
        var saved = await FindAppointmentAsync(areaId, id, token);

        Assert.Equal("2030-06-05T16:00:00Z", saved.GetProperty("date").GetString());
    }

    // Zeitraum-Abfrage für den Terminkalender (#298)

    [Fact]
    public async Task GetInRange_LiefertNurTermineImZeitraum_SortiertMitGebiet()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var later   = await CreateAsync(areaId, token, "Zeitraum spät",    "2031-03-20T10:00:00Z");
        var earlier = await CreateAsync(areaId, token, "Zeitraum früh",    "2031-03-10T10:00:00Z");
        var atEnd   = await CreateAsync(areaId, token, "Zeitraum am Ende", "2031-04-01T00:00:00Z");
        var before  = await CreateAsync(areaId, token, "Zeitraum davor",   "2031-02-28T23:59:00Z");

        var response = await SendAsync(HttpMethod.Get,
            "/api/appointments?from=2031-03-01T00:00:00Z&to=2031-04-01T00:00:00Z", token, content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        var ids  = list.Select(a => a.GetProperty("id").GetInt32()).ToList();

        // "to" zählt nicht mehr dazu, "from" schon
        Assert.Equal(new[] { earlier, later }, ids.Where(id => id == earlier || id == later));
        Assert.DoesNotContain(atEnd, ids);
        Assert.DoesNotContain(before, ids);

        // Gleiche Felder wie /upcoming: Gebiet, Teilnehmerzahl, eigener Status
        var first = list.Single(a => a.GetProperty("id").GetInt32() == earlier);
        Assert.False(string.IsNullOrEmpty(first.GetProperty("areaName").GetString()));
        Assert.Equal(1, first.GetProperty("participantCount").GetInt32());
        Assert.True(first.GetProperty("isSubscribed").GetBoolean());
        Assert.True(first.GetProperty("isCreator").GetBoolean());
    }

    [Fact]
    public async Task GetInRange_EnthaeltAuchVergangeneTermine_OhneLogin()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");
        var id     = await CreateAsync(areaId, token, "Vergangen", "2030-09-01T10:00:00Z");

        // Über die API kann man keinen Termin in der Vergangenheit anlegen → direkt in der DB zurückdatieren
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var appointment = await db.Appointments.SingleAsync(a => a.Id == id);
            appointment.Date = new DateTime(2020, 5, 15, 10, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }

        var response = await SendAsync(HttpMethod.Get,
            "/api/appointments?from=2020-05-01T00:00:00Z&to=2020-06-01T00:00:00Z", token: null, content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list  = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        var found = list.Single(a => a.GetProperty("id").GetInt32() == id);
        Assert.False(found.GetProperty("isSubscribed").GetBoolean());
    }

    [Theory]
    [InlineData("/api/appointments")]                                                            // beides fehlt
    [InlineData("/api/appointments?from=2031-03-01T00:00:00Z")]                                  // "to" fehlt
    [InlineData("/api/appointments?from=2031-03-01T00:00:00Z&to=2031-03-01T00:00:00Z")]          // to == from
    [InlineData("/api/appointments?from=2031-04-01T00:00:00Z&to=2031-03-01T00:00:00Z")]          // to < from
    [InlineData("/api/appointments?from=2031-01-01T00:00:00Z&to=2031-12-31T00:00:00Z")]          // zu lang
    [InlineData("/api/appointments?from=kein-datum&to=2031-03-01T00:00:00Z")]                    // kein Datum
    public async Task GetInRange_UngueltigerZeitraum_Gibt400(string url)
    {
        var response = await SendAsync(HttpMethod.Get, url, token: null, content: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Hilfsmethoden

    private async Task<int> CreateAsync(int areaId, string token, string title, string date)
    {
        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title, date }));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        return (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> FindAppointmentAsync(int areaId, int appointmentId, string? token)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/areas/{areaId}/appointments", token, content: null);
        var list     = await response.Content.ReadFromJsonAsync<JsonElement>();
        return list.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == appointmentId);
    }

    /// <summary>Schickt eine Anfrage (optional mit Bearer-Token), ohne den gemeinsamen Client zu verändern.</summary>
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? token, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private async Task<int> GetFirstAreaIdAsync()
    {
        var areas = await _client.GetFromJsonAsync<JsonElement>("/api/areas");
        return areas.GetProperty("items")[0].GetProperty("id").GetInt32();
    }

    private async Task<string> RegisterAsync(string username, string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { username, email, password });
        var body     = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private record AuthResponse(string Token);
}
