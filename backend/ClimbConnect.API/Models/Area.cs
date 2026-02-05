namespace ClimbConnect.API.Models;

public class Area
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Location { get; set; } // z.B. "Linz", "Wels"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
