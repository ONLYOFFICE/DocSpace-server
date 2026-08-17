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

public class WebStudioModuleSpecifics(ILogger<ModuleProvider> logger, Helpers helpers) : ModuleSpecificsBase(helpers)
{
    private const string EncryptedKeyProperty = "EncryptedKey";

    public override ModuleName ModuleName => ModuleName.WebStudio;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _relations;

    private readonly TableInfo[] _tables =
    [
        new("webstudio_fckuploads", "TenantID") {InsertMethod = InsertMethod.None},
        new("webstudio_settings", "TenantID") {UserIDColumns = ["UserID"] },
        new("webstudio_uservisit", "tenantid") {InsertMethod = InsertMethod.None},
        new("webhooks_config", "tenant_id", "id"),
        new("webhooks_logs", "tenant_id", "id")
    ];

    private readonly RelationInfo[] _relations =
    [
        new("webhooks_config", "id", "webhooks_logs", "config_id")
    ];

    public override void PrepareData(DataTable data, BackupCorrection backupCorrection)
    {
        if (data.TableName != "webstudio_settings")
        {
            return;
        }

        foreach (var row in data.Rows.Cast<DataRow>().Where(x => IsWebSearchSettings(x["ID"])))
        {
            row["Data"] = ConvertEncryptedKey(row["Data"] as string, Helpers.CreateHash2);
        }
    }

    protected override async Task<(bool, Dictionary<string, object>)> TryPrepareRow(bool dump, DbConnection connection, ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
    {
        var (prepared, preparedRow) = await base.TryPrepareRow(dump, connection, columnMapper, table, row);
        if (!prepared || table.Name != "webstudio_settings" || !IsWebSearchSettings(row["ID"]))
        {
            return (prepared, preparedRow);
        }

        var dataColumn = preparedRow.Keys.FirstOrDefault(x => x.Equals("data", StringComparison.OrdinalIgnoreCase));
        if (dataColumn != null)
        {
            preparedRow[dataColumn] = ConvertEncryptedKey(preparedRow[dataColumn] as string, Helpers.CreateHash);
        }

        return (true, preparedRow);
    }

    private static bool IsWebSearchSettings(object settingsId)
    {
        return Guid.TryParse(Convert.ToString(settingsId), out var id) && id == WebSearchSettings.ID;
    }

    private string ConvertEncryptedKey(string data, Func<string, string> convert)
    {
        if (string.IsNullOrEmpty(data))
        {
            return data;
        }

        try
        {
            if (JsonNode.Parse(data) is not JsonObject settings)
            {
                return data;
            }

            var property = settings.FirstOrDefault(x => x.Key.Equals(EncryptedKeyProperty, StringComparison.OrdinalIgnoreCase));
            var encryptedKey = property.Value?.GetValue<string>();
            if (string.IsNullOrEmpty(encryptedKey))
            {
                return data;
            }

            settings[property.Key] = convert(encryptedKey);

            return settings.ToJsonString();
        }
        catch (Exception ex)
        {
            logger.ErrorCanNotPrepareSettings(WebSearchSettings.ID, ex);

            return data;
        }
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, RelationInfo relation, ref object value)
    {
        if (relation.ParentTable == "crm_organisation_logo")
        {
            var success = true;
            value = Regex.Replace(
                Convert.ToString(value),
                @"(?<=""CompanyLogoID"":)\d+",
                match =>
                {
                    if (Convert.ToInt32(match.Value) == 0)
                    {
                        success = true;

                        return match.Value;
                    }

                    var mappedMessageId = Convert.ToString(columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, match.Value));
                    success = !string.IsNullOrEmpty(mappedMessageId);

                    return mappedMessageId;
                });

            return success;
        }
        return base.TryPrepareValue(connection, columnMapper, relation, ref value);
    }
}