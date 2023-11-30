using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "counter",
                table: "files_folder",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.AddColumn<long>(
                name: "quota",
                table: "files_folder",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'-2'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "counter",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "quota",
                table: "files_folder");
        }
    }
}
