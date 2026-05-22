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
    public partial class MigrationContext_Upgrade60 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "admin_notify", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "periodic_notify", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "send_whats_new", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharefolder", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "updatedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender|telegram.sender");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "admin_notify", "cd84e66b-b803-40fc-99f9-b2969a54a1de", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "periodic_notify", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "send_whats_new", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "asc.web.studio", -1 },
                column: "sender",
                value: "email.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "sharefolder", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");

            migrationBuilder.UpdateData(
                table: "core_subscriptionmethod",
                keyColumns: new[] { "action", "recipient", "source", "tenant" },
                keyValues: new object[] { "updatedocument", "c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e", "6fe286a4-479e-4c25-a8d9-0156e332b0c0", -1 },
                column: "sender",
                value: "email.sender|messanger.sender");
        }
    }
}