namespace ClimbConnect.API.Models;

/// <summary>
/// Meldung eines Pinnwand-Beitrags (#264): wer, warum, wann. Hängt an der Kopie im Fall,
/// nicht am Original – so bleibt sie erhalten, wenn das Original gelöscht wird.
/// Pro Kopie darf jede Person nur einmal melden (Unique-Index auf CasePostId + ReporterId).
/// </summary>
public class PostReport
{
    /// <summary>Erlaubte Meldegründe – gleiche Werte wie im Frontend (pinnwand.model.ts).</summary>
    public static readonly string[] Reasons = ["Beleidigung", "Belaestigung", "UnangemessenerInhalt", "Sonstiges"];

    /// <summary>Bei diesem Grund ist ein Kommentar Pflicht.</summary>
    public const string OtherReason = "Sonstiges";

    public int Id { get; set; }

    public int CasePostId { get; set; }

    public int ReporterId { get; set; }
    public string ReporterUsername { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public CasePost CasePost { get; set; } = null!;
}
