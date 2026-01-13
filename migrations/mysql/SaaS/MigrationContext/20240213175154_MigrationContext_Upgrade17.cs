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
    public partial class MigrationContext_Upgrade17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "features",
                value: "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:1024,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1,statistic");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "features",
                value: "trial,audit,ldap,sso,whitelabel,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1,statistic");

            migrationBuilder.AddColumn<long>(
                name: "counter",
                table: "files_folder",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'0'");

            migrationBuilder.AddColumn<long>(
                name: "quota",
                table: "files_room_settings",
                type: "bigint",
                nullable: false,
                defaultValueSql: "'-2'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -7,
                column: "features",
                value: "non-profit,audit,ldap,sso,thirdparty,restore,oauth,contentsearch,total_size:2147483648,file_size:1024,manager:20");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -6,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,file_size:1024");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -2,
                column: "features",
                value: "audit,ldap,sso,whitelabel,thirdparty,restore,oauth,contentsearch,total_size:107374182400,file_size:1024,manager:1");

            migrationBuilder.UpdateData(
                table: "tenants_quota",
                keyColumn: "tenant",
                keyValue: -1,
                column: "features",
                value: "trial,audit,ldap,sso,whitelabel,thirdparty,restore,oauth,total_size:107374182400,file_size:100,manager:1");

            migrationBuilder.DropColumn(
                name: "counter",
                table: "files_folder");

            migrationBuilder.DropColumn(
                name: "quota",
                table: "files_room_settings");
        }
    }
}