using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade73 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "start_filling",
                table: "files_properties",
                type: "tinyint(1)",
                nullable: true,
                computedColumnSql: "IF(JSON_EXTRACT(`data`, '$.FormFilling.StartFilling') IS NULL, NULL, JSON_EXTRACT(`data`, '$.FormFilling.StartFilling'))",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenant_start_entry",
                table: "files_properties",
                columns: new[] { "tenant_id", "start_filling", "entry_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_tenant_start_entry",
                table: "files_properties");

            migrationBuilder.DropColumn(
                name: "start_filling",
                table: "files_properties");
        }
    }
}
