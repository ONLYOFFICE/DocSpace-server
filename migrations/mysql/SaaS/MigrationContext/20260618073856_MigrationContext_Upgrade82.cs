using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade82 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "save_form_as_xlsx",
                table: "files_room_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValueSql: "1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "save_form_as_xlsx",
                table: "files_room_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "1",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: true);
        }
    }
}
