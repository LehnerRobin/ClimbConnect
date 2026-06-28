namespace ClimbConnect.API.Extensions;

/// <summary>Endpoint für den Bild-Upload (JPG, PNG, WebP, GIF, max 5 MB).</summary>
public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this WebApplication app)
    {
        app.MapPost("/api/upload", async (IFormFile file, HttpContext ctx) =>
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType))
                return Results.BadRequest(new { error = "Nur JPG, PNG, WebP und GIF sind erlaubt." });

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest(new { error = "Maximale Dateigröße ist 5 MB." });

            var ext      = Path.GetExtension(file.FileName).ToLower();
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
}
