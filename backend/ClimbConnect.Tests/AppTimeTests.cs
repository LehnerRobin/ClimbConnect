using ClimbConnect.API.Services;

namespace ClimbConnect.Tests;

/// <summary>
/// Unit-Tests für die Tages-Zeiträume nach Wiener Ortszeit (#239).
/// "Jetzt" wird fest vorgegeben, damit auch die Sommerzeit-Umstellungen geprüft werden.
/// </summary>
public class AppTimeTests
{
    private static DateTime Utc(int y, int m, int d, int h, int min = 0) => new(y, m, d, h, min, 0, DateTimeKind.Utc);

    [Fact]
    public void Heute_KurzNachMitternachtInWien_IstSchonDerNeueTag()
    {
        // 23:30 UTC = 00:30 in Wien am 15. Jänner (Winterzeit, UTC+1)
        var (start, end) = AppTime.ViennaDaysUtc(0, 1, Utc(2026, 1, 14, 23, 30));

        Assert.Equal(Utc(2026, 1, 14, 23), start); // 15.1. 00:00 Wien
        Assert.Equal(Utc(2026, 1, 15, 23), end);   // 16.1. 00:00 Wien
    }

    [Fact]
    public void NaechsteSiebenTage_BeginnenMorgen_OhneHeute()
    {
        // 20. Juni 2026, 12:00 Wien (Sommerzeit, UTC+2)
        var (start, end) = AppTime.ViennaDaysUtc(1, 7, Utc(2026, 6, 20, 10));

        Assert.Equal(Utc(2026, 6, 20, 22), start); // 21.6. 00:00 Wien
        Assert.Equal(Utc(2026, 6, 27, 22), end);   // 28.6. 00:00 Wien
    }

    [Fact]
    public void NaechsteSiebenTage_UeberDieUmstellungAufSommerzeit_SindEineStundeKuerzer()
    {
        // 25. März 2026 – am 29. März wird auf Sommerzeit umgestellt
        var (start, end) = AppTime.ViennaDaysUtc(1, 7, Utc(2026, 3, 25, 12));

        Assert.Equal(Utc(2026, 3, 25, 23), start); // 26.3. 00:00 Wien (UTC+1)
        Assert.Equal(Utc(2026, 4, 1, 22), end);    //  2.4. 00:00 Wien (UTC+2)
        Assert.Equal(TimeSpan.FromHours(7 * 24 - 1), end - start);
    }

    [Fact]
    public void NaechsteSiebenTage_UeberDieUmstellungAufWinterzeit_SindEineStundeLaenger()
    {
        // 22. Oktober 2026 – am 25. Oktober wird auf Winterzeit umgestellt
        var (start, end) = AppTime.ViennaDaysUtc(1, 7, Utc(2026, 10, 22, 12));

        Assert.Equal(Utc(2026, 10, 22, 22), start); // 23.10. 00:00 Wien (UTC+2)
        Assert.Equal(Utc(2026, 10, 29, 23), end);   // 30.10. 00:00 Wien (UTC+1)
        Assert.Equal(TimeSpan.FromHours(7 * 24 + 1), end - start);
    }

    [Fact]
    public void HeuteUndNaechsteWoche_SchliessenLueckenlosAneinander()
    {
        var (_, todayEnd)  = AppTime.TodayInViennaUtc();
        var (weekStart, _) = AppTime.NextWeekInViennaUtc();
        Assert.Equal(todayEnd, weekStart);
    }
}
