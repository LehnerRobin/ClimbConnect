namespace ClimbConnect.API.Models;

/// <summary>
/// Repräsentiert ein Klettergebiet in OÖ (oberste Ebene: Area → Sector → Route).
/// </summary>
public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Ort des Gebiets, z.B. "Hinterstoder", "Bad Ischl".</summary>
    public string? Location { get; set; }

    public string? Description { get; set; }

    /// <summary>URL zu einem Bild des Gebiets (z.B. /uploads/abc123.jpg).</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Breitengrad des Gebiets für die Kartenanzeige (z.B. 47.9022).</summary>
    public double? Latitude { get; set; }

    /// <summary>Längengrad des Gebiets für die Kartenanzeige (z.B. 14.1272).</summary>
    public double? Longitude { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Sector> Sectors { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
}
