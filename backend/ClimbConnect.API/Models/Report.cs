namespace ClimbConnect.API.Models;

/// <summary>
/// Sicherheitsmeldung zu einem Gebiet oder einer Route.
/// Status wird vom Admin verwaltet, Severity wird vom User gesetzt.
/// </summary>
public class Report
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int? AreaId { get; set; }
    public int? RouteId { get; set; }

    public string Text { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }

    /// <summary>Schweregrad: "Low", "Medium" oder "High".</summary>
    public string Severity { get; set; } = "Low";

    /// <summary>Bearbeitungsstatus durch Admin: "Open" oder "Resolved".</summary>
    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Area? Area { get; set; }
    public Route? Route { get; set; }
}
