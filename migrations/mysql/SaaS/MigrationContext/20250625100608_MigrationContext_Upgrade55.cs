using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade55 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_chat",
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
                        name: "FK_ai_chat_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

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
                    url = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
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
                name: "ai_chat_messages",
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
                        name: "FK_ai_chat_messages_ai_chat_chat_id",
                        column: x => x.chat_id,
                        principalTable: "ai_chat",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_id",
                table: "ai_chat",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_room_id_user_id_modified_on",
                table: "ai_chat",
                columns: new[] { "tenant_id", "room_id", "user_id", "modified_on" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_chat_messages_chat_id",
                table: "ai_chat_messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_id",
                table: "ai_providers",
                columns: new[] { "tenant_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_chat_messages");

            migrationBuilder.DropTable(
                name: "ai_providers");

            migrationBuilder.DropTable(
                name: "ai_chat");
        }
    }
}
