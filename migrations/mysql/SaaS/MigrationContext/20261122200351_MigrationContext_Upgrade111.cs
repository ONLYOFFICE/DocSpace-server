using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_tool_preferences",
                columns: new[] { "tenant_id", "entry_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_preferences",
                columns: new[] { "tenant_id", "entry_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_mcp_servers",
                columns: new[] { "tenant_id", "entry_id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_assignments",
                columns: new[] { "tenant_id", "entry_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_tool_preferences");

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_preferences");

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_mcp_servers");

            migrationBuilder.DropIndex(
                name: "IX_tenant_id_entry_id",
                table: "ai_integration_assignments");
        }
    }
}
