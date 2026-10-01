using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClimbConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteCommentsReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Areas_AreaId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Routes_RouteId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Areas_AreaId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Routes_RouteId",
                table: "Reports");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Areas_AreaId",
                table: "Comments",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Routes_RouteId",
                table: "Comments",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Areas_AreaId",
                table: "Reports",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Routes_RouteId",
                table: "Reports",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Areas_AreaId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Routes_RouteId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Areas_AreaId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Routes_RouteId",
                table: "Reports");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Areas_AreaId",
                table: "Comments",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Routes_RouteId",
                table: "Comments",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Areas_AreaId",
                table: "Reports",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Routes_RouteId",
                table: "Reports",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");
        }
    }
}
