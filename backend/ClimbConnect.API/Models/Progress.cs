namespace ClimbConnect.API.Models;

public class Progress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RouteId { get; set; }
    public string Status { get; set; } = "Attempted";   // "Attempted" | "Completed" | "Flashed" | "Onsight"
    public int Attempts { get; set; } = 0;
    public string? Notes { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Route Route { get; set; } = null!;
}
