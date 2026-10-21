using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentServices.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxTeamsPerGroup = table.Column<int>(type: "integer", nullable: false),
                    NumberOfGroups = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TournamentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_groups_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_teams",
                columns: table => new
                {
                    group_id = table.Column<string>(type: "character varying(64)", nullable: false),
                    team_id = table.Column<string>(type: "character varying(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_teams", x => new { x.group_id, x.team_id });
                    table.ForeignKey(
                        name: "FK_group_teams_groups_group_id",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_teams_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TournamentId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GroupId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    HomeTeamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VisitorTeamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: false),
                    VisitorScore = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matches_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_matches_teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matches_teams_VisitorTeamId",
                        column: x => x.VisitorTeamId,
                        principalTable: "teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matches_tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_group_teams_team_id",
                table: "group_teams",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_TournamentId_Name",
                table: "groups",
                columns: new[] { "TournamentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_GroupId",
                table: "matches",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_HomeTeamId",
                table: "matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_TournamentId",
                table: "matches",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_VisitorTeamId",
                table: "matches",
                column: "VisitorTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_teams_Name",
                table: "teams",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "group_teams");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "teams");

            migrationBuilder.DropTable(
                name: "tournaments");
        }
    }
}
