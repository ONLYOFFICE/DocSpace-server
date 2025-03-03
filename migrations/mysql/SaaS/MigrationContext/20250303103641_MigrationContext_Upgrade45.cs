using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade45 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhooks");

            migrationBuilder.DropColumn(
                name: "webhook_id",
                table: "webhooks_logs");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "trigger",
                table: "webhooks_logs");

            migrationBuilder.AddColumn<int>(
                name: "webhook_id",
                table: "webhooks_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "webhooks",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    method = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, defaultValueSql: "''")
                        .Annotation("MySql:CharSet", "utf8"),
                    route = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, defaultValueSql: "''")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8");
        }
    }
}
