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
    public partial class MigrationContext_Upgrade42 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_authorization_client_id",
                table: "identity_authorizations");

            migrationBuilder.DropForeignKey(
                name: "identity_consents_ibfk_1",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_is_invalidated",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents");

            migrationBuilder.DropIndex(
                name: "idx_identity_clients_is_invalidated",
                table: "identity_clients");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_grant_type",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_is_invalidated",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_principal_id",
                table: "identity_authorizations");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations");

            migrationBuilder.RenameColumn(
                name: "scope_name",
                table: "identity_consent_scopes",
                newName: "scopes");

            migrationBuilder.RenameIndex(
                name: "idx_identity_consent_scopes_scope_name",
                table: "identity_consent_scopes",
                newName: "idx_identity_consent_scopes_scopes");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_client_secret",
                table: "identity_clients",
                column: "client_secret");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_id",
                table: "identity_authorizations",
                column: "id");

            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_authorizations\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_authorizations\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");
            migrationBuilder.Sql("CREATE EVENT IF NOT EXISTS identity_delete_old_consents\r\nON SCHEDULE EVERY 1 DAY\r\nON COMPLETION PRESERVE\r\nDO\r\nDELETE FROM identity_consents\r\nWHERE modified_at < NOW() - INTERVAL 30 DAY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_client_secret",
                table: "identity_clients");

            migrationBuilder.DropIndex(
                name: "idx_identity_authorizations_id",
                table: "identity_authorizations");

            migrationBuilder.RenameColumn(
                name: "scopes",
                table: "identity_consent_scopes",
                newName: "scope_name");

            migrationBuilder.RenameIndex(
                name: "idx_identity_consent_scopes_scopes",
                table: "identity_consent_scopes",
                newName: "idx_identity_consent_scopes_scope_name");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "varchar(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_is_invalidated",
                table: "identity_consents",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_principal_id",
                table: "identity_consents",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_consents_registered_client_id",
                table: "identity_consents",
                column: "registered_client_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_clients_is_invalidated",
                table: "identity_clients",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_grant_type",
                table: "identity_authorizations",
                column: "authorization_grant_type");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_is_invalidated",
                table: "identity_authorizations",
                column: "is_invalidated");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_principal_id",
                table: "identity_authorizations",
                column: "principal_id");

            migrationBuilder.CreateIndex(
                name: "idx_identity_authorizations_registered_client_id",
                table: "identity_authorizations",
                column: "registered_client_id");

            migrationBuilder.AddForeignKey(
                name: "FK_authorization_client_id",
                table: "identity_authorizations",
                column: "registered_client_id",
                principalTable: "identity_clients",
                principalColumn: "client_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "identity_consents_ibfk_1",
                table: "identity_consents",
                column: "registered_client_id",
                principalTable: "identity_clients",
                principalColumn: "client_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}