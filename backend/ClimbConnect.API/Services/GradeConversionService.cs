namespace ClimbConnect.API.Services;

/// <summary>
/// Konvertiert Klettergrade zwischen den Skalen Französisch (intern), UIAA und Amerikanisch.
/// Intern wird immer die französische Skala gespeichert.
/// </summary>
public static class GradeConversionService
{
    /// <summary>
    /// Zuordnungstabelle: Französisch → (UIAA, Amerikanisch).
    /// Quelle: Standard-Konversionstabelle für Sportklettern.
    /// </summary>
    private static readonly Dictionary<string, (string Uiaa, string American)> GradeMap = new()
    {
        { "4",   ("IV",    "5.6")  },
        { "4+",  ("IV+",   "5.7")  },
        { "5",   ("V",     "5.8")  },
        { "5+",  ("V+",    "5.9")  },
        { "6a",  ("VI",    "5.10a") },
        { "6a+", ("VI+",   "5.10b") },
        { "6b",  ("VII-",  "5.10c") },
        { "6b+", ("VII",   "5.10d") },
        { "6c",  ("VII+",  "5.11a") },
        { "6c+", ("VII+",  "5.11b") },
        { "7a",  ("VIII-", "5.11c") },
        { "7a+", ("VIII",  "5.11d") },
        { "7b",  ("VIII+", "5.12a") },
        { "7b+", ("IX-",   "5.12b") },
        { "7c",  ("IX",    "5.12c") },
        { "7c+", ("IX+",   "5.12d") },
        { "8a",  ("X-",    "5.13a") },
        { "8a+", ("X",     "5.13b") },
        { "8b",  ("X+",    "5.13c") },
        { "8b+", ("XI-",   "5.13d") },
        { "8c",  ("XI",    "5.14a") },
        { "8c+", ("XI+",   "5.14b") },
        { "9a",  ("XII-",  "5.15a") },
    };

    /// <summary>
    /// Sortierreihenfolge für französische Grade (niedriger = einfacher).
    /// Wird für die Grad-Entwicklung im Stats-Endpoint verwendet.
    /// </summary>
    private static readonly List<string> GradeOrder =
    [
        "4", "4+", "5", "5a", "5b", "5+", "5c",
        "6a", "6a+", "6b", "6b+", "6c", "6c+",
        "7a", "7a+", "7b", "7b+", "7c", "7c+",
        "8a", "8a+", "8b", "8b+", "8c", "8c+",
        "9a", "9a+", "9b", "9b+", "9c"
    ];

    /// <summary>
    /// Gibt die komplette sortierte Liste aller französischen Grade zurück (für Rang → Grade Konvertierung).
    /// </summary>
    public static List<string> GetAllGrades() => GradeOrder;

    /// <summary>
    /// Gibt den numerischen Rang eines Grads zurück — höher = schwerer.
    /// Gibt -1 zurück wenn der Grad unbekannt ist.
    /// </summary>
    public static int Rank(string? frenchGrade)
    {
        if (string.IsNullOrWhiteSpace(frenchGrade)) return -1;
        var idx = GradeOrder.IndexOf(frenchGrade.ToLower().Trim());
        return idx;
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

        var key = frenchGrade.ToLower().Trim();

        return scale.ToLower() switch
        {
            "french"   => frenchGrade,
            "uiaa"     => GradeMap.TryGetValue(key, out var u) ? u.Uiaa     : null,
            "american" => GradeMap.TryGetValue(key, out var a) ? a.American : null,
            _          => frenchGrade
        };
    }
}
