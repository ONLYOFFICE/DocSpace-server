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

namespace ASC.Migrations.PostgreSql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            DropDependentObjects(migrationBuilder);

            migrationBuilder.AlterColumn<string>(
                name: "index_name",
                table: "webstudio_index",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "webhooks_logs",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_payload",
                table: "webhooks_logs",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "uri",
                table: "webhooks_config",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "target_id",
                table: "webhooks_config",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "secret_key",
                table: "webhooks_config",
                type: "citext",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValueSql: "''");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "webhooks_config",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "last_failure_content",
                table: "webhooks_config",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "route",
                table: "webhooks",
                type: "citext",
                maxLength: 200,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "method",
                table: "webhooks",
                type: "citext",
                maxLength: 10,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "version",
                table: "tenants_version",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "tenants_version",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "trusteddomains",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_id",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 38,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 38,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "mappeddomain",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 10,
                nullable: false,
                defaultValueSql: "'en-US'",
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldDefaultValueSql: "'en-US'");

            migrationBuilder.AlterColumn<string>(
                name: "alias",
                table: "tenants_tenants",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "customer_id",
                table: "tenants_tariff",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "tenants_tariff",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tag",
                table: "tenants_quotarow",
                type: "citext",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "tenants_quotarow",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "service_name",
                table: "tenants_quota",
                type: "citext",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "service_group",
                table: "tenants_quota",
                type: "citext",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "product_id",
                table: "tenants_quota",
                type: "citext",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "tenants_quota",
                type: "citext",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "features",
                table: "tenants_quota",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "tenants_quota",
                type: "citext",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "partner_id",
                table: "tenants_partners",
                type: "citext",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "campaign",
                table: "tenants_partners",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "affiliate_id",
                table: "tenants_partners",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "tenants_iprestrictions",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "tenants_forbiden",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "telegram_username",
                table: "telegram_users",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(35)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "short",
                table: "short_links",
                type: "citext",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "Regions",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                table: "Regions",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "Regions",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                table: "notify_queue",
                type: "citext",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender_type",
                table: "notify_queue",
                type: "citext",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender",
                table: "notify_queue",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reply_to",
                table: "notify_queue",
                type: "citext",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reciever",
                table: "notify_queue",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content_type",
                table: "notify_queue",
                type: "citext",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "notify_queue",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "auto_submitted",
                table: "notify_queue",
                type: "citext",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attachments",
                table: "notify_queue",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_email",
                table: "mobile_app_install",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "platform",
                table: "login_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "page",
                table: "login_events",
                type: "citext",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "login",
                table: "login_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "login_events",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "login_events",
                type: "citext",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "login_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "locked_by",
                table: "identity_shedlock",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_shedlock",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "identity_scopes",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "group",
                table: "identity_scopes",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_scopes",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_purge_cursors",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "routing_key",
                table: "identity_pending_messages",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "headers",
                table: "identity_pending_messages",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "exchange_name",
                table: "identity_pending_messages",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "owner_user_id",
                table: "identity_consents",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_consents",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_consents",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "scopes",
                table: "identity_consent_scopes",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_consent_scopes",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_consent_scopes",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "website_url",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "terms_url",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "policy_url",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_clients",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "modified_by",
                table: "identity_clients",
                type: "citext",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "logout_redirect_uri",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "logo",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "identity_clients",
                type: "citext",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "identity_clients",
                type: "citext",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "client_secret",
                table: "identity_clients",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_clients",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "scope_name",
                table: "identity_client_scopes",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_scopes",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "redirect_uri",
                table: "identity_client_redirect_uris",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_redirect_uris",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_authentication_methods",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "authentication_method",
                table: "identity_client_authentication_methods",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_allowed_origins",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "allowed_origin",
                table: "identity_client_allowed_origins",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "public_key",
                table: "identity_certs",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "private_key",
                table: "identity_certs",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "identity_certs",
                type: "citext",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_value",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_metadata",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_hash",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "owner_user_id",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_value",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_metadata",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_claims",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorized_scopes",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_code_value",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_code_metadata",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attributes",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_value",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_type",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_scopes",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_metadata",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_hash",
                table: "identity_authorizations",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_grant_type",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_authorizations",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "worker_type_name",
                table: "hosting_instance_registration",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "instance_registration_id",
                table: "hosting_instance_registration",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "firebase_device_token",
                table: "firebase_users",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "application",
                table: "firebase_users",
                type: "citext",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "files_thirdparty_id_mapping",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "hash_id",
                table: "files_thirdparty_id_mapping",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "files_thirdparty_app",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "app",
                table: "files_thirdparty_app",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying (50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "files_thirdparty_account",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "files_thirdparty_account",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "folder_id",
                table: "files_thirdparty_account",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_title",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "cover",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "files_thirdparty_account",
                type: "citext",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_tag_link",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_tag",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_skill",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "files_skill",
                type: "citext",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_security",
                type: "citext",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "thirdparty_room_id",
                table: "files_roomgroup",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cover",
                table: "files_room_settings",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "files_room_settings",
                type: "citext",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "data",
                table: "files_properties",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_properties",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "value_string",
                table: "files_metadata_value",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "option_id",
                table: "files_metadata_value",
                type: "citext",
                maxLength: 36,
                nullable: false,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldMaxLength: 36,
                oldDefaultValueSql: "''");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_metadata_template",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_metadata_field",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "linked_id",
                table: "files_link",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "source_id",
                table: "files_link",
                type: "citext",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_group",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                table: "files_group",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_color",
                table: "files_form_role_mapping",
                type: "citext",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                table: "files_form_role_mapping",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "files_folder",
                type: "citext",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "private_key_enc",
                table: "files_file_keys",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "files_file",
                type: "citext",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "converted_type",
                table: "files_file",
                type: "citext",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "files_file",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "changes",
                table: "files_file",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "left_node",
                table: "files_bunch_objects",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "right_node",
                table: "files_bunch_objects",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "event_type_name",
                table: "event_bus_integration_event_log",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "event_bus_integration_event_log",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "event_bus_integration_event_log",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "zipcode",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "weather_code",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "timezone_name",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "stateprov_code",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "stateprov",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "district",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "continent",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "dbip_lookup",
                type: "citext",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "addr_type",
                table: "dbip_lookup",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "pwdhash",
                table: "core_usersecurity",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "permissions",
                table: "core_user_api_key",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "core_user_api_key",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key_postfix",
                table: "core_user_api_key",
                type: "citext",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 4);

            migrationBuilder.AlterColumn<string>(
                name: "hashed_key",
                table: "core_user_api_key",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "core_user",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "core_user",
                type: "citext",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sso_session_id",
                table: "core_user",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sso_name_id",
                table: "core_user",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sid",
                table: "core_user",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "core_user",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "core_user",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "core_user",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lastname",
                table: "core_user",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "firstname",
                table: "core_user",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "core_user",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "culture",
                table: "core_user",
                type: "citext",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contacts",
                table: "core_user",
                type: "citext",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender",
                table: "core_subscriptionmethod",
                type: "citext",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "core_subscriptionmethod",
                type: "citext",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_subscriptionmethod",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "core_subscriptionmethod",
                type: "citext",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "object",
                table: "core_subscription",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "core_subscription",
                type: "citext",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_subscription",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "core_subscription",
                type: "citext",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_settings",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "sid",
                table: "core_group",
                type: "citext",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "core_group",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "object",
                table: "core_acl",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_params",
                table: "backup_schedule",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "storage_base_path",
                table: "backup_schedule",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cron",
                table: "backup_schedule",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_path",
                table: "backup_backup",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_params",
                table: "backup_backup",
                type: "citext",
                nullable: true,
                defaultValueSql: "NULL",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValueSql: "NULL");

            migrationBuilder.AlterColumn<string>(
                name: "storage_base_path",
                table: "backup_backup",
                type: "citext",
                maxLength: 255,
                nullable: true,
                defaultValueSql: "NULL",
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255,
                oldNullable: true,
                oldDefaultValueSql: "NULL");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "backup_backup",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "backup_backup",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "target",
                table: "audit_events",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "platform",
                table: "audit_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "page",
                table: "audit_events",
                type: "citext",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "audit_events",
                type: "citext",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "initiator",
                table: "audit_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "audit_events",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "audit_events",
                type: "citext",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "app_settings",
                type: "citext",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "default_model",
                table: "ai_providers_default",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "ai_providers",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_providers",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_providers",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "alias",
                table: "ai_model_settings",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "model_id",
                table: "ai_model_settings",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_mcp_servers",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "headers",
                table: "ai_mcp_servers",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "endpoint",
                table: "ai_mcp_servers",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "ai_mcp_servers",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "oauth_credentials",
                table: "ai_mcp_server_settings",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "server_type",
                table: "ai_integration_tool_preferences",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_integration_threads",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "ai_integration_prompts",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_prompts",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_prompt_folders",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "provider_type",
                table: "ai_integration_profiles",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_profiles",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "model_id",
                table: "ai_integration_profiles",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_integration_profiles",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "base_url",
                table: "ai_integration_profiles",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_mcp_servers",
                type: "citext",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "config",
                table: "ai_integration_mcp_servers",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "ai_integration_byok_keys",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_integration_byok_keys",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_integration_attachments",
                type: "citext",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "thirdparty_entry_id",
                table: "ai_integration_attachments",
                type: "citext",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "char(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "ai_integration_attachments",
                type: "citext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_chats",
                type: "citext",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "account_links",
                type: "citext",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "profile",
                table: "account_links",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "uid",
                table: "account_links",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "account_links",
                type: "citext",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar",
                oldMaxLength: 200);

            AddDependentObjects(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            DropDependentObjects(migrationBuilder);

            migrationBuilder.AlterColumn<string>(
                name: "index_name",
                table: "webstudio_index",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "response_payload",
                table: "webhooks_logs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "request_payload",
                table: "webhooks_logs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "uri",
                table: "webhooks_config",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "target_id",
                table: "webhooks_config",
                type: "character varying",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "secret_key",
                table: "webhooks_config",
                type: "text",
                maxLength: 50,
                nullable: true,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true,
                oldDefaultValueSql: "''");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "webhooks_config",
                type: "character varying",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "last_failure_content",
                table: "webhooks_config",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "route",
                table: "webhooks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "method",
                table: "webhooks",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10,
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "version",
                table: "tenants_version",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "tenants_version",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "trusteddomains",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "timezone",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "payment_id",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 38,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 38,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "mappeddomain",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "tenants_tenants",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValueSql: "'en-US'",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10,
                oldDefaultValueSql: "'en-US'");

            migrationBuilder.AlterColumn<string>(
                name: "alias",
                table: "tenants_tenants",
                type: "varchar",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "customer_id",
                table: "tenants_tariff",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "tenants_tariff",
                type: "varchar",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "tag",
                table: "tenants_quotarow",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "tenants_quotarow",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "service_name",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "service_group",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "product_id",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "features",
                table: "tenants_quota",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "tenants_quota",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "partner_id",
                table: "tenants_partners",
                type: "character varying",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "campaign",
                table: "tenants_partners",
                type: "character varying",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "affiliate_id",
                table: "tenants_partners",
                type: "character varying",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "tenants_iprestrictions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "tenants_forbiden",
                type: "varchar",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "telegram_username",
                table: "telegram_users",
                type: "varchar(35)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "short",
                table: "short_links",
                type: "varchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "Regions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionString",
                table: "Regions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "Regions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "subject",
                table: "notify_queue",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender_type",
                table: "notify_queue",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender",
                table: "notify_queue",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reply_to",
                table: "notify_queue",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reciever",
                table: "notify_queue",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content_type",
                table: "notify_queue",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "notify_queue",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "auto_submitted",
                table: "notify_queue",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attachments",
                table: "notify_queue",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_email",
                table: "mobile_app_install",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "platform",
                table: "login_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "page",
                table: "login_events",
                type: "varchar",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "login",
                table: "login_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "login_events",
                type: "varchar",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "login_events",
                type: "varchar",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "login_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "locked_by",
                table: "identity_shedlock",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_shedlock",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "identity_scopes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "group",
                table: "identity_scopes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_scopes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_purge_cursors",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "routing_key",
                table: "identity_pending_messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "headers",
                table: "identity_pending_messages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "exchange_name",
                table: "identity_pending_messages",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "owner_user_id",
                table: "identity_consents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_consents",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_consents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "scopes",
                table: "identity_consent_scopes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_consent_scopes",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_consent_scopes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "website_url",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "terms_url",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "policy_url",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "identity_clients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "modified_by",
                table: "identity_clients",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "logout_redirect_uri",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "logo",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "identity_clients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "identity_clients",
                type: "character varying(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "client_secret",
                table: "identity_clients",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_clients",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "scope_name",
                table: "identity_client_scopes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_scopes",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "redirect_uri",
                table: "identity_client_redirect_uris",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_redirect_uris",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_authentication_methods",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "authentication_method",
                table: "identity_client_authentication_methods",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "client_id",
                table: "identity_client_allowed_origins",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "allowed_origin",
                table: "identity_client_allowed_origins",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "public_key",
                table: "identity_certs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "private_key",
                table: "identity_certs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "identity_certs",
                type: "character varying(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "identity_authorizations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_value",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_metadata",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "refresh_token_hash",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "owner_user_id",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_value",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_metadata",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id_token_claims",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorized_scopes",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_code_value",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_code_metadata",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attributes",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_value",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_type",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_scopes",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_metadata",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "access_token_hash",
                table: "identity_authorizations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "authorization_grant_type",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "registered_client_id",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "principal_id",
                table: "identity_authorizations",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "worker_type_name",
                table: "hosting_instance_registration",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "instance_registration_id",
                table: "hosting_instance_registration",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "firebase_device_token",
                table: "firebase_users",
                type: "varchar",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "application",
                table: "firebase_users",
                type: "varchar",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "files_thirdparty_id_mapping",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "hash_id",
                table: "files_thirdparty_id_mapping",
                type: "char(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "files_thirdparty_app",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "app",
                table: "files_thirdparty_app",
                type: "character varying (50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "user_name",
                table: "files_thirdparty_account",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "files_thirdparty_account",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "token",
                table: "files_thirdparty_account",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "files_thirdparty_account",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "files_thirdparty_account",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "folder_id",
                table: "files_thirdparty_account",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customer_title",
                table: "files_thirdparty_account",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "cover",
                table: "files_thirdparty_account",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "files_thirdparty_account",
                type: "varchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_tag_link",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_tag",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_skill",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "files_skill",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_security",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "thirdparty_room_id",
                table: "files_roomgroup",
                type: "varchar(200)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cover",
                table: "files_room_settings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "files_room_settings",
                type: "varchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "data",
                table: "files_properties",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entry_id",
                table: "files_properties",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "value_string",
                table: "files_metadata_value",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "option_id",
                table: "files_metadata_value",
                type: "varchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValueSql: "''",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 36,
                oldDefaultValueSql: "''");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_metadata_template",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_metadata_field",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "linked_id",
                table: "files_link",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "source_id",
                table: "files_link",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "files_group",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "icon",
                table: "files_group",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_color",
                table: "files_form_role_mapping",
                type: "varchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "role_name",
                table: "files_form_role_mapping",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "files_folder",
                type: "character varying",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "private_key_enc",
                table: "files_file_keys",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "files_file",
                type: "character varying",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "converted_type",
                table: "files_file",
                type: "character varying",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "comment",
                table: "files_file",
                type: "character varying",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "changes",
                table: "files_file",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "left_node",
                table: "files_bunch_objects",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "right_node",
                table: "files_bunch_objects",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "event_type_name",
                table: "event_bus_integration_event_log",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "event_bus_integration_event_log",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionId",
                table: "event_bus_integration_event_log",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "zipcode",
                table: "dbip_lookup",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "weather_code",
                table: "dbip_lookup",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "timezone_name",
                table: "dbip_lookup",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "stateprov_code",
                table: "dbip_lookup",
                type: "varchar(15)",
                maxLength: 15,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "stateprov",
                table: "dbip_lookup",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "district",
                table: "dbip_lookup",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "dbip_lookup",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "continent",
                table: "dbip_lookup",
                type: "char(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "dbip_lookup",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "addr_type",
                table: "dbip_lookup",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "pwdhash",
                table: "core_usersecurity",
                type: "varchar",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "permissions",
                table: "core_user_api_key",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "core_user_api_key",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key_postfix",
                table: "core_user_api_key",
                type: "varchar",
                maxLength: 4,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 4);

            migrationBuilder.AlterColumn<string>(
                name: "hashed_key",
                table: "core_user_api_key",
                type: "varchar",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "core_user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "core_user",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sso_session_id",
                table: "core_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sso_name_id",
                table: "core_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sid",
                table: "core_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "core_user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notes",
                table: "core_user",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "location",
                table: "core_user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lastname",
                table: "core_user",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "firstname",
                table: "core_user",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "core_user",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "culture",
                table: "core_user",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contacts",
                table: "core_user",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sender",
                table: "core_subscriptionmethod",
                type: "varchar",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 1024);

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "core_subscriptionmethod",
                type: "varchar",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_subscriptionmethod",
                type: "varchar",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "core_subscriptionmethod",
                type: "varchar",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "object",
                table: "core_subscription",
                type: "varchar",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "recipient",
                table: "core_subscription",
                type: "varchar",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "core_subscription",
                type: "varchar",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "core_subscription",
                type: "varchar",
                maxLength: 38,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 38);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "core_settings",
                type: "character varying",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "sid",
                table: "core_group",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "core_group",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "object",
                table: "core_acl",
                type: "text",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_params",
                table: "backup_schedule",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "storage_base_path",
                table: "backup_schedule",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cron",
                table: "backup_schedule",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_path",
                table: "backup_backup",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "storage_params",
                table: "backup_backup",
                type: "text",
                nullable: true,
                defaultValueSql: "NULL",
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true,
                oldDefaultValueSql: "NULL");

            migrationBuilder.AlterColumn<string>(
                name: "storage_base_path",
                table: "backup_backup",
                type: "character varying",
                maxLength: 255,
                nullable: true,
                defaultValueSql: "NULL",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true,
                oldDefaultValueSql: "NULL");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "backup_backup",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "hash",
                table: "backup_backup",
                type: "character varying",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "target",
                table: "audit_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "platform",
                table: "audit_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "page",
                table: "audit_events",
                type: "varchar",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip",
                table: "audit_events",
                type: "varchar",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "initiator",
                table: "audit_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "audit_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "browser",
                table: "audit_events",
                type: "varchar",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "app_settings",
                type: "character varying",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "default_model",
                table: "ai_providers_default",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "url",
                table: "ai_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_providers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_providers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "alias",
                table: "ai_model_settings",
                type: "character varying",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "model_id",
                table: "ai_model_settings",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_mcp_servers",
                type: "varchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "headers",
                table: "ai_mcp_servers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "endpoint",
                table: "ai_mcp_servers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "ai_mcp_servers",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "oauth_credentials",
                table: "ai_mcp_server_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "server_type",
                table: "ai_integration_tool_preferences",
                type: "character varying",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_integration_threads",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "text",
                table: "ai_integration_prompts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_prompts",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_prompt_folders",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "provider_type",
                table: "ai_integration_profiles",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_profiles",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "model_id",
                table: "ai_integration_profiles",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_integration_profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "base_url",
                table: "ai_integration_profiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "ai_integration_mcp_servers",
                type: "character varying",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "config",
                table: "ai_integration_mcp_servers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "ai_integration_byok_keys",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                table: "ai_integration_byok_keys",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_integration_attachments",
                type: "character varying",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "thirdparty_entry_id",
                table: "ai_integration_attachments",
                type: "char(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content",
                table: "ai_integration_attachments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "ai_chats",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "account_links",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "profile",
                table: "account_links",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AlterColumn<string>(
                name: "uid",
                table: "account_links",
                type: "varchar",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "account_links",
                type: "varchar",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 200);

            AddDependentObjects(migrationBuilder);
        }

        /// <summary>
        /// The foreign keys between text columns of the ASC.Identity tables: PostgreSQL refuses to change the type of one
        /// column of such a key while the other still has the old type, so they are dropped before the columns become citext
        /// (and back) and created again afterwards, as the first migration created them.
        /// </summary>
        private static readonly (string Name, string Table, string[] Columns, string PrincipalTable, string[] PrincipalColumns)[] _textForeignKeys =
        [
            ("fk_identity_consent_scopes_principal_client", "identity_consent_scopes", ["principal_id", "registered_client_id"], "identity_consents", ["principal_id", "registered_client_id"]),
            ("fk_identity_consent_scopes_scopes", "identity_consent_scopes", ["scopes"], "identity_scopes", ["name"]),
            ("identity_client_allowed_origins_ibfk_1", "identity_client_allowed_origins", ["client_id"], "identity_clients", ["client_id"]),
            ("identity_client_authentication_methods_fk_client_id", "identity_client_authentication_methods", ["client_id"], "identity_clients", ["client_id"]),
            ("identity_client_redirect_uris_fk_client", "identity_client_redirect_uris", ["client_id"], "identity_clients", ["client_id"]),
            ("identity_client_scopes_client_id_fk", "identity_client_scopes", ["client_id"], "identity_clients", ["client_id"]),
            ("identity_client_scopes_scope_name_fk", "identity_client_scopes", ["scope_name"], "identity_scopes", ["name"])
        ];

        /// <summary>
        /// The objects that keep PostgreSQL from changing the type of a text column: the foreign keys between text columns
        /// (<see cref="_textForeignKeys"/>) and files_properties.start_filling, a stored column computed from
        /// files_properties.data, with the index that holds it. They are dropped before the columns change type and created
        /// again afterwards, as the first migration created them.
        /// </summary>
        private static void DropDependentObjects(MigrationBuilder migrationBuilder)
        {
            DropTextForeignKeys(migrationBuilder);

            migrationBuilder.DropIndex(name: "idx_tenant_start_entry", table: "files_properties");
            migrationBuilder.DropColumn(name: "start_filling", table: "files_properties");
        }

        private static void AddDependentObjects(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "start_filling",
                table: "files_properties",
                type: "boolean",
                nullable: true,
                computedColumnSql: "CASE WHEN (data::jsonb)->'FormFilling'->>'StartFilling' IS NULL THEN NULL ELSE ((data::jsonb)->'FormFilling'->>'StartFilling')::boolean END",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "idx_tenant_start_entry",
                table: "files_properties",
                columns: new[] { "tenant_id", "start_filling", "entry_id" });

            AddTextForeignKeys(migrationBuilder);
        }

        private static void DropTextForeignKeys(MigrationBuilder migrationBuilder)
        {
            foreach (var key in _textForeignKeys)
            {
                migrationBuilder.DropForeignKey(name: key.Name, table: key.Table);
            }
        }

        private static void AddTextForeignKeys(MigrationBuilder migrationBuilder)
        {
            foreach (var key in _textForeignKeys)
            {
                migrationBuilder.AddForeignKey(
                    name: key.Name,
                    table: key.Table,
                    columns: key.Columns,
                    principalTable: key.PrincipalTable,
                    principalColumns: key.PrincipalColumns,
                    onDelete: ReferentialAction.Cascade);
            }
        }
    }
}
