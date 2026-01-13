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