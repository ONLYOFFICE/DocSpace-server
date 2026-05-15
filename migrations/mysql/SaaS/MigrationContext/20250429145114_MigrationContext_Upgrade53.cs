// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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
