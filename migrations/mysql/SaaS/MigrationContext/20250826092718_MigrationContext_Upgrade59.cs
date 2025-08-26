using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade59 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_mcp_rooms_servers_files_folder_room_id",
                table: "ai_mcp_rooms_servers");

            migrationBuilder.DropForeignKey(
                name: "FK_ai_mcp_rooms_servers_tenants_tenants_tenant_id",
                table: "ai_mcp_rooms_servers");

            migrationBuilder.RenameTable(
                name: "ai_mcp_rooms_servers",
                newName: "ai_mcp_room_servers");

            migrationBuilder.RenameIndex(
                name: "IX_ai_mcp_rooms_servers_room_id",
                table: "ai_mcp_room_servers",
                newName: "IX_ai_mcp_room_servers_room_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_mcp_room_servers_files_folder_room_id",
                table: "ai_mcp_room_servers",
                column: "room_id",
                principalTable: "files_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_mcp_room_servers_tenants_tenants_tenant_id",
                table: "ai_mcp_room_servers",
                column: "tenant_id",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ai_mcp_room_servers_files_folder_room_id",
                table: "ai_mcp_room_servers");

            migrationBuilder.DropForeignKey(
                name: "FK_ai_mcp_room_servers_tenants_tenants_tenant_id",
                table: "ai_mcp_room_servers");

            migrationBuilder.RenameTable(
                name: "ai_mcp_room_servers",
                newName: "ai_mcp_rooms_servers");

            migrationBuilder.RenameIndex(
                name: "IX_ai_mcp_room_servers_room_id",
                table: "ai_mcp_rooms_servers",
                newName: "IX_ai_mcp_rooms_servers_room_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ai_mcp_rooms_servers_files_folder_room_id",
                table: "ai_mcp_rooms_servers",
                column: "room_id",
                principalTable: "files_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ai_mcp_rooms_servers_tenants_tenants_tenant_id",
                table: "ai_mcp_rooms_servers",
                column: "tenant_id",
                principalTable: "tenants_tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
