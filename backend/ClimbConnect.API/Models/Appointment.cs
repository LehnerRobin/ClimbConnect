namespace ClimbConnect.API.Models;

public class Appointment
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? MeetingPoint { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Area Area { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<AppointmentUser> AppointmentUsers { get; set; } = [];
}
