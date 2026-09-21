namespace ClimbConnect.API.Models;

/// <summary>
/// Repräsentiert eine Kletterroute innerhalb eines Sektors (Area → Sector → Route).
/// Der Grad wird intern immer in der französischen Skala gespeichert.
/// </summary>
public class Route
{
    public int Id { get; set; }

    /// <summary>Fremdschlüssel zum übergeordneten Sektor.</summary>
    public int SectorId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Grad in der französischen Skala, z.B. "6b", "7a+".</summary>
    public string? Grade { get; set; }

    public int? LengthMeters { get; set; }

    /// <summary>Klettersti, z.B. "Sport", "Trad".</summary>
    public string? Style { get; set; }

    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Sector Sector { get; set; } = null!;
    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
}
