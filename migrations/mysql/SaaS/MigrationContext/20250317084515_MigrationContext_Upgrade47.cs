using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade47 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "core_user_api_key",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    key_prefix = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    hashed_key = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    permissions = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8"),
                    last_used = table.Column<DateTime>(type: "datetime", nullable: true),
                    create_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    create_by = table.Column<string>(type: "varchar(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    tenant_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "hashed_key",
                table: "core_user_api_key",
                columns: new[] { "tenant_id", "hashed_key" });

            migrationBuilder.CreateIndex(
                name: "is_active",
                table: "core_user_api_key",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "core_user_api_key");
        }
    }
}
