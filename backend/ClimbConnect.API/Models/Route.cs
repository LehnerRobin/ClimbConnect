namespace ClimbConnect.API.Models;

public class Route
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Grade { get; set; }        // UIAA, z.B. "6b", "7a+"
    public string? Sector { get; set; }
    public int? LengthMeters { get; set; }
    public string? Style { get; set; }        // z.B. "Sport", "Trad"
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Area Area { get; set; } = null!;
    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
}
