using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    public partial class DailyRewards : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_reward_progress",
                columns: table => new
                {
                    daily_reward_progress_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_streak = table.Column<int>(type: "integer", nullable: false),
                    last_claim_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pending_active_time = table.Column<TimeSpan>(type: "interval", nullable: false)
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
