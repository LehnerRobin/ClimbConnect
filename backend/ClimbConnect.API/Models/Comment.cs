namespace ClimbConnect.API.Models;

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? AreaId { get; set; }
    public int? RouteId { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>Optionaler Pfad zum hochgeladenen Foto, z.B. "/uploads/abc123.jpg".</summary>
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Area? Area { get; set; }
    public Route? Route { get; set; }
}
