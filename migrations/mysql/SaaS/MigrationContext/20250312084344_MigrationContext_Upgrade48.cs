using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade48 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trigger",
                table: "webhooks_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "webhooks_config",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_content",
                table: "webhooks_config",
                type: "varchar(200)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_failure_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_success_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "webhooks_config",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "triggers",
                table: "webhooks_config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "target_id",
                table: "webhooks_config",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trigger",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_failure_content",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_failure_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_success_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "triggers",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "target_id",
                table: "webhooks_config");
        }
    }
}
