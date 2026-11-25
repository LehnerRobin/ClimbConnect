namespace ClimbConnect.API.Models;

/// <summary>
/// Kopie eines Pinnwand-Beitrags in einem Fall (#264). Bleibt erhalten, auch wenn das Original
/// oder der Termin gelöscht wird – sonst könnte jemand den Beweis einfach wegklicken.
/// Deshalb kein Fremdschlüssel auf den Beitrag und den Verfasser; der Name wird mitkopiert.
/// </summary>
public class CasePost
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    /// <summary>Id des Original-Beitrags – nur als Wert gespeichert, ohne Fremdschlüssel.</summary>
    public int OriginalPostId { get; set; }

    public int AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>true, wenn der Verfasser das Original auf der Pinnwand gelöscht hat.</summary>
    public bool DeletedByAuthor { get; set; }

    // Navigation
    public ModerationCase Case { get; set; } = null!;
    public ICollection<PostReport> Reports { get; set; } = [];
}
