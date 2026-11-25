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
        const string prefix = "/uploads/";
        if (!url.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var fileName = url[prefix.Length..];
        // Keine Unterordner und kein "..": nur ein einfacher Dateiname ist gültig
        if (fileName.Length == 0 || fileName != Path.GetFileName(fileName) || fileName.Contains(".."))
            return false;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;

        return File.Exists(Path.Combine(UploadsPath, fileName));
    }
}
