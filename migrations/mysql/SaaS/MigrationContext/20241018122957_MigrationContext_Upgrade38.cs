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
    public partial class MigrationContext_Upgrade38 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "spam",
                table: "core_user",
                type: "tinyint(1)",
                nullable: true);

            /*
             migrationBuilder.UpdateData(
                 table: "core_user",
                 keyColumn: "id",
                 keyValue: "66faa6e4-f133-11ea-b126-00ffeec8b4ef",
                 column: "spam",
                 value: null);
            */

            migrationBuilder.Sql("UPDATE core_user JOIN tenants_tenants ON core_user.id = tenants_tenants.owner_id SET core_user.spam = tenants_tenants.spam");

            migrationBuilder.DropColumn(
                name: "spam",
                table: "tenants_tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "spam",
                table: "tenants_tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValueSql: "'1'");

            /*
            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: -1,
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.UpdateData(
                table: "tenants_tenants",
                keyColumn: "id",
                keyValue: 1,
                columns: new string[0],
                values: new object[0]);
            */

            migrationBuilder.Sql("UPDATE tenants_tenants JOIN core_user ON tenants_tenants.owner_id = core_user.id SET tenants_tenants.spam = core_user.spam");

            migrationBuilder.DropColumn(
                name: "spam",
                table: "core_user");
        }
    }
}