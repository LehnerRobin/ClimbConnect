namespace ClimbConnect.API.Models;

/// <summary>
/// Speichert den Fortschritt eines Users an einer Route.
/// Status: Projekt, Rotpunkt, Flash oder Onsight.
/// </summary>
public class Progress
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public int RouteId { get; set; }

    /// <summary>
    /// Begehungsstatus: "Projekt", "Rotpunkt", "Flash" oder "Onsight".
    /// </summary>
    public string Status { get; set; } = "Projekt";

    /// <summary>
    /// Begehungsart: "Toprope" oder "Vorstieg".
    /// </summary>
    public string ClimbingStyle { get; set; } = "Vorstieg";

    /// <summary>Anzahl der Versuche an dieser Route.</summary>
    public int Attempts { get; set; } = 0;

    public string? Notes { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>
    /// Subjektiver Grad des Users in der französischen Skala, z.B. "6b".
    /// Fließt in den Community-Grad der Route ein.
    /// </summary>
    public string? SubjectiveGrade { get; set; }

    /// <summary>Kurzer Kommentar zur subjektiven Gradbewertung.</summary>
    public string? SubjectiveGradeComment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Route Route { get; set; } = null!;
}
