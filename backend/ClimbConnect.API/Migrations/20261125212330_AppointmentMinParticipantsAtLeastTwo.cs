using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <summary>
    /// #228: Das Minimum eines Termins ist jetzt mindestens 2 ("allein klettern braucht keinen
    /// Termin"). Bestehende Termine ohne Minimum oder mit Minimum 1 werden auf 2 gesetzt.
    /// Ein Maximum unter 2 wird auf 2 angehoben, damit Minimum ≤ Maximum gilt.
    /// </summary>
    public partial class AppointmentMinParticipantsAtLeastTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Appointments SET MinParticipants = 2 WHERE MinParticipants IS NULL OR MinParticipants < 2;");
            migrationBuilder.Sql(
                "UPDATE Appointments SET MaxParticipants = 2 WHERE MaxParticipants IS NOT NULL AND MaxParticipants < 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reine Datenänderung – die alten Werte lassen sich nicht wiederherstellen
        }
    }
}
