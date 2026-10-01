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

    // Hilfsmethoden

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
