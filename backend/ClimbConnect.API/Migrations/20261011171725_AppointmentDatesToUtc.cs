using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <summary>
    /// Termine wurden bisher in Wiener Ortszeit gespeichert, ab jetzt in UTC.
    /// Rechnet vorhandene Termine einmalig um: Sommerzeit -2 Stunden, Winterzeit -1 Stunde.
    /// </summary>
    public partial class AppointmentDatesToUtc : Migration
    {
        // Sommerzeit in Wien (Ortszeit): letzter Sonntag im März 02:00 bis letzter Sonntag im Oktober 03:00
        private static readonly (string Start, string End)[] SummerTime =
        [
            ("2024-03-31 02:00:00", "2024-10-27 03:00:00"),
            ("2025-03-30 02:00:00", "2025-10-26 03:00:00"),
            ("2026-03-29 02:00:00", "2026-10-25 03:00:00"),
            ("2027-03-28 02:00:00", "2027-10-31 03:00:00"),
            ("2028-03-26 02:00:00", "2028-10-29 03:00:00"),
            ("2029-03-25 02:00:00", "2029-10-28 03:00:00"),
            ("2030-03-31 02:00:00", "2030-10-27 03:00:00"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE \"Appointments\" SET \"Date\" = datetime(\"Date\", CASE WHEN {IsSummerTime()} THEN '-2 hours' ELSE '-1 hours' END);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Umkehrung: UTC zurück in Ortszeit (Grenzen gelten hier näherungsweise auch für UTC-Werte)
            migrationBuilder.Sql(
                $"UPDATE \"Appointments\" SET \"Date\" = datetime(\"Date\", CASE WHEN {IsSummerTime()} THEN '+2 hours' ELSE '+1 hours' END);");
        }

        /// <summary>SQL-Bedingung: liegt "Date" in einer Sommerzeit-Periode?</summary>
        private static string IsSummerTime() => string.Join(" OR ",
            SummerTime.Select(p => $"(datetime(\"Date\") >= '{p.Start}' AND datetime(\"Date\") < '{p.End}')"));
    }
}
