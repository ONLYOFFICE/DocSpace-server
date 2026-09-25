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

namespace ASC.Migrations.MySql.Migrations.CoreDb;
/// <inheritdoc />
public partial class MigrationContext_Upgrade5 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // add missing primary and foreign keys to identity client dependency tables:
        // the tables have no key, so duplicates are dropped by copying into a keyed table,
        // and the old table is dropped together with its foreign keys, whatever they are named

        migrationBuilder.CreateTable(
            name: "identity_client_authentication_methods_new",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                authentication_method = table.Column<string>(type: "enum('client_secret_post','none')", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PRIMARY", x => new { x.client_id, x.authentication_method });
            });

        migrationBuilder.Sql(@"
            INSERT IGNORE INTO identity_client_authentication_methods_new (client_id, authentication_method)
            SELECT client_id, authentication_method FROM identity_client_authentication_methods;");

        migrationBuilder.DropTable(
            name: "identity_client_authentication_methods");

        migrationBuilder.RenameTable(
            name: "identity_client_authentication_methods_new",
            newName: "identity_client_authentication_methods");

        migrationBuilder.AddForeignKey(
            name: "FK_identity_client_authentication_methods_client_id",
            table: "identity_client_authentication_methods",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_redirect_uris_new",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                redirect_uri = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PRIMARY", x => new { x.client_id, x.redirect_uri });
            });

        migrationBuilder.Sql(@"
            INSERT IGNORE INTO identity_client_redirect_uris_new (client_id, redirect_uri)
            SELECT client_id, redirect_uri FROM identity_client_redirect_uris;");

        migrationBuilder.DropTable(
            name: "identity_client_redirect_uris");

        migrationBuilder.RenameTable(
            name: "identity_client_redirect_uris_new",
            newName: "identity_client_redirect_uris");

        migrationBuilder.AddForeignKey(
            name: "FK_identity_client_redirect_uris_client_id",
            table: "identity_client_redirect_uris",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_allowed_origins_new",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                allowed_origin = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_0900_bin")
                    .Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PRIMARY", x => new { x.client_id, x.allowed_origin });
            });

        migrationBuilder.Sql(@"
            INSERT IGNORE INTO identity_client_allowed_origins_new (client_id, allowed_origin)
            SELECT client_id, allowed_origin FROM identity_client_allowed_origins;");

        migrationBuilder.DropTable(
            name: "identity_client_allowed_origins");

        migrationBuilder.RenameTable(
            name: "identity_client_allowed_origins_new",
            newName: "identity_client_allowed_origins");

        migrationBuilder.AddForeignKey(
            name: "FK_identity_client_allowed_origins_client_id",
            table: "identity_client_allowed_origins",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_scopes_new",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                scope_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PRIMARY", x => new { x.client_id, x.scope_name });
            });

        migrationBuilder.CreateIndex(
            name: "idx_identity_client_scopes_scope_name",
            table: "identity_client_scopes_new",
            column: "scope_name");

        migrationBuilder.Sql(@"
            INSERT IGNORE INTO identity_client_scopes_new (client_id, scope_name)
            SELECT client_id, scope_name FROM identity_client_scopes;");

        migrationBuilder.DropTable(
            name: "identity_client_scopes");

        migrationBuilder.RenameTable(
            name: "identity_client_scopes_new",
            newName: "identity_client_scopes");

        migrationBuilder.AddForeignKey(
            name: "FK_identity_client_scopes_client_id",
            table: "identity_client_scopes",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_identity_client_scopes_scope_name",
            table: "identity_client_scopes",
            column: "scope_name",
            principalTable: "identity_scopes",
            principalColumn: "name",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // restore the keyless tables with the names, types and indexes created by MigrationContext_Upgrade33
        migrationBuilder.CreateTable(
            name: "identity_client_authentication_methods_old",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                authentication_method = table.Column<string>(type: "enum('client_secret_post','none')", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "idx_client_authentication_methods_client_id",
            table: "identity_client_authentication_methods_old",
            column: "client_id");

        migrationBuilder.Sql(@"
            INSERT INTO identity_client_authentication_methods_old (client_id, authentication_method)
            SELECT client_id, authentication_method FROM identity_client_authentication_methods;");

        migrationBuilder.DropTable(
            name: "identity_client_authentication_methods");

        migrationBuilder.RenameTable(
            name: "identity_client_authentication_methods_old",
            newName: "identity_client_authentication_methods");

        migrationBuilder.AddForeignKey(
            name: "identity_client_authentication_methods_ibfk_1",
            table: "identity_client_authentication_methods",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_redirect_uris_old",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                redirect_uri = table.Column<string>(type: "tinytext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "idx_identity_client_redirect_uris_client_id",
            table: "identity_client_redirect_uris_old",
            column: "client_id");

        migrationBuilder.Sql(@"
            INSERT INTO identity_client_redirect_uris_old (client_id, redirect_uri)
            SELECT client_id, redirect_uri FROM identity_client_redirect_uris;");

        migrationBuilder.DropTable(
            name: "identity_client_redirect_uris");

        migrationBuilder.RenameTable(
            name: "identity_client_redirect_uris_old",
            newName: "identity_client_redirect_uris");

        migrationBuilder.AddForeignKey(
            name: "identity_client_redirect_uris_ibfk_1",
            table: "identity_client_redirect_uris",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_allowed_origins_old",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                allowed_origin = table.Column<string>(type: "tinytext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "idx_identity_client_allowed_origins_client_id",
            table: "identity_client_allowed_origins_old",
            column: "client_id");

        migrationBuilder.Sql(@"
            INSERT INTO identity_client_allowed_origins_old (client_id, allowed_origin)
            SELECT client_id, allowed_origin FROM identity_client_allowed_origins;");

        migrationBuilder.DropTable(
            name: "identity_client_allowed_origins");

        migrationBuilder.RenameTable(
            name: "identity_client_allowed_origins_old",
            newName: "identity_client_allowed_origins");

        migrationBuilder.AddForeignKey(
            name: "identity_client_allowed_origins_ibfk_1",
            table: "identity_client_allowed_origins",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateTable(
            name: "identity_client_scopes_old",
            columns: table => new
            {
                client_id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                scope_name = table.Column<string>(type: "varchar(255)", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4")
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "idx_identity_client_scopes_client_id",
            table: "identity_client_scopes_old",
            column: "client_id");

        migrationBuilder.CreateIndex(
            name: "idx_identity_client_scopes_scope_name",
            table: "identity_client_scopes_old",
            column: "scope_name");

        migrationBuilder.Sql(@"
            INSERT INTO identity_client_scopes_old (client_id, scope_name)
            SELECT client_id, scope_name FROM identity_client_scopes;");

        migrationBuilder.DropTable(
            name: "identity_client_scopes");

        migrationBuilder.RenameTable(
            name: "identity_client_scopes_old",
            newName: "identity_client_scopes");

        migrationBuilder.AddForeignKey(
            name: "identity_client_scopes_ibfk_1",
            table: "identity_client_scopes",
            column: "client_id",
            principalTable: "identity_clients",
            principalColumn: "client_id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "identity_client_scopes_ibfk_2",
            table: "identity_client_scopes",
            column: "scope_name",
            principalTable: "identity_scopes",
            principalColumn: "name",
            onDelete: ReferentialAction.Cascade);
    }
}
