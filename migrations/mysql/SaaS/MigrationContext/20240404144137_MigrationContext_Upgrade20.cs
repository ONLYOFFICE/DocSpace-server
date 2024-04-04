using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "removed",
                table: "files_folder",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.AddColumn<bool>(
                name: "removed",
                table: "files_file",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "removed",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "removed",
                table: "files_file");
        }
    }
}
