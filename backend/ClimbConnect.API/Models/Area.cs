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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Sector> Sectors { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
}
