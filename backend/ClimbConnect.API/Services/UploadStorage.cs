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
}
