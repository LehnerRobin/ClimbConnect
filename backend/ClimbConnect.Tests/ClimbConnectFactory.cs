using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClimbConnect.API.Data;

namespace ClimbConnect.Tests;

/// <summary>
/// WebApplicationFactory für Integrationstests — ersetzt SQLite durch eine
/// In-Memory-Datenbank, damit kein echtes Dateisystem benötigt wird.
/// Jede Factory-Instanz (= 1 pro Testklasse) bekommt eine eigene Datenbank.
/// </summary>
public class ClimbConnectFactory : WebApplicationFactory<Program>
{
    // Einmal pro Instanz evaluiert → stabiler DB-Name für alle Tests in der Klasse
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Alle DbContext-Options-Registrierungen entfernen
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // In-Memory-Datenbank mit fixem Namen für diese Factory-Instanz
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });

        builder.UseEnvironment("Testing");
    }
}
