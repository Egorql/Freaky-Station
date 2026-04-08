using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class DailyRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_reward_progress",
                columns: table => new
                {
                    daily_reward_progress_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    current_streak = table.Column<int>(type: "INTEGER", nullable: false),
                    last_claim_time = table.Column<DateTime>(type: "TEXT", nullable: true),
                    pending_active_time = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_reward_progress", x => x.daily_reward_progress_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_reward_progress_player_id",
                table: "daily_reward_progress",
                column: "player_id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_reward_progress");
        }
    }
}
