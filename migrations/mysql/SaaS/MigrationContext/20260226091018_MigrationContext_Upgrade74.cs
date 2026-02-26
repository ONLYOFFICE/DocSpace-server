using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade74 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "save_form_as_xlsx",
                table: "files_room_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.AddColumn<bool>(
                name: "send_form_to_external_db",
                table: "files_room_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "save_form_as_xlsx",
                table: "files_room_settings");

            migrationBuilder.DropColumn(
                name: "send_form_to_external_db",
                table: "files_room_settings");
        }
    }
}
