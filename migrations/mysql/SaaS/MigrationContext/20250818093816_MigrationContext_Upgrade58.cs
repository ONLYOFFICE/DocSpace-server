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
            migrationBuilder.AddColumn<string>(
                name: "telegram_username",
                table: "telegram_users",
                type: "varchar(35)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "tgUsername",
                table: "telegram_users",
                column: "telegram_username");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "tgUsername",
                table: "telegram_users");

            migrationBuilder.DropColumn(
                name: "telegram_username",
                table: "telegram_users");
        }
    }
}
