namespace ClimbConnect.API.Models;

public class Ping
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
