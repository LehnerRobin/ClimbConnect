namespace ClimbConnect.API.Models;

public class Comment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? AreaId { get; set; }
    public int? RouteId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Area? Area { get; set; }
    public Route? Route { get; set; }
}
