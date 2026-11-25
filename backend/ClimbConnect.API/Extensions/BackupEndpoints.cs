using System.IO.Compression;
using ClimbConnect.API.Data;
using ClimbConnect.API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClimbConnect.API.Extensions;

/// <summary>
/// Backup-Download für den Admin: Datenbank und hochgeladene Bilder als ZIP (#236).
/// </summary>
public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/admin/backup", async (AppDbContext db, UploadStorage storage, CancellationToken ct) =>
        {
            // Nur mit SQLite möglich (in den Tests läuft eine In-Memory-Datenbank)
            if (!db.Database.IsRelational())
                return Results.BadRequest(new { error = "Backup ist nur mit der SQLite-Datenbank möglich." });

            var tempDir = Directory.CreateTempSubdirectory("climbconnect-backup-");
            try
            {
                // 1. Konsistente Kopie der Datenbank über die SQLite-Backup-API.
                //    Einfach die Datei zu kopieren wäre unsicher, falls gerade jemand schreibt.
                var dbCopy = Path.Combine(tempDir.FullName, "ClimbConnect.db");
                await using (var source = new SqliteConnection(db.Database.GetConnectionString()))
                await using (var target = new SqliteConnection($"Data Source={dbCopy};Pooling=False"))
                {
                    await source.OpenAsync(ct);
                    await target.OpenAsync(ct);
                    source.BackupDatabase(target);
                }

                // 2. ZIP mit Datenbank und allen Bildern. Es landet in einer temporären Datei,
                //    die beim Schließen des Streams automatisch gelöscht wird.
                var zipPath = Path.Combine(Path.GetTempPath(), $"climbconnect-backup-{Guid.NewGuid():N}.zip");
                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(dbCopy, "ClimbConnect.db");
                    foreach (var file in Directory.EnumerateFiles(storage.UploadsPath))
                        zip.CreateEntryFromFile(file, $"uploads/{Path.GetFileName(file)}");
                }

                var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.None,
                                            bufferSize: 81920, FileOptions.DeleteOnClose);
                var name = $"climbconnect-backup-{TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTime.Vienna):yyyy-MM-dd-HHmm}.zip";
                return Results.File(stream, "application/zip", name);
            }
            finally
            {
                tempDir.Delete(recursive: true);
            }
        })
        .WithName("DownloadBackup")
        .WithTags("Admin")
        .RequireAuthorization("Admin");
    }
}
