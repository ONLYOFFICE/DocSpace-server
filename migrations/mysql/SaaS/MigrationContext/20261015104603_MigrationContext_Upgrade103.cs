using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade103 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempts",
                table: "webhooks_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "lease_id",
                table: "webhooks_logs",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "next_attempt_on",
                table: "webhooks_logs",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "next_attempt_on",
                table: "webhooks_logs",
                column: "next_attempt_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "next_attempt_on",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "attempts",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "lease_id",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "next_attempt_on",
                table: "webhooks_logs");
        }
    }
}
