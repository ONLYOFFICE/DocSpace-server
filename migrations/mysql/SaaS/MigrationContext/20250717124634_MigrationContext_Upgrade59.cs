using System;
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
            migrationBuilder.CreateTable(
                name: "ai_mcp_tools",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    server_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tools = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.room_id, x.user_id, x.server_id });
                    table.ForeignKey(
                        name: "FK_ai_mcp_tools_core_user_user_id",
                        column: x => x.user_id,
                        principalTable: "core_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_mcp_tools_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_mcp_tools_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mcp_tools_room_id",
                table: "ai_mcp_tools",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mcp_tools_user_id",
                table: "ai_mcp_tools",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_mcp_tools");
        }
    }
}
