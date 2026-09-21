namespace ClimbConnect.API.Models;

/// <summary>
/// Repräsentiert einen Sektor innerhalb eines Klettergebiets (Area → Sector → Route).
/// </summary>
public class Sector
{
    public int Id { get; set; }

    /// <summary>Fremdschlüssel zum übergeordneten Gebiet.</summary>
    public int AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Area Area { get; set; } = null!;
    public ICollection<Route> Routes { get; set; } = [];
}
