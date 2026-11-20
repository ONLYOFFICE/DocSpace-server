using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade109 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_attachments",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deleted_on_id",
                table: "ai_integration_attachments",
                columns: new[] { "deleted_on", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_deleted_on_id",
                table: "ai_integration_attachments");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "ai_integration_attachments");
        }
    }
}
