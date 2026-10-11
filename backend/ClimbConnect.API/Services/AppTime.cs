namespace ClimbConnect.API.Services;

/// <summary>
/// Zeitumrechnung für Termine. Termine werden in UTC gespeichert; "heute" bezieht sich
/// aber auf die österreichische Ortszeit, weil alle Klettergebiete in OÖ liegen.
/// </summary>
public static class AppTime
{
    /// <summary>Zeitzone der Klettergebiete (Mitteleuropäische Zeit inkl. Sommerzeit).</summary>
    public static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    /// <summary>
    /// Wandelt eine vom Client gesendete Zeit in UTC um. Zeiten mit Offset (z.B. "...Z" oder
    /// "+02:00") werden umgerechnet; Zeiten ohne Angabe gelten laut API als UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc   => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _                  => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>
    /// Beginn und Ende des heutigen Tages in Wien, jeweils als UTC-Zeitpunkt.
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) TodayInViennaUtc()
    {
        var todayVienna = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Vienna).Date;
        var startUtc    = TimeZoneInfo.ConvertTimeToUtc(todayVienna, Vienna);
        var endUtc      = TimeZoneInfo.ConvertTimeToUtc(todayVienna.AddDays(1), Vienna);
        return (startUtc, endUtc);
    }
}
