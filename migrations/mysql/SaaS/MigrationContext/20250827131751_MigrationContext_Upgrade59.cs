using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade59 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -12);

            migrationBuilder.DropColumn(
                name: "paid",
                table: "backup_backup");

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
                    connection_type = table.Column<int>(type: "int", nullable: false)
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
                name: "files_converts",
                columns: table => new
                {
                    input = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    output = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.input, x.output });
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
                    id = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.InsertData(
                table: "files_converts",
                columns: new[] { "input", "output" },
                values: new object[,]
                {
                    { ".csv", ".ods" },
                    { ".csv", ".ots" },
                    { ".csv", ".pdf" },
                    { ".csv", ".xlsm" },
                    { ".csv", ".xlsx" },
                    { ".csv", ".xltm" },
                    { ".csv", ".xltx" },
                    { ".djvu", ".pdf" },
                    { ".doc", ".docm" },
                    { ".doc", ".docx" },
                    { ".doc", ".dotm" },
                    { ".doc", ".dotx" },
                    { ".doc", ".epub" },
                    { ".doc", ".fb2" },
                    { ".doc", ".html" },
                    { ".doc", ".odt" },
                    { ".doc", ".ott" },
                    { ".doc", ".pdf" },
                    { ".doc", ".rtf" },
                    { ".doc", ".txt" },
                    { ".docm", ".docx" },
                    { ".docm", ".dotm" },
                    { ".docm", ".dotx" },
                    { ".docm", ".epub" },
                    { ".docm", ".fb2" },
                    { ".docm", ".html" },
                    { ".docm", ".odt" },
                    { ".docm", ".ott" },
                    { ".docm", ".pdf" },
                    { ".docm", ".rtf" },
                    { ".docm", ".txt" },
                    { ".doct", ".docx" },
                    { ".docx", ".docm" },
                    { ".docx", ".dotm" },
                    { ".docx", ".dotx" },
                    { ".docx", ".epub" },
                    { ".docx", ".fb2" },
                    { ".docx", ".html" },
                    { ".docx", ".odt" },
                    { ".docx", ".ott" },
                    { ".docx", ".pdf" },
                    { ".docx", ".rtf" },
                    { ".docx", ".txt" },
                    { ".docxf", ".docm" },
                    { ".docxf", ".docx" },
                    { ".docxf", ".dotm" },
                    { ".docxf", ".dotx" },
                    { ".docxf", ".epub" },
                    { ".docxf", ".fb2" },
                    { ".docxf", ".html" },
                    { ".docxf", ".odt" },
                    { ".docxf", ".ott" },
                    { ".docxf", ".pdf" },
                    { ".docxf", ".rtf" },
                    { ".docxf", ".txt" },
                    { ".dot", ".docm" },
                    { ".dot", ".docx" },
                    { ".dot", ".dotm" },
                    { ".dot", ".dotx" },
                    { ".dot", ".epub" },
                    { ".dot", ".fb2" },
                    { ".dot", ".html" },
                    { ".dot", ".odt" },
                    { ".dot", ".ott" },
                    { ".dot", ".pdf" },
                    { ".dot", ".rtf" },
                    { ".dot", ".txt" },
                    { ".dotm", ".docm" },
                    { ".dotm", ".docx" },
                    { ".dotm", ".dotx" },
                    { ".dotm", ".epub" },
                    { ".dotm", ".fb2" },
                    { ".dotm", ".html" },
                    { ".dotm", ".odt" },
                    { ".dotm", ".ott" },
                    { ".dotm", ".pdf" },
                    { ".dotm", ".rtf" },
                    { ".dotm", ".txt" },
                    { ".dotx", ".docm" },
                    { ".dotx", ".docx" },
                    { ".dotx", ".dotm" },
                    { ".dotx", ".epub" },
                    { ".dotx", ".fb2" },
                    { ".dotx", ".html" },
                    { ".dotx", ".odt" },
                    { ".dotx", ".ott" },
                    { ".dotx", ".pdf" },
                    { ".dotx", ".rtf" },
                    { ".dotx", ".txt" },
                    { ".dps", ".odp" },
                    { ".dps", ".otp" },
                    { ".dps", ".pdf" },
                    { ".dps", ".potm" },
                    { ".dps", ".potx" },
                    { ".dps", ".ppsm" },
                    { ".dps", ".ppsx" },
                    { ".dps", ".pptm" },
                    { ".dps", ".pptx" },
                    { ".dpt", ".odp" },
                    { ".dpt", ".otp" },
                    { ".dpt", ".pdf" },
                    { ".dpt", ".potm" },
                    { ".dpt", ".potx" },
                    { ".dpt", ".ppsm" },
                    { ".dpt", ".ppsx" },
                    { ".dpt", ".pptm" },
                    { ".dpt", ".pptx" },
                    { ".epub", ".docm" },
                    { ".epub", ".docx" },
                    { ".epub", ".dotm" },
                    { ".epub", ".dotx" },
                    { ".epub", ".fb2" },
                    { ".epub", ".html" },
                    { ".epub", ".odt" },
                    { ".epub", ".ott" },
                    { ".epub", ".pdf" },
                    { ".epub", ".rtf" },
                    { ".epub", ".txt" },
                    { ".et", ".csv" },
                    { ".et", ".ods" },
                    { ".et", ".ots" },
                    { ".et", ".pdf" },
                    { ".et", ".xlsm" },
                    { ".et", ".xlsx" },
                    { ".et", ".xltm" },
                    { ".et", ".xltx" },
                    { ".ett", ".csv" },
                    { ".ett", ".ods" },
                    { ".ett", ".ots" },
                    { ".ett", ".pdf" },
                    { ".ett", ".xlsm" },
                    { ".ett", ".xlsx" },
                    { ".ett", ".xltm" },
                    { ".ett", ".xltx" },
                    { ".fb2", ".docm" },
                    { ".fb2", ".docx" },
                    { ".fb2", ".dotm" },
                    { ".fb2", ".dotx" },
                    { ".fb2", ".epub" },
                    { ".fb2", ".html" },
                    { ".fb2", ".odt" },
                    { ".fb2", ".ott" },
                    { ".fb2", ".pdf" },
                    { ".fb2", ".rtf" },
                    { ".fb2", ".txt" },
                    { ".fodp", ".odp" },
                    { ".fodp", ".otp" },
                    { ".fodp", ".pdf" },
                    { ".fodp", ".potm" },
                    { ".fodp", ".potx" },
                    { ".fodp", ".ppsm" },
                    { ".fodp", ".ppsx" },
                    { ".fodp", ".pptm" },
                    { ".fodp", ".pptx" },
                    { ".fods", ".csv" },
                    { ".fods", ".ods" },
                    { ".fods", ".ots" },
                    { ".fods", ".pdf" },
                    { ".fods", ".xlsm" },
                    { ".fods", ".xlsx" },
                    { ".fods", ".xltm" },
                    { ".fods", ".xltx" },
                    { ".fodt", ".docm" },
                    { ".fodt", ".docx" },
                    { ".fodt", ".dotm" },
                    { ".fodt", ".dotx" },
                    { ".fodt", ".epub" },
                    { ".fodt", ".fb2" },
                    { ".fodt", ".html" },
                    { ".fodt", ".odt" },
                    { ".fodt", ".ott" },
                    { ".fodt", ".pdf" },
                    { ".fodt", ".rtf" },
                    { ".fodt", ".txt" },
                    { ".htm", ".docm" },
                    { ".htm", ".docx" },
                    { ".htm", ".dotm" },
                    { ".htm", ".dotx" },
                    { ".htm", ".epub" },
                    { ".htm", ".fb2" },
                    { ".htm", ".html" },
                    { ".htm", ".odt" },
                    { ".htm", ".ott" },
                    { ".htm", ".pdf" },
                    { ".htm", ".rtf" },
                    { ".htm", ".txt" },
                    { ".html", ".docm" },
                    { ".html", ".docx" },
                    { ".html", ".dotm" },
                    { ".html", ".dotx" },
                    { ".html", ".epub" },
                    { ".html", ".fb2" },
                    { ".html", ".odt" },
                    { ".html", ".ott" },
                    { ".html", ".pdf" },
                    { ".html", ".rtf" },
                    { ".html", ".txt" },
                    { ".hwp", ".docm" },
                    { ".hwp", ".docx" },
                    { ".hwp", ".dotm" },
                    { ".hwp", ".dotx" },
                    { ".hwp", ".epub" },
                    { ".hwp", ".fb2" },
                    { ".hwp", ".html" },
                    { ".hwp", ".odt" },
                    { ".hwp", ".ott" },
                    { ".hwp", ".pdf" },
                    { ".hwp", ".rtf" },
                    { ".hwp", ".txt" },
                    { ".hwpx", ".docm" },
                    { ".hwpx", ".docx" },
                    { ".hwpx", ".dotm" },
                    { ".hwpx", ".dotx" },
                    { ".hwpx", ".epub" },
                    { ".hwpx", ".fb2" },
                    { ".hwpx", ".html" },
                    { ".hwpx", ".odt" },
                    { ".hwpx", ".ott" },
                    { ".hwpx", ".pdf" },
                    { ".hwpx", ".rtf" },
                    { ".hwpx", ".txt" },
                    { ".key", ".odp" },
                    { ".key", ".otp" },
                    { ".key", ".pdf" },
                    { ".key", ".potm" },
                    { ".key", ".potx" },
                    { ".key", ".ppsm" },
                    { ".key", ".ppsx" },
                    { ".key", ".pptm" },
                    { ".key", ".pptx" },
                    { ".md", ".docm" },
                    { ".md", ".docx" },
                    { ".md", ".dotm" },
                    { ".md", ".dotx" },
                    { ".md", ".epub" },
                    { ".md", ".fb2" },
                    { ".md", ".html" },
                    { ".md", ".odt" },
                    { ".md", ".ott" },
                    { ".md", ".pdf" },
                    { ".md", ".rtf" },
                    { ".md", ".txt" },
                    { ".mht", ".docm" },
                    { ".mht", ".docx" },
                    { ".mht", ".dotm" },
                    { ".mht", ".dotx" },
                    { ".mht", ".epub" },
                    { ".mht", ".fb2" },
                    { ".mht", ".html" },
                    { ".mht", ".odt" },
                    { ".mht", ".ott" },
                    { ".mht", ".pdf" },
                    { ".mht", ".rtf" },
                    { ".mht", ".txt" },
                    { ".mhtml", ".docm" },
                    { ".mhtml", ".docx" },
                    { ".mhtml", ".dotm" },
                    { ".mhtml", ".dotx" },
                    { ".mhtml", ".epub" },
                    { ".mhtml", ".fb2" },
                    { ".mhtml", ".html" },
                    { ".mhtml", ".odt" },
                    { ".mhtml", ".ott" },
                    { ".mhtml", ".pdf" },
                    { ".mhtml", ".rtf" },
                    { ".mhtml", ".txt" },
                    { ".numbers", ".csv" },
                    { ".numbers", ".ods" },
                    { ".numbers", ".ots" },
                    { ".numbers", ".pdf" },
                    { ".numbers", ".xlsm" },
                    { ".numbers", ".xlsx" },
                    { ".numbers", ".xltm" },
                    { ".numbers", ".xltx" },
                    { ".odg", ".odp" },
                    { ".odg", ".otp" },
                    { ".odg", ".pdf" },
                    { ".odg", ".potm" },
                    { ".odg", ".potx" },
                    { ".odg", ".ppsm" },
                    { ".odg", ".ppsx" },
                    { ".odg", ".pptm" },
                    { ".odg", ".pptx" },
                    { ".odp", ".otp" },
                    { ".odp", ".pdf" },
                    { ".odp", ".potm" },
                    { ".odp", ".potx" },
                    { ".odp", ".ppsm" },
                    { ".odp", ".ppsx" },
                    { ".odp", ".pptm" },
                    { ".odp", ".pptx" },
                    { ".ods", ".csv" },
                    { ".ods", ".ots" },
                    { ".ods", ".pdf" },
                    { ".ods", ".xlsm" },
                    { ".ods", ".xlsx" },
                    { ".ods", ".xltm" },
                    { ".ods", ".xltx" },
                    { ".odt", ".docm" },
                    { ".odt", ".docx" },
                    { ".odt", ".dotm" },
                    { ".odt", ".dotx" },
                    { ".odt", ".epub" },
                    { ".odt", ".fb2" },
                    { ".odt", ".html" },
                    { ".odt", ".ott" },
                    { ".odt", ".pdf" },
                    { ".odt", ".rtf" },
                    { ".odt", ".txt" },
                    { ".oform", ".pdf" },
                    { ".otp", ".odp" },
                    { ".otp", ".pdf" },
                    { ".otp", ".potm" },
                    { ".otp", ".potx" },
                    { ".otp", ".ppsm" },
                    { ".otp", ".ppsx" },
                    { ".otp", ".pptm" },
                    { ".otp", ".pptx" },
                    { ".ots", ".csv" },
                    { ".ots", ".ods" },
                    { ".ots", ".pdf" },
                    { ".ots", ".xlsm" },
                    { ".ots", ".xlsx" },
                    { ".ots", ".xltm" },
                    { ".ots", ".xltx" },
                    { ".ott", ".docm" },
                    { ".ott", ".docx" },
                    { ".ott", ".dotm" },
                    { ".ott", ".dotx" },
                    { ".ott", ".epub" },
                    { ".ott", ".fb2" },
                    { ".ott", ".html" },
                    { ".ott", ".odt" },
                    { ".ott", ".pdf" },
                    { ".ott", ".rtf" },
                    { ".ott", ".txt" },
                    { ".oxps", ".docm" },
                    { ".oxps", ".docx" },
                    { ".oxps", ".dotm" },
                    { ".oxps", ".dotx" },
                    { ".oxps", ".epub" },
                    { ".oxps", ".fb2" },
                    { ".oxps", ".html" },
                    { ".oxps", ".odt" },
                    { ".oxps", ".ott" },
                    { ".oxps", ".pdf" },
                    { ".oxps", ".rtf" },
                    { ".oxps", ".txt" },
                    { ".pages", ".docm" },
                    { ".pages", ".docx" },
                    { ".pages", ".dotm" },
                    { ".pages", ".dotx" },
                    { ".pages", ".epub" },
                    { ".pages", ".fb2" },
                    { ".pages", ".html" },
                    { ".pages", ".odt" },
                    { ".pages", ".ott" },
                    { ".pages", ".pdf" },
                    { ".pages", ".rtf" },
                    { ".pages", ".txt" },
                    { ".pdf", ".docm" },
                    { ".pdf", ".docx" },
                    { ".pdf", ".dotm" },
                    { ".pdf", ".dotx" },
                    { ".pdf", ".epub" },
                    { ".pdf", ".fb2" },
                    { ".pdf", ".html" },
                    { ".pdf", ".odt" },
                    { ".pdf", ".ott" },
                    { ".pdf", ".rtf" },
                    { ".pdf", ".txt" },
                    { ".pot", ".odp" },
                    { ".pot", ".otp" },
                    { ".pot", ".pdf" },
                    { ".pot", ".potm" },
                    { ".pot", ".potx" },
                    { ".pot", ".ppsm" },
                    { ".pot", ".ppsx" },
                    { ".pot", ".pptm" },
                    { ".pot", ".pptx" },
                    { ".potm", ".odp" },
                    { ".potm", ".otp" },
                    { ".potm", ".pdf" },
                    { ".potm", ".potx" },
                    { ".potm", ".ppsm" },
                    { ".potm", ".ppsx" },
                    { ".potm", ".pptm" },
                    { ".potm", ".pptx" },
                    { ".potx", ".odp" },
                    { ".potx", ".otp" },
                    { ".potx", ".pdf" },
                    { ".potx", ".potm" },
                    { ".potx", ".ppsm" },
                    { ".potx", ".ppsx" },
                    { ".potx", ".pptm" },
                    { ".potx", ".pptx" },
                    { ".pps", ".odp" },
                    { ".pps", ".otp" },
                    { ".pps", ".pdf" },
                    { ".pps", ".potm" },
                    { ".pps", ".potx" },
                    { ".pps", ".ppsm" },
                    { ".pps", ".ppsx" },
                    { ".pps", ".pptm" },
                    { ".pps", ".pptx" },
                    { ".ppsm", ".odp" },
                    { ".ppsm", ".otp" },
                    { ".ppsm", ".pdf" },
                    { ".ppsm", ".potm" },
                    { ".ppsm", ".potx" },
                    { ".ppsm", ".ppsx" },
                    { ".ppsm", ".pptm" },
                    { ".ppsm", ".pptx" },
                    { ".ppsx", ".odp" },
                    { ".ppsx", ".otp" },
                    { ".ppsx", ".pdf" },
                    { ".ppsx", ".potm" },
                    { ".ppsx", ".potx" },
                    { ".ppsx", ".ppsm" },
                    { ".ppsx", ".pptm" },
                    { ".ppsx", ".pptx" },
                    { ".ppt", ".odp" },
                    { ".ppt", ".otp" },
                    { ".ppt", ".pdf" },
                    { ".ppt", ".potm" },
                    { ".ppt", ".potx" },
                    { ".ppt", ".ppsm" },
                    { ".ppt", ".ppsx" },
                    { ".ppt", ".pptm" },
                    { ".ppt", ".pptx" },
                    { ".pptm", ".odp" },
                    { ".pptm", ".otp" },
                    { ".pptm", ".pdf" },
                    { ".pptm", ".potm" },
                    { ".pptm", ".potx" },
                    { ".pptm", ".ppsm" },
                    { ".pptm", ".ppsx" },
                    { ".pptm", ".pptx" },
                    { ".pptt", ".pptx" },
                    { ".pptx", ".odp" },
                    { ".pptx", ".otp" },
                    { ".pptx", ".pdf" },
                    { ".pptx", ".potm" },
                    { ".pptx", ".potx" },
                    { ".pptx", ".ppsm" },
                    { ".pptx", ".ppsx" },
                    { ".pptx", ".pptm" },
                    { ".rtf", ".docm" },
                    { ".rtf", ".docx" },
                    { ".rtf", ".dotm" },
                    { ".rtf", ".dotx" },
                    { ".rtf", ".epub" },
                    { ".rtf", ".fb2" },
                    { ".rtf", ".html" },
                    { ".rtf", ".odt" },
                    { ".rtf", ".ott" },
                    { ".rtf", ".pdf" },
                    { ".rtf", ".txt" },
                    { ".stw", ".docm" },
                    { ".stw", ".docx" },
                    { ".stw", ".dotm" },
                    { ".stw", ".dotx" },
                    { ".stw", ".epub" },
                    { ".stw", ".fb2" },
                    { ".stw", ".html" },
                    { ".stw", ".odt" },
                    { ".stw", ".ott" },
                    { ".stw", ".pdf" },
                    { ".stw", ".rtf" },
                    { ".stw", ".txt" },
                    { ".sxc", ".csv" },
                    { ".sxc", ".ods" },
                    { ".sxc", ".ots" },
                    { ".sxc", ".pdf" },
                    { ".sxc", ".xlsm" },
                    { ".sxc", ".xlsx" },
                    { ".sxc", ".xltm" },
                    { ".sxc", ".xltx" },
                    { ".sxi", ".odp" },
                    { ".sxi", ".otp" },
                    { ".sxi", ".pdf" },
                    { ".sxi", ".potm" },
                    { ".sxi", ".potx" },
                    { ".sxi", ".ppsm" },
                    { ".sxi", ".ppsx" },
                    { ".sxi", ".pptm" },
                    { ".sxi", ".pptx" },
                    { ".sxw", ".docm" },
                    { ".sxw", ".docx" },
                    { ".sxw", ".dotm" },
                    { ".sxw", ".dotx" },
                    { ".sxw", ".epub" },
                    { ".sxw", ".fb2" },
                    { ".sxw", ".html" },
                    { ".sxw", ".odt" },
                    { ".sxw", ".ott" },
                    { ".sxw", ".pdf" },
                    { ".sxw", ".rtf" },
                    { ".sxw", ".txt" },
                    { ".txt", ".docm" },
                    { ".txt", ".docx" },
                    { ".txt", ".dotm" },
                    { ".txt", ".dotx" },
                    { ".txt", ".epub" },
                    { ".txt", ".fb2" },
                    { ".txt", ".html" },
                    { ".txt", ".odt" },
                    { ".txt", ".ott" },
                    { ".txt", ".pdf" },
                    { ".txt", ".rtf" },
                    { ".vsdm", ".bmp" },
                    { ".vsdm", ".gif" },
                    { ".vsdm", ".jpg" },
                    { ".vsdm", ".pdf" },
                    { ".vsdm", ".pdfa" },
                    { ".vsdm", ".png" },
                    { ".vsdx", ".bmp" },
                    { ".vsdx", ".gif" },
                    { ".vsdx", ".jpg" },
                    { ".vsdx", ".pdf" },
                    { ".vsdx", ".pdfa" },
                    { ".vsdx", ".png" },
                    { ".vssm", ".bmp" },
                    { ".vssm", ".gif" },
                    { ".vssm", ".jpg" },
                    { ".vssm", ".pdf" },
                    { ".vssm", ".pdfa" },
                    { ".vssm", ".png" },
                    { ".vssx", ".bmp" },
                    { ".vssx", ".gif" },
                    { ".vssx", ".jpg" },
                    { ".vssx", ".pdf" },
                    { ".vssx", ".pdfa" },
                    { ".vssx", ".png" },
                    { ".vstm", ".bmp" },
                    { ".vstm", ".gif" },
                    { ".vstm", ".jpg" },
                    { ".vstm", ".pdf" },
                    { ".vstm", ".pdfa" },
                    { ".vstm", ".png" },
                    { ".vstx", ".bmp" },
                    { ".vstx", ".gif" },
                    { ".vstx", ".jpg" },
                    { ".vstx", ".pdf" },
                    { ".vstx", ".pdfa" },
                    { ".vstx", ".png" },
                    { ".wps", ".docm" },
                    { ".wps", ".docx" },
                    { ".wps", ".dotm" },
                    { ".wps", ".dotx" },
                    { ".wps", ".epub" },
                    { ".wps", ".fb2" },
                    { ".wps", ".html" },
                    { ".wps", ".odt" },
                    { ".wps", ".ott" },
                    { ".wps", ".pdf" },
                    { ".wps", ".rtf" },
                    { ".wps", ".txt" },
                    { ".wpt", ".docm" },
                    { ".wpt", ".docx" },
                    { ".wpt", ".dotm" },
                    { ".wpt", ".dotx" },
                    { ".wpt", ".epub" },
                    { ".wpt", ".fb2" },
                    { ".wpt", ".html" },
                    { ".wpt", ".odt" },
                    { ".wpt", ".ott" },
                    { ".wpt", ".pdf" },
                    { ".wpt", ".rtf" },
                    { ".wpt", ".txt" },
                    { ".xls", ".csv" },
                    { ".xls", ".ods" },
                    { ".xls", ".ots" },
                    { ".xls", ".pdf" },
                    { ".xls", ".xlsm" },
                    { ".xls", ".xlsx" },
                    { ".xls", ".xltm" },
                    { ".xls", ".xltx" },
                    { ".xlsb", ".csv" },
                    { ".xlsb", ".ods" },
                    { ".xlsb", ".ots" },
                    { ".xlsb", ".pdf" },
                    { ".xlsb", ".xlsm" },
                    { ".xlsb", ".xlsx" },
                    { ".xlsb", ".xltm" },
                    { ".xlsb", ".xltx" },
                    { ".xlsm", ".csv" },
                    { ".xlsm", ".ods" },
                    { ".xlsm", ".ots" },
                    { ".xlsm", ".pdf" },
                    { ".xlsm", ".xlsx" },
                    { ".xlsm", ".xltm" },
                    { ".xlsm", ".xltx" },
                    { ".xlst", ".xlsx" },
                    { ".xlsx", ".csv" },
                    { ".xlsx", ".ods" },
                    { ".xlsx", ".ots" },
                    { ".xlsx", ".pdf" },
                    { ".xlsx", ".xlsm" },
                    { ".xlsx", ".xltm" },
                    { ".xlsx", ".xltx" },
                    { ".xlt", ".csv" },
                    { ".xlt", ".ods" },
                    { ".xlt", ".ots" },
                    { ".xlt", ".pdf" },
                    { ".xlt", ".xlsm" },
                    { ".xlt", ".xlsx" },
                    { ".xlt", ".xltm" },
                    { ".xlt", ".xltx" },
                    { ".xltm", ".csv" },
                    { ".xltm", ".ods" },
                    { ".xltm", ".ots" },
                    { ".xltm", ".pdf" },
                    { ".xltm", ".xlsm" },
                    { ".xltm", ".xlsx" },
                    { ".xltm", ".xltx" },
                    { ".xltx", ".csv" },
                    { ".xltx", ".ods" },
                    { ".xltx", ".ots" },
                    { ".xltx", ".pdf" },
                    { ".xltx", ".xlsm" },
                    { ".xltx", ".xlsx" },
                    { ".xltx", ".xltm" },
                    { ".xml", ".docm" },
                    { ".xml", ".docx" },
                    { ".xml", ".dotm" },
                    { ".xml", ".dotx" },
                    { ".xml", ".epub" },
                    { ".xml", ".fb2" },
                    { ".xml", ".html" },
                    { ".xml", ".odt" },
                    { ".xml", ".ott" },
                    { ".xml", ".pdf" },
                    { ".xml", ".rtf" },
                    { ".xml", ".txt" },
                    { ".xps", ".docm" },
                    { ".xps", ".docx" },
                    { ".xps", ".dotm" },
                    { ".xps", ".dotx" },
                    { ".xps", ".epub" },
                    { ".xps", ".fb2" },
                    { ".xps", ".html" },
                    { ".xps", ".odt" },
                    { ".xps", ".ott" },
                    { ".xps", ".pdf" },
                    { ".xps", ".rtf" },
                    { ".xps", ".txt" }
                });

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,year");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic");

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
                name: "IX_tenant_id_id",
                table: "ai_providers",
                columns: new[] { "tenant_id", "id" });

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
                name: "files_converts");

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

            migrationBuilder.AddColumn<bool>(
                name: "paid",
                table: "backup_backup",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -10,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,year,free_backup:2:fixed");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -9,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:268435456000,file_size:1024,manager:1,statistic,free_backup:2:fixed");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic,free_backup:2:fixed");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,customization,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic,free_backup:2:fixed");

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "wallet" },
                values: new object[] { -12, null, "backup", "backup", 12m, "10006", true });
        }
    }
}
