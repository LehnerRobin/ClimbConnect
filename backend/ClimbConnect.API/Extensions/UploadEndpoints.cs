namespace ClimbConnect.API.Extensions;

/// <summary>Endpoint für den Bild-Upload (JPG, PNG, WebP, GIF, max 5 MB).</summary>
public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/upload", async (IFormFile file, HttpContext ctx) =>
        {
            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "Maximale Dateigröße ist 5 MB." });

            // Dateityp am Inhalt erkennen – Content-Type und Dateiname kommen vom Client
            // und lassen sich beliebig fälschen.
            var header = new byte[12];
            await using (var input = file.OpenReadStream())
            {
                var read = await input.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
                if (read < header.Length)
                    return Results.BadRequest(new { error = "Nur JPG, PNG, WebP und GIF sind erlaubt." });
            }

            var ext = DetectImageExtension(header);
            if (ext is null)
                return Results.BadRequest(new { error = "Nur JPG, PNG, WebP und GIF sind erlaubt." });

            var fileName = $"{Guid.NewGuid()}{ext}";
            var savePath = Path.Combine(
                AppDomain.CurrentDomain.GetData("DataDirectory") as string
                    ?? AppDomain.CurrentDomain.BaseDirectory,
                "uploads", fileName);

            await using var stream = File.Create(savePath);
            await file.CopyToAsync(stream);

            return Results.Ok(new { url = $"/uploads/{fileName}" });
        })
        .WithName("UploadImage")
        .WithTags("Upload")
        .RequireAuthorization("User")
        .DisableAntiforgery();
    }

    /// <summary>
    /// Erkennt das Bildformat an den ersten Bytes der Datei (Signatur) und gibt die
    /// passende Dateiendung zurück – oder null, wenn es kein erlaubtes Bild ist.
    /// </summary>
    private static string? DetectImageExtension(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith<byte>([0xFF, 0xD8, 0xFF]))                                     return ".jpg";
        if (header.StartsWith<byte>([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))       return ".png";
        if (header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8))                  return ".gif";
        if (header.StartsWith("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8))       return ".webp";
        return null;
    }
}
