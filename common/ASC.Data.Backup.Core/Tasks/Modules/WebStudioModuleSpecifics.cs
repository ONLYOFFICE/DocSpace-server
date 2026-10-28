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
        new("app_settings", "tenant_id") {DateColumns = new Dictionary<string, bool> {{"last_modified", false}}},
        new("webhooks_config", "tenant_id", "id") { UserIDColumns = ["created_by", "modified_by"] },
        new("webhooks_logs", "tenant_id", "id") { UserIDColumns = ["retried_by", "uid"] }
    ];

    private readonly RelationInfo[] _relations =
    [
        new("webhooks_config", "id", "webhooks_logs", "config_id"),
        new("webhooks_logs", "id", "webhooks_logs", "retry_of")
    ];

    public override void PrepareData(DataTable data, BackupCorrection backupCorrection)
    {
        if (data.TableName != "webstudio_settings")
        {
            return;
        }

        var hasher = Helpers.CreateHash2;

        foreach (var row in data.Rows.Cast<DataRow>().Where(x => IsWebSearchSettings(x["ID"])))
        {
            row["Data"] = ConvertEncryptedKey(row["Data"] as string, hasher);
        }
    }

    protected override async Task<(bool, Dictionary<string, object>)> TryPrepareRow(bool dump, DbConnection connection, ColumnMapper columnMapper, TableInfo table, DataRowInfo row)
    {
        var (prepared, preparedRow) = await base.TryPrepareRow(dump, connection, columnMapper, table, row);

        if (prepared && table.Name == "webhooks_logs")
        {
            // The schedule is kept, so a delivery waiting for its next attempt resumes on the target: a portal transfer
            // (TransferPortalTask restores through this same path) would otherwise lose every pending retry, since the
            // source portal stops sending once it is no longer active. Only the lease is cleared: it names an instance
            // of the source's sender, and a row that was in flight is due again once its lease time (held in
            // next_attempt_on while leased) has passed. The cost for a plain restore: pending rows of the archive are
            // sent by the restored portal, those whose time has passed right after the portal is active again, under
            // new ids (event.id); a delivery the portal had already completed after the backup was taken reaches the
            // receiver a second time.
            SetNull(preparedRow, "lease_id");

            return (true, preparedRow);
        }

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

    private static void SetNull(Dictionary<string, object> preparedRow, string column)
    {
        var key = preparedRow.Keys.FirstOrDefault(x => x.Equals(column, StringComparison.OrdinalIgnoreCase));
        if (key != null)
        {
            preparedRow[key] = null;
        }
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

    // Columns that only record who did something. A user who cannot be mapped (removed before the backup) would drop
    // the whole row under the generic rule, losing a subscription for the admin who last edited it, or a delivery
    // record for the member who caused it; the column is blanked instead. created_by is not among them: a subscription
    // without its owner would skip the access checks made for the owner, so it is dropped with its owner.
    private static readonly HashSet<(string Table, string Column)> _auditUserColumns =
    [
        ("webhooks_config", "modified_by"),
        ("webhooks_logs", "uid"),
        ("webhooks_logs", "retried_by")
    ];

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, ref object value)
    {
        if (!_auditUserColumns.Contains((table.Name, columnName.ToLowerInvariant())))
        {
            return base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);
        }

        var userId = Convert.ToString(value);

        if (Helpers.IsEmptyOrSystemUser(userId))
        {
            return true;
        }

        // uid is not nullable: the empty user is what an event no member caused carries
        value = columnMapper.GetUserMapping(userId) ?? (table.Name == "webhooks_logs" && columnName.Equals("uid", StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty.ToString()
            : null);

        return true;
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

        if (relation.ChildTable == "webhooks_logs" && relation.ChildColumn is "retry_of" or "config_id")
        {
            // A row whose parent has no mapping is dropped: the generic rule would keep the old id, which in the new
            // tenant points to no row (a foreign key error on insert) or to an unrelated one. For config_id, that is a
            // subscription dropped with its creator who could not be mapped, and its delivery records go with it. For
            // retry_of, the receiver would get that id as event.id; the original row of an event is always restored
            // before its retries, since RestoreDbModuleTask passes the rows of a self relation parents first, whatever
            // order the archive holds them in
            if (value == null || value == DBNull.Value)
            {
                return true;
            }

            var mapped = columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, value);
            if (mapped == null)
            {
                return false;
            }

            value = mapped;

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, relation, ref value);
    }
}
