using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    public partial class ERPConsent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "erps",
                table: "profile",
                newName: "erp_consent");

            migrationBuilder.Sql("UPDATE profile SET erp_consent = 'Enabled' WHERE erp_consent IN ('Yes', 'Full', 'Partial');");
            migrationBuilder.Sql("UPDATE profile SET erp_consent = 'Disabled' WHERE erp_consent IN ('No', 'Disabled');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE profile SET erp_consent = 'Yes' WHERE erp_consent IN ('Enabled', 'Full');");
            migrationBuilder.Sql("UPDATE profile SET erp_consent = 'No' WHERE erp_consent IN ('Disabled', 'No', 'Partial');");

            migrationBuilder.RenameColumn(
                name: "erp_consent",
                table: "profile",
                newName: "erps");
        }

    }
}
