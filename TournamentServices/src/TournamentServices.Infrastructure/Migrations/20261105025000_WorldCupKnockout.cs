using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorldCupKnockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "tournaments");

            migrationBuilder.AddColumn<int>(
                name: "Round",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Round",
                table: "matches");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "tournaments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }
    }
}
