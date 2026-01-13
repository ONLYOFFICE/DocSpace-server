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

using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.MySql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feed_last");

            migrationBuilder.DropTable(
                name: "feed_readed");

            migrationBuilder.DropTable(
                name: "feed_users");

            migrationBuilder.DropTable(
                name: "feed_aggregate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feed_aggregate",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(88)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    tenant = table.Column<int>(type: "int", nullable: false),
                    aggregated_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    author = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    context_id = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    created_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    group_id = table.Column<string>(type: "varchar(70)", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    json = table.Column<string>(type: "mediumtext", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    keywords = table.Column<string>(type: "text", nullable: true, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    modified_by = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    modified_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    module = table.Column<string>(type: "varchar(50)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    product = table.Column<string>(type: "varchar(50)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_aggregate", x => x.id);
                    table.ForeignKey(
                        name: "FK_feed_aggregate_tenants_tenants_tenant",
                        column: x => x.tenant,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "feed_last",
                columns: table => new
                {
                    last_key = table.Column<string>(type: "varchar(128)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    last_date = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.last_key);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "feed_readed",
                columns: table => new
                {
                    tenant_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<string>(type: "varchar(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    module = table.Column<string>(type: "varchar(50)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.tenant_id, x.user_id, x.module });
                    table.ForeignKey(
                        name: "FK_feed_readed_tenants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants_tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateTable(
                name: "feed_users",
                columns: table => new
                {
                    feed_id = table.Column<string>(type: "varchar(88)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8"),
                    user_id = table.Column<string>(type: "char(38)", nullable: false, collation: "utf8_general_ci")
                        .Annotation("MySql:CharSet", "utf8")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.feed_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_feed_users_feed_aggregate_feed_id",
                        column: x => x.feed_id,
                        principalTable: "feed_aggregate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.CreateIndex(
                name: "aggregated_date",
                table: "feed_aggregate",
                columns: new[] { "tenant", "aggregated_date" });

            migrationBuilder.CreateIndex(
                name: "modified_date",
                table: "feed_aggregate",
                columns: new[] { "tenant", "modified_date" });

            migrationBuilder.CreateIndex(
                name: "product",
                table: "feed_aggregate",
                columns: new[] { "tenant", "product" });

            migrationBuilder.CreateIndex(
                name: "user_id",
                table: "feed_users",
                column: "user_id");
        }
    }
}