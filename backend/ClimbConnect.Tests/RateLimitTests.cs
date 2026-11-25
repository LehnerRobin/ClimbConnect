using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ClimbConnect.Tests;

/// <summary>Factory mit kleinem Limit, damit der Test schnell an die Grenze kommt.</summary>
public class LowRateLimitFactory : ClimbConnectFactory
{
    protected override int AuthPermitLimit => 3;
}

/// <summary>
/// Integrationstests für den Schutz gegen Passwort-Raten (#233):
/// nach zu vielen Login-Versuchen pro Minute kommt 429.
/// </summary>
public class RateLimitTests : IClassFixture<LowRateLimitFactory>
{
    private readonly HttpClient _client;

    public RateLimitTests(LowRateLimitFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ZuVieleVersuche_Gibt429MitMeldung()
    {
        var falsch = new { email = "user@climbconnect.at", password = "falsch123" };

        // Die ersten 3 Versuche werden normal geprüft (falsches Passwort → 401)
        for (var i = 0; i < 3; i++)
        {
            var ok = await _client.PostAsJsonAsync("/api/auth/login", falsch);
            Assert.Equal(HttpStatusCode.Unauthorized, ok.StatusCode);
        }

        // Der 4. Versuch innerhalb derselben Minute wird abgelehnt
        var blocked = await _client.PostAsJsonAsync("/api/auth/login", falsch);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Zu viele Versuche", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AndereEndpoints_SindNichtBegrenzt()
    {
        for (var i = 0; i < 10; i++)
        {
            var response = await _client.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
