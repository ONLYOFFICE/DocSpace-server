using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade86 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_properties",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_thirdparty_form_role_mapping",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    form_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8"),
                    user_id = table.Column<string>(type: "varchar(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    role_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    room_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8"),
                    role_color = table.Column<string>(type: "char(6)", maxLength: 6, nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    sequence = table.Column<int>(type: "int", nullable: false),
                    opened_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    submission_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    submitted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.form_id, x.role_name, x.user_id });
                    table.ForeignKey(
                        name: "FK_files_thirdparty_form_role_mapping_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_thirdparty_room_settings",
                columns: table => new
                {
                    hash_id = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    indexing = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    deny_download = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    watermark = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    lifetime = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    send_form_to_external_db = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    save_form_as_xlsx = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("primary", x => new { x.tenant_id, x.hash_id });
                    table.ForeignKey(
                        name: "FK_files_thirdparty_room_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "tenant_id_form_id",
                table: "files_thirdparty_form_role_mapping",
                columns: new[] { "tenant_id", "form_id" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_form_id_user_id",
                table: "files_thirdparty_form_role_mapping",
                columns: new[] { "tenant_id", "form_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_thirdparty_form_role_mapping");

            migrationBuilder.DropTable(
                name: "files_thirdparty_room_settings");

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_properties",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                collation: "utf8_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldCollation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8")
                .OldAnnotation("MySql:CharSet", "utf8");
        }
    }
}
