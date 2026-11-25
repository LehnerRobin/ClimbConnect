using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <summary>
    /// #254: Der Ersteller eines Termins ist automatisch Teilnehmer. Bei bestehenden Terminen
    /// wird er nachgetragen, falls er noch nicht beigetreten ist.
    /// </summary>
    public partial class AppointmentCreatorIsParticipant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO AppointmentUsers (AppointmentId, UserId, Comment, JoinedAtUtc)
                SELECT a.Id, a.CreatedByUserId, NULL, a.CreatedAtUtc
                FROM Appointments a
                WHERE EXISTS (SELECT 1 FROM Users u WHERE u.Id = a.CreatedByUserId)
                  AND NOT EXISTS (SELECT 1 FROM AppointmentUsers au
                                  WHERE au.AppointmentId = a.Id AND au.UserId = a.CreatedByUserId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reine Datenänderung – nicht unterscheidbar, wer selbst beigetreten ist
        }
    }
}
