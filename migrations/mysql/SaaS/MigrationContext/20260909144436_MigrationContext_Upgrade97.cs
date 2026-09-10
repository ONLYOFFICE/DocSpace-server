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
            migrationBuilder.AddColumn<int>(
                name: "depth",
                table: "ai_integration_preferences",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE ai_integration_preferences SET depth = CASE deep_mode WHEN 1 THEN 2 WHEN 0 THEN 0 END");

            migrationBuilder.DropColumn(
                name: "deep_mode",
                table: "ai_integration_preferences");

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

            migrationBuilder.Sql(
                """
                UPDATE ai_integration_profiles SET reasoning = CASE
                    WHEN reasoning = 1 THEN '{"Thinks":true,"CanDisable":true,"Depths":[],"DefaultDepth":null}'
                    WHEN reasoning = 0 THEN '{"Thinks":false,"CanDisable":false,"Depths":[],"DefaultDepth":null}'
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "deep_mode",
                table: "ai_integration_preferences",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.Sql("UPDATE ai_integration_preferences SET deep_mode = CASE WHEN depth = 0 THEN 0 WHEN depth > 0 THEN 1 END");

            migrationBuilder.DropColumn(
                name: "depth",
                table: "ai_integration_preferences");

            migrationBuilder.Sql(
                """
                UPDATE ai_integration_profiles SET reasoning = CASE JSON_UNQUOTE(JSON_EXTRACT(reasoning, '$.Thinks'))
                    WHEN 'true' THEN CAST(1 AS JSON)
                    WHEN 'false' THEN CAST(0 AS JSON)
                END
                """);

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
        }
    }
}
