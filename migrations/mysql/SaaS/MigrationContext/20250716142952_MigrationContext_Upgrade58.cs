using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade58 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "chat_provider_id",
                table: "files_room_settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "chat_settings",
                table: "files_room_settings",
                type: "json",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_chat_provider_id",
                table: "files_room_settings",
                column: "chat_provider_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_chat_provider_id",
                table: "files_room_settings");

            migrationBuilder.DropColumn(
                name: "chat_provider_id",
                table: "files_room_settings");

            migrationBuilder.DropColumn(
                name: "chat_settings",
                table: "files_room_settings");
        }
    }
}
