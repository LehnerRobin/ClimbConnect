using System.Net;
using System.Net.Http.Json;

namespace ClimbConnect.Tests;

/// <summary>Integrationstests für Register- und Login-Endpoints.</summary>
public class AuthTests : IClassFixture<ClimbConnectFactory>
{
    private readonly HttpClient _client;

    public AuthTests(ClimbConnectFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_GültigeDaten_Gibt200UndToken()
    {
        // Hinweis: "testuser" ist durch Seed-Daten vergeben → anderen Namen verwenden
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "neuerkletterer",
            email    = "neuer@example.com",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Token);
        Assert.Equal("neuerkletterer", body.Username);
        Assert.Equal("user", body.Role);
    }

    [Fact]
    public async Task Register_DoppelteEmail_Gibt409()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ersterperson",
            email    = "doppelt@example.com",
            password = "Test1234!"
        });

        // Zweite Registrierung mit gleicher E-Mail muss 409 zurückgeben
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "zweiteperson",
            email    = "doppelt@example.com",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DoppelterUsername_Gibt409()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "duplikat",
            email    = "duplikat1@example.com",
            password = "Test1234!"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "duplikat",
            email    = "duplikat2@example.com",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ZuKurzesPasswort_Gibt400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "kurzpassuser",
            email    = "kurzpass@example.com",
            password = "kurz"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_MitSeedUser_Gibt200UndToken()
    {
        // Seed-User ist immer vorhanden: user@climbconnect.at / User1234!
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "user@climbconnect.at",
            password = "User1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.Token);
    }

    [Fact]
    public async Task Login_FalschesPasswort_Gibt401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "user@climbconnect.at",
            password = "FalschesPasswort123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NichtExistierenderUser_Gibt401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "gibtnicht@example.com",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record AuthResponse(string Token, int Id, string Username, string Email, string Role);
}
