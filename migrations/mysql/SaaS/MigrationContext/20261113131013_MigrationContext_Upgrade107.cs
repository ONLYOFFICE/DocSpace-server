using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade107 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "artifacts_folder_id",
                table: "ai_integration_threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attachments_folder_id",
                table: "ai_integration_threads",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "artifacts_folder_id",
                table: "ai_integration_threads");

            migrationBuilder.DropColumn(
                name: "attachments_folder_id",
                table: "ai_integration_threads");
        }
    }
}
