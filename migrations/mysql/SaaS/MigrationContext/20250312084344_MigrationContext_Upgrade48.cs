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
    public partial class MigrationContext_Upgrade48 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "trigger",
                table: "webhooks_logs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "webhooks_config",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_failure_content",
                table: "webhooks_config",
                type: "varchar(200)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_failure_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_success_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "webhooks_config",
                type: "varchar(36)",
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");

            migrationBuilder.AddColumn<DateTime>(
                name: "modified_on",
                table: "webhooks_config",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "triggers",
                table: "webhooks_config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "target_id",
                table: "webhooks_config",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true,
                collation: "utf8_general_ci")
                .Annotation("MySql:CharSet", "utf8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trigger",
                table: "webhooks_logs");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "created_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_failure_content",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_failure_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "last_success_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "modified_on",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "triggers",
                table: "webhooks_config");

            migrationBuilder.DropColumn(
                name: "target_id",
                table: "webhooks_config");
        }
    }
}