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

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASC.Migrations.PostgreSql.SaaS.Migrations
{
    /// <inheritdoc />
    public partial class MigrationContext_Upgrade1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "webstudio_settings",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "webstudio_index",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "next_attempt_on",
                table: "webhooks_logs",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "delivery",
                table: "webhooks_logs",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "creation_time",
                table: "webhooks_logs",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "webhooks_config",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_success_on",
                table: "webhooks_config",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_failure_on",
                table: "webhooks_config",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "webhooks_config",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "version_changed",
                table: "tenants_tenants",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "statuschanged",
                table: "tenants_tenants",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "tenants_tenants",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "creationdatetime",
                table: "tenants_tenants",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "due_date",
                table: "tenants_tariffrow",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "stamp",
                table: "tenants_tariff",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "tenants_tariff",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "tenants_quotarow",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "creation_date",
                table: "notify_queue",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modify_date",
                table: "notify_info",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "registered_on",
                table: "mobile_app_install",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_sign",
                table: "mobile_app_install",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "login_events",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expiration",
                table: "invitation_link",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_updated",
                table: "hosting_instance_registration",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_thirdparty_app",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_thirdparty_account",
                type: "timestamp(0) with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_thirdparty_account",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_tag_link",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "files_security",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "value_date",
                table: "files_metadata_value",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_value",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_value",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_template",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_template",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_link",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_field",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_field",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "submission_date",
                table: "files_form_role_mapping",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "opened_at",
                table: "files_form_role_mapping",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_folder",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_folder",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_on",
                table: "files_file_vectorization",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "files_file_vectorization",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_file_keys",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_file",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_file",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_chat_message_attachment",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "event_bus_integration_event_log",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "lastmodified",
                table: "core_usersecurity",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_usergroup",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_used",
                table: "core_user_api_key",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "core_user_api_key",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "core_user_api_key",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "workfromdate",
                table: "core_user",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "terminateddate",
                table: "core_user",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_user",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "core_user",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "bithdate",
                table: "core_user",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_settings",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_group",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_backup_time",
                table: "backup_schedule",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "backup_backup",
                type: "timestamp(0) with time zone",
                nullable: false,
                defaultValueSql: "'0001-01-01 00:00:00'",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "'0001-01-01 00:00:00'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "backup_backup",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "audit_events",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "app_settings",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_providers",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_providers",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_mcp_servers",
                type: "timestamp(0) with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_tool_preferences",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_edit_date",
                table: "ai_integration_threads",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_threads",
                type: "timestamp(0) without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_threads",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_integration_prompts",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_prompts",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_integration_prompt_folders",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_prompt_folders",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_profiles",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_mcp_servers",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_integration_byok_keys",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_attachments",
                type: "timestamp(0) without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_attachments",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_assignments",
                type: "timestamp(0) without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_chats_messages",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_chats",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_chats",
                type: "timestamp(0) with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_chats",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz");

            migrationBuilder.AlterColumn<DateTime>(
                name: "linked",
                table: "account_links",
                type: "timestamp(0) with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "webstudio_settings",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "webstudio_index",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "next_attempt_on",
                table: "webhooks_logs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "delivery",
                table: "webhooks_logs",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "creation_time",
                table: "webhooks_logs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "webhooks_config",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_success_on",
                table: "webhooks_config",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_failure_on",
                table: "webhooks_config",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "webhooks_config",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "version_changed",
                table: "tenants_tenants",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "statuschanged",
                table: "tenants_tenants",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "tenants_tenants",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "creationdatetime",
                table: "tenants_tenants",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "due_date",
                table: "tenants_tariffrow",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "stamp",
                table: "tenants_tariff",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "tenants_tariff",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "tenants_quotarow",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "creation_date",
                table: "notify_queue",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modify_date",
                table: "notify_info",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "registered_on",
                table: "mobile_app_install",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_sign",
                table: "mobile_app_install",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "login_events",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expiration",
                table: "invitation_link",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_updated",
                table: "hosting_instance_registration",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_thirdparty_app",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_thirdparty_account",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_thirdparty_account",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_tag_link",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "files_security",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "value_date",
                table: "files_metadata_value",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_value",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_value",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_template",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_template",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_link",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_metadata_field",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_metadata_field",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "submission_date",
                table: "files_form_role_mapping",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "opened_at",
                table: "files_form_role_mapping",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_folder",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_folder",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_on",
                table: "files_file_vectorization",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "files_file_vectorization",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_file_keys",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_file",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "files_file",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "files_chat_message_attachment",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "event_bus_integration_event_log",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "lastmodified",
                table: "core_usersecurity",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_usergroup",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_used",
                table: "core_user_api_key",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "core_user_api_key",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "core_user_api_key",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "workfromdate",
                table: "core_user",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "terminateddate",
                table: "core_user",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_user",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_on",
                table: "core_user",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "bithdate",
                table: "core_user",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_settings",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "core_group",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_backup_time",
                table: "backup_schedule",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_on",
                table: "backup_backup",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "'0001-01-01 00:00:00'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldDefaultValueSql: "'0001-01-01 00:00:00'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "backup_backup",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "date",
                table: "audit_events",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_modified",
                table: "app_settings",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_providers",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_providers",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_mcp_servers",
                type: "timestamptz",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_tool_preferences",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_edit_date",
                table: "ai_integration_threads",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_threads",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_threads",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_integration_prompts",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_prompts",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "ai_integration_prompt_folders",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_prompt_folders",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_profiles",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_mcp_servers",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_integration_byok_keys",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_integration_attachments",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_attachments",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "ai_integration_assignments",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_chats_messages",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "modified_on",
                table: "ai_chats",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_on",
                table: "ai_chats",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_on",
                table: "ai_chats",
                type: "timestamptz",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "linked",
                table: "account_links",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(0) with time zone");
        }
    }
}
