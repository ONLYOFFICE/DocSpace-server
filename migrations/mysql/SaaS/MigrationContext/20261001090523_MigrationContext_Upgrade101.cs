using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade101 : Migration
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
                name: "next_attempt_on",
                table: "webhooks_logs");
        }
    }
}
