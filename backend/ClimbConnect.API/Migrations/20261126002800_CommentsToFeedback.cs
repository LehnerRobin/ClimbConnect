using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class CommentsToFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kommentare werden zu Feedback (#244): neue Tabelle anlegen, alle Kommentare
            // übernehmen (gleiche Ids, als "ungelesen") und erst danach die alte Tabelle löschen –
            // so geht nichts verloren.
            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    RouteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                "INSERT INTO Feedback (Id, UserId, AreaId, RouteId, Text, PhotoUrl, IsRead, CreatedAtUtc) " +
                "SELECT Id, UserId, AreaId, RouteId, Text, PhotoUrl, 0, CreatedAtUtc FROM Comments;");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_AreaId",
                table: "Feedback",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_RouteId",
                table: "Feedback",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_UserId",
                table: "Feedback",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    RouteId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AreaId",
                table: "Comments",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_RouteId",
                table: "Comments",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            // Rückweg: Feedback wieder als Kommentare (Gelesen-Status geht dabei verloren)
            migrationBuilder.Sql(
                "INSERT INTO Comments (Id, UserId, AreaId, RouteId, Text, PhotoUrl, CreatedAtUtc) " +
                "SELECT Id, UserId, AreaId, RouteId, Text, PhotoUrl, CreatedAtUtc FROM Feedback;");

            migrationBuilder.DropTable(
                name: "Feedback");
        }
    }
}
