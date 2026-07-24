using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade90 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files_metadata_field",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    type = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'0'"),
                    options = table.Column<string>(type: "json", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    order = table.Column<int>(type: "int", nullable: false, defaultValueSql: "'0'"),
                    create_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    create_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_metadata_field", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_metadata_field_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_metadata_link",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    template_id = table.Column<int>(type: "int", nullable: false),
                    entry_id = table.Column<int>(type: "int", nullable: false),
                    entry_type = table.Column<int>(type: "int", nullable: false),
                    is_cascade = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    source_folder_id = table.Column<int>(type: "int", nullable: true),
                    create_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    create_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.template_id, x.entry_id, x.entry_type });
                    table.ForeignKey(
                        name: "FK_files_metadata_link_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_metadata_template",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    visible = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'1'"),
                    is_system = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "'0'"),
                    create_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    create_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files_metadata_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_files_metadata_template_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_metadata_value",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    entry_id = table.Column<int>(type: "int", nullable: false),
                    entry_type = table.Column<int>(type: "int", nullable: false),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    option_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false, defaultValueSql: "''", collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    value_string = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    value_number = table.Column<long>(type: "bigint", nullable: true),
                    value_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    create_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    create_on = table.Column<DateTime>(type: "datetime", nullable: false),
                    modified_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    modified_on = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.entry_id, x.entry_type, x.field_id, x.option_id });
                    table.ForeignKey(
                        name: "FK_files_metadata_value_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "tenant_id_template_id",
                table: "files_metadata_field",
                columns: new[] { "tenant_id", "template_id" });

            migrationBuilder.CreateIndex(
                name: "entry_id",
                table: "files_metadata_link",
                columns: new[] { "tenant_id", "entry_id", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "source_folder_id",
                table: "files_metadata_link",
                columns: new[] { "tenant_id", "source_folder_id" });

            migrationBuilder.CreateIndex(
                name: "tenant_id_name",
                table: "files_metadata_template",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "field_id_option_id",
                table: "files_metadata_value",
                columns: new[] { "tenant_id", "field_id", "option_id" });

            migrationBuilder.CreateIndex(
                name: "field_id_value_date",
                table: "files_metadata_value",
                columns: new[] { "tenant_id", "field_id", "value_date" });

            migrationBuilder.CreateIndex(
                name: "field_id_value_number",
                table: "files_metadata_value",
                columns: new[] { "tenant_id", "field_id", "value_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "files_metadata_field");

            migrationBuilder.DropTable(
                name: "files_metadata_link");

            migrationBuilder.DropTable(
                name: "files_metadata_template");

            migrationBuilder.DropTable(
                name: "files_metadata_value");
        }
    }
}
