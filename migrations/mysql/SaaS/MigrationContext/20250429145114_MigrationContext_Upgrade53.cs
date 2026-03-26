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
    public partial class MigrationContext_Upgrade53 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "due_date",
                table: "tenants_tariffrow",
                type: "datetime",
                nullable: true,
                defaultValueSql: "NULL");

            migrationBuilder.AddColumn<int>(
                name: "next_quantity",
                table: "tenants_tariffrow",
                type: "int",
                nullable: true,
                defaultValueSql: "NULL");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(10,4)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldDefaultValueSql: "'0.00'");

            migrationBuilder.AddColumn<bool>(
                name: "wallet",
                table: "tenants_quota",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.InsertData(
                table: "tenants_quota",
                columns: new[] { "tenant", "description", "features", "name", "price", "product_id", "visible", "wallet" },
                values: new object[] { -11, null, "total_size:1073741824", "storage", 0.14m, "1011", true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -11);

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "tenants_tariffrow");

            migrationBuilder.DropColumn(
                name: "next_quantity",
                table: "tenants_tariffrow");

            migrationBuilder.DropColumn(
                name: "wallet",
                table: "tenants_quota");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                table: "tenants_quota",
                type: "decimal(10,2)",
                nullable: false,
                defaultValueSql: "'0.00'",
                oldClrType: typeof(decimal),
                oldType: "decimal(10,4)",
                oldDefaultValueSql: "'0.00'");
        }
    }
}
