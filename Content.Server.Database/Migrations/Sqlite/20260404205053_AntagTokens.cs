using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AntagTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_antag_token",
                columns: table => new
                {
                    player_antag_token_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    token_id = table.Column<string>(type: "TEXT", nullable: false),
                    amount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_antag_token", x => x.player_antag_token_id);
                });

            migrationBuilder.CreateTable(
                name: "player_antag_token_selection",
                columns: table => new
                {
                    player_antag_token_selection_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    token_id = table.Column<string>(type: "TEXT", nullable: false),
                    antag_id = table.Column<string>(type: "TEXT", nullable: false),
                    selected_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_antag_token_selection", x => x.player_antag_token_selection_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_antag_token_player_id_token_id",
                table: "player_antag_token",
                columns: new[] { "player_id", "token_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_antag_token_selection_player_id",
                table: "player_antag_token_selection",
                column: "player_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_antag_token");

            migrationBuilder.DropTable(
                name: "player_antag_token_selection");
        }
    }
}
