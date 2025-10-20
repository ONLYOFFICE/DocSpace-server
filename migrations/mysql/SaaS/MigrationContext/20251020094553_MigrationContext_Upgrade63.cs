using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade63 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "chat_provider_id",
                table: "files_room_settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "chat_settings",
                table: "files_room_settings",
                type: "json",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "ai_chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    created_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_chats_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "ai_mcp_room_servers",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    server_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.room_id, x.server_id });
                    table.ForeignKey(
                        name: "FK_ai_mcp_room_servers_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_mcp_room_servers_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "ai_mcp_server_settings",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    server_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    oauth_credentials = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tool_config = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.room_id, x.user_id, x.server_id });
                    table.ForeignKey(
                        name: "FK_ai_mcp_server_settings_core_user_user_id",
                        column: x => x.user_id,
                        principalTable: "core_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_mcp_server_settings_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_mcp_server_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "ai_mcp_server_states",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    enabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ai_mcp_server_states_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "ai_mcp_servers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    endpoint = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    headers = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    connection_type = table.Column<int>(type: "int", nullable: false),
                    has_icon = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_mcp_servers_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "ai_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    url = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    key = table.Column<string>(type: "text", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    created_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_providers_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "ai_user_chat_settings",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    web_search_enabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.room_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_ai_user_chat_settings_core_user_user_id",
                        column: x => x.user_id,
                        principalTable: "core_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_user_chat_settings_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_user_chat_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_file_vectorization",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    file_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    updated_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.file_id });
                    table.ForeignKey(
                        name: "FK_files_file_vectorization_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateTable(
                name: "ai_chats_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    chat_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    role = table.Column<int>(type: "int", nullable: false),
                    content = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    created_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_chats_messages_ai_chats_chat_id",
                        column: x => x.chat_id,
                        principalTable: "ai_chats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_chat_provider_id",
                table: "files_room_settings",
                column: "chat_provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_id",
                table: "ai_chats",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_room_id_user_id_modified_on",
                table: "ai_chats",
                columns: new[] { "tenant_id", "room_id", "user_id", "modified_on" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chats_messages_chat_id",
                table: "ai_chats_messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mcp_room_servers_room_id",
                table: "ai_mcp_room_servers",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mcp_server_settings_room_id",
                table: "ai_mcp_server_settings",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_mcp_server_settings_user_id",
                table: "ai_mcp_server_settings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_id",
                table: "ai_mcp_servers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_name",
                table: "ai_mcp_servers",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_id",
                table: "ai_providers",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_user_chat_settings_room_id",
                table: "ai_user_chat_settings",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_user_chat_settings_user_id",
                table: "ai_user_chat_settings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_id",
                table: "files_file_vectorization",
                column: "file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_chats_messages");

            migrationBuilder.DropTable(
                name: "ai_mcp_room_servers");

            migrationBuilder.DropTable(
                name: "ai_mcp_server_settings");

            migrationBuilder.DropTable(
                name: "ai_mcp_server_states");

            migrationBuilder.DropTable(
                name: "ai_mcp_servers");

            migrationBuilder.DropTable(
                name: "ai_providers");

            migrationBuilder.DropTable(
                name: "ai_user_chat_settings");

            migrationBuilder.DropTable(
                name: "files_file_vectorization");

            migrationBuilder.DropTable(
                name: "ai_chats");

            migrationBuilder.DropIndex(
                name: "IX_chat_provider_id",
                table: "files_room_settings");

            migrationBuilder.DropColumn(
                name: "chat_provider_id",
                table: "files_room_settings");

            migrationBuilder.DropColumn(
                name: "chat_settings",
                table: "files_room_settings");
        }
    }
}
