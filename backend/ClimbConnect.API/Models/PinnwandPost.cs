namespace ClimbConnect.API.Models;

/// <summary>
/// Beitrag auf der Pinnwand eines Termins (#245). Nur Teilnehmer und Ersteller sehen und
/// schreiben Beiträge. Wird der Termin gelöscht, werden die Beiträge mitgelöscht – es gibt
/// keine Kopie (Datenminimierung). Nur bei einer Meldung (#264) bleibt eine Kopie im Fall.
/// </summary>
public class PinnwandPost
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    /// <summary>Verfasser des Beitrags.</summary>
    public int UserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Appointment Appointment { get; set; } = null!;
    public User User { get; set; } = null!;
}
