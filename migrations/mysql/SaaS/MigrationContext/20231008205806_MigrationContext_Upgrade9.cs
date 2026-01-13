// (c) Copyright Ascensio System SIA 2009-2026
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "files_order",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    entry_id = table.Column<int>(type: "int", nullable: false),
                    entry_type = table.Column<sbyte>(type: "tinyint", nullable: false),
                    parent_folder_id = table.Column<int>(type: "int", nullable: false),
                    order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("primary", x => new { x.tenant_id, x.entry_id, x.entry_type });
                    table.ForeignKey(
                        name: "FK_files_order_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "files_room_settings",
                columns: table => new
                {
                    room_id = table.Column<int>(type: "int", nullable: false),
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    @private = table.Column<bool>(name: "private", type: "tinyint(1)", nullable: false, defaultValueSql: "0"),
                    has_logo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "0"),
                    color = table.Column<string>(type: "char(6)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    indexing = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("primary", x => new { x.tenant_id, x.room_id });
                    table.ForeignKey(
                        name: "FK_files_room_settings_files_folder_room_id",
                        column: x => x.room_id,
                        principalTable: "files_folder",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_files_room_settings_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "parent_folder_id",
                table: "files_order",
                columns: new[] { "tenant_id", "parent_folder_id", "entry_type" });

            migrationBuilder.CreateIndex(
                name: "IX_files_room_settings_room_id",
                table: "files_room_settings",
                column: "room_id",
                unique: true);

            migrationBuilder.Sql("insert into files_room_settings (room_id, tenant_id, private, has_logo, color) select id, tenant_id, private, has_logo, color from files_folder where folder_type in (15,16,17,18,19,22)");

            migrationBuilder.DropColumn(
                name: "color",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "has_logo",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "private",
                table: "files_folder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "files_folder",
                type: "char(6)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<bool>(
                name: "has_logo",
                table: "files_folder",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "private",
                table: "files_folder",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "0");

            migrationBuilder.DropTable(
                name: "files_order");

            migrationBuilder.DropTable(
                name: "files_room_settings");
        }
    }
}