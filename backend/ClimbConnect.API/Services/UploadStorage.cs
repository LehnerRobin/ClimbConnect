using System.Linq.Expressions;
using ClimbConnect.API.Models;
using ClimbConnect.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Services;

/// <summary>
/// Kennt den Ordner, in dem hochgeladene Bilder gespeichert werden.
/// Standard ist "uploads" neben der Anwendung (/app/uploads im Container).
/// Über Storage:UploadsPath (Umgebungsvariable Storage__UploadsPath) lässt sich
/// der Ordner verlegen, z.B. in ein Volume auf Railway.
/// </summary>
public class UploadStorage
{
    /// <summary>Absoluter Pfad zum Upload-Ordner.</summary>
    public string UploadsPath { get; }

    /// <summary>
    /// Liest den Pfad aus der Konfiguration und legt den Ordner an, falls er fehlt.
    /// </summary>
    public UploadStorage(IConfiguration config)
    {
        var configured = config["Storage:UploadsPath"];
        UploadsPath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "uploads")
            : Path.GetFullPath(configured);
        Directory.CreateDirectory(UploadsPath);
    }

    /// <summary>
    /// Prüft, ob ein Foto-Link auf ein hier hochgeladenes Bild zeigt.
    /// Erlaubt ist nur "/uploads/{dateiname}" ohne Unterordner, und die Datei muss
    /// im Upload-Ordner existieren. Fremde URLs (z.B. https://...) werden abgelehnt,
    /// damit kein fremder Server die IP-Adressen der Besucher mitprotokollieren kann.
    /// </summary>
    /// <param name="url">Der vom Client geschickte Foto-Link.</param>
    /// <returns>true, wenn der Link auf eine existierende eigene Datei zeigt.</returns>
    public bool IsOwnUpload(string url)
    {
        var fileName = ToFileName(url);
        return fileName is not null && File.Exists(Path.Combine(UploadsPath, fileName));
    }

    /// <summary>
    /// Löscht die Bilddateien zu den angegebenen Foto-Links – aber nur, wenn kein Feedback,
    /// kein Report und kein Gebiet mehr darauf zeigt (ein Link kann mehrfach verwendet werden).
    /// Muss NACH dem Speichern der Löschung in der Datenbank aufgerufen werden.
    /// Ein Fehler beim Löschen einer Datei wird nur geloggt: Der Eintrag ist dann trotzdem weg,
    /// es bleibt höchstens eine verwaiste Datei übrig.
    /// </summary>
    /// <param name="db">Datenbank, um zu prüfen, ob der Link noch verwendet wird.</param>
    /// <param name="urls">Foto-Links der gelöschten Einträge; null und fremde Links werden ignoriert.</param>
    /// <param name="logger">Für Fehlermeldungen beim Löschen.</param>
    /// <returns>Anzahl der gelöschten Dateien.</returns>
    public async Task<int> DeleteUnusedAsync(AppDbContext db, IEnumerable<string?> urls, ILogger logger)
    {
        var deleted = 0;
        foreach (var url in urls.OfType<string>().Distinct())
        {
            var fileName = ToFileName(url);
            if (fileName is null) continue;

            var stillUsed = await db.Feedback.AnyAsync(f => f.PhotoUrl == url)
                         || await db.Reports.AnyAsync(r => r.PhotoUrl == url)
                         || await db.Areas.AnyAsync(a => a.ImageUrl == url);
            if (stillUsed) continue;

            try
            {
                var path = Path.Combine(UploadsPath, fileName);
                if (!File.Exists(path)) continue;
                File.Delete(path);
                deleted++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Bild {File} konnte nicht gelöscht werden.", fileName);
            }
        }
        return deleted;
    }

    /// <summary>
    /// Sammelt die Foto-Links aller Feedback-Einträge und Reports, die gleich gelöscht werden –
    /// VOR dem Löschen aufrufen, danach sind die Einträge weg.
    /// </summary>
    /// <param name="db">Datenbank.</param>
    /// <param name="feedback">Welche Feedback-Einträge betroffen sind.</param>
    /// <param name="reports">Welche Reports betroffen sind.</param>
    public static async Task<List<string?>> PhotoUrlsAsync(AppDbContext db,
        Expression<Func<Feedback, bool>> feedback, Expression<Func<Report, bool>> reports)
    {
        var urls = await db.Feedback.Where(feedback).Where(f => f.PhotoUrl != null).Select(f => f.PhotoUrl).ToListAsync();
        urls.AddRange(await db.Reports.Where(reports).Where(r => r.PhotoUrl != null).Select(r => r.PhotoUrl).ToListAsync());
        return urls;
    }

    /// <summary>
    /// Macht aus "/uploads/{dateiname}" den reinen Dateinamen – oder null, wenn der Link nicht
    /// auf den Upload-Ordner zeigt. Keine Unterordner und kein "..", damit niemand über einen
    /// präparierten Link Dateien außerhalb des Ordners erreicht.
    /// </summary>
    private static string? ToFileName(string url)
    {
        const string prefix = "/uploads/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) return null;

        var fileName = url[prefix.Length..];
        if (fileName.Length == 0 || fileName != Path.GetFileName(fileName) || fileName.Contains(".."))
            return null;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return null;
        return fileName;
    }
}
