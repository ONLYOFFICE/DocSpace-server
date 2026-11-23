using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade108 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "artifacts_folder_id",
                table: "ai_integration_threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "attachments_folder_id",
                table: "ai_integration_threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_threads",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deleted_on_id",
                table: "ai_integration_threads",
                columns: new[] { "deleted_on", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deleted_on_id",
                table: "ai_integration_threads");

            migrationBuilder.DropColumn(
                name: "artifacts_folder_id",
                table: "ai_integration_threads");

            migrationBuilder.DropColumn(
                name: "attachments_folder_id",
                table: "ai_integration_threads");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "ai_integration_threads");
        }
    }
}
