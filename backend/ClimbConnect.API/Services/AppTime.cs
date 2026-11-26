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
    public static (DateTime StartUtc, DateTime EndUtc) TodayInViennaUtc() => ViennaDaysUtc(0, 1);

    /// <summary>
    /// Die nächsten 7 Tage in Wien – ab morgen, ohne heute (#239) –, als UTC-Zeitraum.
    /// "Heute" wird in der Gebietsliste separat gezählt, so zählt kein Termin doppelt.
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) NextWeekInViennaUtc() => ViennaDaysUtc(1, 7);

    /// <summary>
    /// Ganze Tage nach Wiener Ortszeit als UTC-Zeitraum: ab Tag <paramref name="fromDay"/>
    /// (0 = heute, 1 = morgen) für <paramref name="dayCount"/> Tage. Beginn zählt dazu, Ende nicht.
    /// Über eine Sommerzeit-Umstellung hinweg ist der Zeitraum eine Stunde kürzer oder länger –
    /// gerechnet wird immer von Wiener Mitternacht zu Wiener Mitternacht.
    /// </summary>
    /// <param name="nowUtc">"Jetzt" in UTC – nur für Tests; ohne Angabe die aktuelle Zeit.</param>
    public static (DateTime StartUtc, DateTime EndUtc) ViennaDaysUtc(int fromDay, int dayCount, DateTime? nowUtc = null)
    {
        var todayVienna = TimeZoneInfo.ConvertTimeFromUtc(nowUtc ?? DateTime.UtcNow, Vienna).Date;
        var startUtc    = TimeZoneInfo.ConvertTimeToUtc(todayVienna.AddDays(fromDay), Vienna);
        var endUtc      = TimeZoneInfo.ConvertTimeToUtc(todayVienna.AddDays(fromDay + dayCount), Vienna);
        return (startUtc, endUtc);
    }
}
