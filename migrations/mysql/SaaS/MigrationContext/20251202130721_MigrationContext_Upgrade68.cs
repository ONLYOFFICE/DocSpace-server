using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade68 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files_group",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    icon = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    user_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_group_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_roomgroup",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    group_id = table.Column<int>(type: "int", nullable: false),
                    internal_room_id = table.Column<int>(type: "int", nullable: true),
                    thirdparty_room_id = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_roomgroup", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_roomgroup_files_folder_internal_room_id",
                        column: x => x.internal_room_id,
                        principalTable: "files_folder",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_files_roomgroup_files_group_group_id",
                        column: x => x.group_id,
                        principalTable: "files_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_files_roomgroup_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "IX_files_group_tenant_id",
                table: "files_group",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_group",
                table: "files_roomgroup",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "idx_internal_room",
                table: "files_roomgroup",
                column: "internal_room_id");

            migrationBuilder.CreateIndex(
                name: "idx_thirdparty_room",
                table: "files_roomgroup",
                column: "thirdparty_room_id");

            migrationBuilder.CreateIndex(
                name: "uq_roomgroup_internal",
                table: "files_roomgroup",
                columns: new[] { "tenant_id", "group_id", "internal_room_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_roomgroup_thirdparty",
                table: "files_roomgroup",
                columns: new[] { "tenant_id", "group_id", "thirdparty_room_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_roomgroup");

            migrationBuilder.DropTable(
                name: "files_group");
        }
    }
}
