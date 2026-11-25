namespace ClimbConnect.API.Models;

/// <summary>
/// Klettertermin in einem Gebiet. User können beitreten (subscribe).
/// </summary>
public class Appointment
{
    /// <summary>Standard und kleinster erlaubter Wert für das Minimum: allein klettern braucht keinen Termin.</summary>
    public const int DefaultMinParticipants = 2;

    public int Id { get; set; }

    public int AreaId { get; set; }
    public int CreatedByUserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? MeetingPoint { get; set; }
    public string? Description { get; set; }

    /// <summary>Mindestanzahl Teilnehmer, mindestens <see cref="DefaultMinParticipants"/>.</summary>
    public int? MinParticipants { get; set; }

    /// <summary>Maximalanzahl Teilnehmer; null = "Open End". Subscribe wird ab diesem Limit gesperrt.</summary>
    public int? MaxParticipants { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Area Area { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<AppointmentUser> AppointmentUsers { get; set; } = [];
}
