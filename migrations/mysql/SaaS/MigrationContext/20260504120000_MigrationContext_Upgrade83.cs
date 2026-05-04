using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade83 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PRIMARY",
                table: "ai_integration_tool_prefs");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "ai_integration_tool_prefs",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddPrimaryKey(
                name: "PRIMARY",
                table: "ai_integration_tool_prefs",
                columns: new[] { "tenant_id", "server_type", "created_by" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_created_by",
                table: "ai_integration_tool_prefs",
                columns: new[] { "tenant_id", "created_by" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_id_created_by",
                table: "ai_integration_tool_prefs");

            migrationBuilder.DropPrimaryKey(
                name: "PRIMARY",
                table: "ai_integration_tool_prefs");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "ai_integration_tool_prefs");

            migrationBuilder.AddPrimaryKey(
                name: "PRIMARY",
                table: "ai_integration_tool_prefs",
                columns: new[] { "tenant_id", "server_type" });
        }
    }
}
