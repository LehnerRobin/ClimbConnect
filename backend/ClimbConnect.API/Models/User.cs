namespace ClimbConnect.API.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";    // "user" | "admin"
    public string? Bio { get; set; }
    public string? PreferredGradeScale { get; set; }  // "french" | "uiaa" | "american"
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Progress> Progresses { get; set; } = [];
    public ICollection<Appointment> CreatedAppointments { get; set; } = [];
    public ICollection<AppointmentUser> AppointmentUsers { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
}
