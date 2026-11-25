namespace ClimbConnect.API.Models;

/// <summary>
/// Fall für den Admin (#264): fasst alle Meldungen zu den Pinnwand-Beiträgen EINES Termins
/// zusammen. Hat absichtlich keinen Fremdschlüssel auf den Termin – der Fall muss das Löschen
/// des Termins überleben. Titel, Datum und Gebiet werden deshalb mitkopiert.
/// </summary>
public class ModerationCase
{
    /// <summary>Nach so vielen Tagen ohne neue Meldung wird der Fall automatisch gelöscht.</summary>
    public const int RetentionDays = 30;

    public int Id { get; set; }

    /// <summary>Id des Termins – nur als Wert gespeichert, ohne Fremdschlüssel.</summary>
    public int AppointmentId { get; set; }

    public string AppointmentTitle { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string? AreaName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Zeitpunkt der letzten Meldung – ab hier laufen die 30 Tage bis zur Löschung.</summary>
    public DateTime LastReportAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CasePost> Posts { get; set; } = [];
}
