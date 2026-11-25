using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModerationCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AppointmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    AppointmentTitle = table.Column<string>(type: "TEXT", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AreaName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReportAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasePosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CaseId = table.Column<int>(type: "INTEGER", nullable: false),
                    OriginalPostId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorUsername = table.Column<string>(type: "TEXT", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeletedByAuthor = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasePosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CasePosts_ModerationCases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "ModerationCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CasePostId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReporterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReporterUsername = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostReports_CasePosts_CasePostId",
                        column: x => x.CasePostId,
                        principalTable: "CasePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CasePosts_CaseId_OriginalPostId",
                table: "CasePosts",
                columns: new[] { "CaseId", "OriginalPostId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CasePosts_OriginalPostId",
                table: "CasePosts",
                column: "OriginalPostId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationCases_AppointmentId",
                table: "ModerationCases",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostReports_CasePostId_ReporterId",
                table: "PostReports",
                columns: new[] { "CasePostId", "ReporterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostReports");

            migrationBuilder.DropTable(
                name: "CasePosts");

            migrationBuilder.DropTable(
                name: "ModerationCases");
        }
    }
}
