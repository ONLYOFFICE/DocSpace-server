using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade75 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_on",
                table: "ai_chats",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "files_chat_message_attachment",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    chat_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    file_id = table.Column<int>(type: "int", nullable: false),
                    message_id = table.Column<long>(type: "bigint", nullable: true),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.chat_id, x.file_id });
                    table.ForeignKey(
                        name: "FK_files_chat_message_attachment_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8")
                .Annotation("Relational:Collation", "utf8_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_deleted_on",
                table: "ai_chats",
                column: "deleted_on");

            migrationBuilder.CreateIndex(
                name: "IX_chat_id",
                table: "files_chat_message_attachment",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "IX_file_id",
                table: "files_chat_message_attachment",
                column: "file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_chat_message_attachment");

            migrationBuilder.DropIndex(
                name: "IX_deleted_on",
                table: "ai_chats");

            migrationBuilder.DropColumn(
                name: "deleted_on",
                table: "ai_chats");
        }
    }
}
