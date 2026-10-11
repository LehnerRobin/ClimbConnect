using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>Integrationstests für die Terminliste eines Gebiets (Teilnehmerzahl und Beitritts-Status).</summary>
public class AppointmentTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public AppointmentTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAppointments_NachBeitritt_ZeigtTeilnehmerzahlUndStatus()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Teilnehmer-Test", date = "2030-06-01T18:00:00" }));
        var appointmentId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var join = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", token,
            JsonContent.Create(new { comment = (string?)null }));
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        // Eingeloggt: Teilnehmerzahl 1 und als beigetreten markiert
        var own = await FindAppointmentAsync(areaId, appointmentId, token);
        Assert.Equal(1, own.GetProperty("participantCount").GetInt32());
        Assert.True(own.GetProperty("isSubscribed").GetBoolean());

        // Ohne Login: gleiche Teilnehmerzahl, aber nicht beigetreten
        var anonymous = await FindAppointmentAsync(areaId, appointmentId, token: null);
        Assert.Equal(1, anonymous.GetProperty("participantCount").GetInt32());
        Assert.False(anonymous.GetProperty("isSubscribed").GetBoolean());
    }

    [Fact]
    public async Task Subscribe_ZweimalBeitreten_Gibt409()
    {
        var areaId = await GetFirstAreaIdAsync();
        var token  = await LoginAsync("user@climbconnect.at", "User1234!");

        var create = await SendAsync(HttpMethod.Post, $"/api/areas/{areaId}/appointments", token,
            JsonContent.Create(new { title = "Doppelt-Test", date = "2030-06-02T18:00:00" }));
        var appointmentId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var body = new { comment = (string?)null };
        await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", token, JsonContent.Create(body));
        var second = await SendAsync(HttpMethod.Post, $"/api/appointments/{appointmentId}/subscribe", token, JsonContent.Create(body));

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

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body     = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body!.Token;
    }

    private record AuthResponse(string Token);
}
