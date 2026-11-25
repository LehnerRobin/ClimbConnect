using ClimbConnect.API.Data;
using ClimbConnect.API.Extensions;

namespace ClimbConnect.API.Services;

/// <summary>
/// Löscht einmal täglich alle Meldungs-Fälle, deren letzte Meldung mehr als 30 Tage her ist (#264).
/// So gilt die Speicherbegrenzung auch dann, wenn der Admin die Fälle nie öffnet.
/// </summary>
public class ModerationCleanupService(IServiceScopeFactory scopeFactory, ILogger<ModerationCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    /// <summary>Läuft beim Start und danach alle 24 Stunden, bis die App beendet wird.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kurz warten, damit beim Start zuerst die Migrationen laufen
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ContinueWith(_ => { });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // DbContext ist "scoped" – pro Durchlauf einen eigenen Scope anlegen
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var deleted = await ModerationEndpoints.DeleteExpiredCasesAsync(db, DateTime.UtcNow);
                if (deleted > 0)
                    logger.LogInformation("{Count} abgelaufene Meldungs-Fälle gelöscht.", deleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ein Fehler soll die App nicht beenden – beim nächsten Durchlauf erneut versuchen
                logger.LogError(ex, "Aufräumen der Meldungs-Fälle fehlgeschlagen.");
            }

            await Task.Delay(Interval, stoppingToken).ContinueWith(_ => { });
        }
    }
}
