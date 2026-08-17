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

namespace ASC.Data.Backup.Tasks.Modules;

public class AiProvidersModuleSpecifics(Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Ai;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => [];

    private readonly TableInfo[] _tables =
    [
        new("ai_providers", "tenant_id", "id")
        {
            DateColumns = new Dictionary<string, bool> { { "created_on", false }, { "modified_on", false } }
        }
    ];
}

public class AiModuleSpecifics(Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Ai;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private readonly TableInfo[] _tables =
    [
        new("ai_chats", "tenant_id", "id", IdType.Guid)
        {
            UserIDColumns = ["user_id"],
            DateColumns = new Dictionary<string, bool> { { "created_on", false }, { "modified_on", false } }
        },
        new("ai_chats_messages", idColumn: "id", idType: IdType.Autoincrement)
        {
            DateColumns = new Dictionary<string, bool> { { "created_on", false } }
        },
        new("ai_user_chat_settings", "tenant_id"),
        new("ai_mcp_servers", "tenant_id", "id", IdType.Guid)
        {
            DateColumns = new Dictionary<string, bool> { { "modified_on", false } }
        },
        new("ai_mcp_server_states", "tenant_id"),
        new("ai_mcp_room_servers", "tenant_id"),
        new("ai_mcp_server_settings", "tenant_id"),
        new("ai_model_settings", "tenant_id")
    ];

    private readonly RelationInfo[] _tableRelations =
    [
        new("core_user", "id", "ai_chats", "user_id"),
        new("core_user", "id", "ai_user_chat_settings", "user_id"),
        new("core_user", "id", "ai_mcp_server_settings", "user_id"),
        new("files_folder", "id", "ai_chats", "room_id"),
        new("files_folder", "id", "ai_mcp_server_settings", "room_id"),
        new("files_folder", "id", "ai_mcp_room_servers", "room_id"),
        new("files_folder", "id", "ai_user_chat_settings", "room_id"),
        new("ai_chats", "id", "ai_chats_messages", "chat_id"),
        new("ai_mcp_servers", "id", "ai_mcp_server_states", "id"),
        new("ai_providers", "id", "ai_model_settings", "provider_id")
    ];

    protected override string GetSelectCommandConditionText(int tenantId, TableInfo table)
    {
        return table.Name == "ai_chats_messages"
            ? $"inner join ai_chats as chat on chat.id = t.chat_id and chat.tenant_id = {tenantId}"
            : base.GetSelectCommandConditionText(tenantId, table);
    }
}

public class AiIntegrationModuleSpecifics(ILogger<ModuleProvider> logger, Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Ai;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private readonly TableInfo[] _tables =
    [
        new("ai_integration_profiles", "tenant_id", "id", IdType.GuidV7)
        {
            DateColumns = new Dictionary<string, bool> { { "created_at", false } }
        },
        new("ai_integration_threads", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"],
            DateColumns = new Dictionary<string, bool> { { "created_at", false }, { "last_edit_date", false } }
        },
        new("ai_integration_messages", "tenant_id", "id", IdType.GuidV7)
        {
            DateColumns = new Dictionary<string, bool> { { "timestamp", false } }
        },
        new("ai_integration_attachments", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"],
            DateColumns = new Dictionary<string, bool> { { "created_at", false } }
        },
        new("ai_integration_prompt_folders", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"],
            DateColumns = new Dictionary<string, bool> { { "created_at", false }, { "updated_at", false } }
        },
        new("ai_integration_prompts", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"],
            DateColumns = new Dictionary<string, bool> { { "created_at", false }, { "updated_at", false } }
        },
        new("ai_integration_preferences", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"]
        },
        new("ai_integration_tool_preferences", "tenant_id", "id", IdType.GuidV7)
        {
            UserIDColumns = ["created_by"],
            DateColumns = new Dictionary<string, bool> { { "created_at", false } }
        },
        new("ai_integration_mcp_servers", "tenant_id", "id", IdType.GuidV7)
        {
            DateColumns = new Dictionary<string, bool> { { "created_at", false } }
        },
        new("ai_integration_assignments", "tenant_id", "id", IdType.GuidV7)
        {
            DateColumns = new Dictionary<string, bool> { { "created_at", false } }
        }
    ];

    private readonly RelationInfo[] _tableRelations =
    [
        new("core_user", "id", "ai_integration_threads", "created_by"),
        new("core_user", "id", "ai_integration_prompt_folders", "created_by"),
        new("core_user", "id", "ai_integration_prompts", "created_by"),
        new("core_user", "id", "ai_integration_preferences", "created_by"),
        new("core_user", "id", "ai_integration_tool_preferences", "created_by"),
        new("files_folder", "id", "ai_integration_threads", "entry_id"),
        new("files_folder", "id", "ai_integration_preferences", "entry_id"),
        new("files_folder", "id", "ai_integration_tool_preferences", "entry_id"),
        new("files_folder", "id", "ai_integration_mcp_servers", "entry_id"),
        new("files_folder", "id", "ai_integration_assignments", "entry_id"),
        new("files_file", "id", "ai_integration_attachments", "entry_id"),
        new("files_thirdparty_id_mapping", "hash_id", "ai_integration_attachments", "thirdparty_entry_id", typeof(FilesModuleSpecifics)),
        new("ai_integration_profiles", "id", "ai_integration_threads", "profile_id"),
        new("ai_integration_profiles", "id", "ai_integration_assignments", "profile_id"),
        new("ai_integration_prompt_folders", "id", "ai_integration_prompts", "folder_id"),
        new("ai_integration_threads", "id", "ai_integration_messages", "thread_id"),
        new("ai_integration_messages", "id", "ai_integration_attachments", "message_id")
    ];

    public override void PrepareData(DataTable data, BackupCorrection backupCorrection)
    {
        switch (data.TableName)
        {
            case "ai_integration_profiles":
                PrepareEncryptedColumn(data, "key", false);
                break;
            case "ai_integration_mcp_servers":
                PrepareEncryptedColumn(data, "config", true);
                break;
        }
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, ref object value)
    {
        switch (table.Name)
        {
            case "ai_integration_profiles" when columnName == "key" && value != null:
                try
                {
                    value = Helpers.CreateHash(value as string);
                }
                catch (Exception ex)
                {
                    logger.ErrorCanNotPrepareValue(value as string, ex);
                    value = null;
                }

                return true;
            case "ai_integration_mcp_servers" when columnName == "config" && value != null:
                try
                {
                    value = Helpers.CreateHash(value as string);
                }
                catch (Exception ex)
                {
                    logger.ErrorCanNotPrepareValue(value as string, ex);

                    return false;
                }

                return true;
            default:
                return base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);
        }
    }

    private void PrepareEncryptedColumn(DataTable data, string columnName, bool removeOnFailure)
    {
        for (var i = 0; i < data.Rows.Count; i++)
        {
            var row = data.Rows[i];
            try
            {
                row[columnName] = Helpers.CreateHash2(row[columnName] as string);
            }
            catch (Exception ex)
            {
                logger.ErrorCanNotPrepareValue(row[columnName] as string, ex);

                if (removeOnFailure)
                {
                    data.Rows.Remove(row);
                    i--;
                }
                else
                {
                    row[columnName] = null;
                }
            }
        }
    }
}
