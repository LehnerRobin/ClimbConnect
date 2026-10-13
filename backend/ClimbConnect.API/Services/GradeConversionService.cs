namespace ClimbConnect.API.Services;

/// <summary>
/// Konvertiert Klettergrade zwischen den Skalen Französisch (intern), UIAA und Amerikanisch.
/// Intern wird immer die französische Skala gespeichert.
/// </summary>
public static class GradeConversionService
{
    /// <summary>Ein Grad in allen drei Skalen.</summary>
    public record Grade(string French, string Uiaa, string American);

    /// <summary>
    /// Die einzige Grad-Tabelle der Anwendung, sortiert von leicht nach schwer.
    /// Die Position in der Liste ist der Rang (für Vergleiche, Durchschnitt und Statistik).
    /// Das Frontend bezieht die Liste über GET /api/grades.
    /// Quelle: übliche Umrechnungstabellen für Sportklettern; die Skalen sind nicht 1:1
    /// umrechenbar, daher kommen manche UIAA- und US-Grade mehrfach vor.
    /// </summary>
    private static readonly List<Grade> Grades =
    [
        new("3a",  "III-",  "5.2"),
        new("3b",  "III",   "5.3"),
        new("3c",  "III+",  "5.4"),
        new("4a",  "IV-",   "5.5"),
        new("4b",  "IV",    "5.6"),
        new("4c",  "IV+",   "5.6"),
        new("5a",  "V-",    "5.7"),
        new("5a+", "V-",    "5.7"),
        new("5b",  "V",     "5.8"),
        new("5b+", "V",     "5.8"),
        new("5c",  "V+",    "5.9"),
        new("5c+", "VI-",   "5.9"),
        new("6a",  "VI",    "5.10a"),
        new("6a+", "VI+",   "5.10b"),
        new("6b",  "VII-",  "5.10c"),
        new("6b+", "VII",   "5.10d"),
        new("6c",  "VII+",  "5.11a"),
        new("6c+", "VII+",  "5.11b"),
        new("7a",  "VIII-", "5.11c"),
        new("7a+", "VIII",  "5.11d"),
        new("7b",  "VIII+", "5.12a"),
        new("7b+", "IX-",   "5.12b"),
        new("7c",  "IX",    "5.12c"),
        new("7c+", "IX+",   "5.12d"),
        new("8a",  "X-",    "5.13a"),
        new("8a+", "X",     "5.13b"),
        new("8b",  "X+",    "5.13c"),
        new("8b+", "XI-",   "5.13d"),
        new("8c",  "XI",    "5.14a"),
        new("8c+", "XI+",   "5.14b"),
        new("9a",  "XII-",  "5.15a"),
        new("9a+", "XII",   "5.15b"),
        new("9b",  "XII+",  "5.15c"),
        new("9b+", "XII+",  "5.15d"),
        new("9c",  "XIII-", "5.16a"),
    ];

    /// <summary>Schneller Zugriff: französischer Grad → Rang (Index in <see cref="Grades"/>).</summary>
    private static readonly Dictionary<string, int> RankByFrench =
        Grades.Select((g, i) => (g.French, i)).ToDictionary(x => x.French, x => x.i);

    /// <summary>Alle Grade in allen Skalen, sortiert von leicht nach schwer.</summary>
    public static IReadOnlyList<Grade> GetGrades() => Grades;

    /// <summary>
    /// Gibt die komplette sortierte Liste aller französischen Grade zurück (für Rang → Grad Konvertierung).
    /// </summary>
    public static List<string> GetAllGrades() => Grades.Select(g => g.French).ToList();

    /// <summary>Bringt einen Grad in die gespeicherte Form (klein, ohne Leerzeichen).</summary>
    public static string Normalize(string grade) => grade.Trim().ToLowerInvariant();

    /// <summary>Prüft, ob der Grad in der französischen Grad-Tabelle vorkommt.</summary>
    public static bool IsValid(string? frenchGrade) =>
        !string.IsNullOrWhiteSpace(frenchGrade) && RankByFrench.ContainsKey(Normalize(frenchGrade));

    /// <summary>
    /// Gibt den numerischen Rang eines Grads zurück — höher = schwerer.
    /// Gibt -1 zurück wenn der Grad unbekannt ist.
    /// </summary>
    public static int Rank(string? frenchGrade)
    {
        if (string.IsNullOrWhiteSpace(frenchGrade)) return -1;
        return RankByFrench.TryGetValue(Normalize(frenchGrade), out var rank) ? rank : -1;
    }

    /// <summary>
    /// Konvertiert einen französischen Grad in die gewünschte Skala.
    /// </summary>
    /// <param name="frenchGrade">Grad in der französischen Skala, z.B. "6b".</param>
    /// <param name="scale">Zielskala: "french", "uiaa" oder "american".</param>
    /// <returns>Konvertierter Grad oder null wenn unbekannt.</returns>
    public static string? Convert(string? frenchGrade, string scale = "french")
    {
        if (string.IsNullOrWhiteSpace(frenchGrade)) return null;

        var rank = Rank(frenchGrade);
        if (rank < 0) return scale.ToLower() is "uiaa" or "american" ? null : frenchGrade;

        var grade = Grades[rank];
        return scale.ToLower() switch
        {
            "uiaa"     => grade.Uiaa,
            "american" => grade.American,
            _          => grade.French
        };
    }
}
