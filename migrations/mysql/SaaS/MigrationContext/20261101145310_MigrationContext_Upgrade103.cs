using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade103 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files_ai_project_room",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.project_id, x.room_id });
                    table.ForeignKey(
                        name: "FK_files_ai_project_room_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_skill",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    file_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.file_id });
                    table.ForeignKey(
                        name: "FK_files_skill_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_id_room_id",
                table: "files_ai_project_room",
                columns: new[] { "tenant_id", "room_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_ai_project_room");

            migrationBuilder.DropTable(
                name: "files_skill");
        }
    }
}
