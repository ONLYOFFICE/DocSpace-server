using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade97 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deep_mode",
                table: "ai_integration_preferences");

            migrationBuilder.Sql("UPDATE ai_integration_profiles SET reasoning = NULL");

            migrationBuilder.AlterColumn<string>(
                name: "reasoning",
                table: "ai_integration_profiles",
                type: "json",
                nullable: true,
                collation: "utf8_general_ci",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<int>(
                name: "depth",
                table: "ai_integration_preferences",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "depth",
                table: "ai_integration_preferences");

            migrationBuilder.Sql("UPDATE ai_integration_profiles SET reasoning = NULL");

            migrationBuilder.AlterColumn<bool>(
                name: "reasoning",
                table: "ai_integration_profiles",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true,
                oldCollation: "utf8_general_ci")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<bool>(
                name: "deep_mode",
                table: "ai_integration_preferences",
                type: "tinyint(1)",
                nullable: true);
        }
    }
}
