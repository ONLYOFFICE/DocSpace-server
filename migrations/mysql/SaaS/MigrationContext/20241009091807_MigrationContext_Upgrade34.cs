using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade34 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "core_user_relations",
                columns: table => new
                {
                    tenant = table.Column<int>(type: "int", nullable: false),
                    source_user_id = table.Column<string>(type: "varchar(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    target_user_id = table.Column<string>(type: "varchar(36)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant, x.source_user_id, x.target_user_id });
                    table.ForeignKey(
                        name: "FK_core_user_relations_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "core_acl",
                columns: new[] { "action", "object", "subject", "tenant", "acetype" },
                values: new object[] { "3e74aff2-7c0c-4089-b209-6495b8643471", "", "88f11e7c-7407-4bea-b4cb-070010cdbb6b", -1, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "core_user_relations");

            migrationBuilder.DeleteData(
                table: "core_acl",
                keyColumns: new[] { "action", "object", "subject", "tenant" },
                keyValues: new object[] { "3e74aff2-7c0c-4089-b209-6495b8643471", "", "88f11e7c-7407-4bea-b4cb-070010cdbb6b", -1 });
        }
    }
}
