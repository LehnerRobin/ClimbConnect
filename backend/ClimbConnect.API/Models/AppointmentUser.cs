namespace ClimbConnect.API.Models;

public class AppointmentUser
{
    public int AppointmentId { get; set; }
    public int UserId { get; set; }
    public string? Comment { get; set; }
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public Appointment Appointment { get; set; } = null!;
    public User User { get; set; } = null!;
}
