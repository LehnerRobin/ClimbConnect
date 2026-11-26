namespace ClimbConnect.API.Models;

/// <summary>
/// Feedback an das Team (#244) – ersetzt die früheren öffentlichen Kommentare. Nur der Admin
/// sieht die Einträge. Optional mit Foto und mit Bezug zu einem Gebiet oder einer Route,
/// wenn der User das Feedback auf dieser Seite geschrieben hat.
/// </summary>
public class Feedback
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Optionaler Bezug: Gebiet, auf dessen Seite das Feedback geschrieben wurde.</summary>
    public int? AreaId { get; set; }

    /// <summary>Optionaler Bezug: Route, auf deren Seite das Feedback geschrieben wurde.</summary>
    public int? RouteId { get; set; }

    public string Text { get; set; } = string.Empty;

    /// <summary>Optionaler Pfad zum hochgeladenen Foto, z.B. "/uploads/abc123.jpg".</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>Hat der Admin das Feedback schon gelesen?</summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Area? Area { get; set; }
    public Route? Route { get; set; }
}
