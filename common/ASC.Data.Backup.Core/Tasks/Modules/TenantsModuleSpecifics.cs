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

public class TenantsModuleSpecifics(CoreSettings coreSettings, Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override string ConnectionStringName => "core";
    public override ModuleName ModuleName => ModuleName.Tenants;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private readonly TableInfo[] _tables =
    [
        new("tenants_quota", "tenant"),
            new("tenants_tenants", "id", "id")
            {
                DateColumns = new Dictionary<string, bool> {{"creationdatetime", false}, {"statuschanged", false}, {"version_changed", false}}
            },
            new("core_user", "tenant", "id", IdType.Guid)
            {
                DateColumns = new Dictionary<string, bool> {{"workfromdate", false}, {"terminateddate", false}, {"last_modified", false}},
                UserIDColumns = ["id"]
            },
            new("core_group", "tenant", "id", IdType.Guid),
            new("tenants_quotarow", "tenant")
            {
                InsertMethod = InsertMethod.Replace,
                UserIDColumns = ["user_id"]
            },
            new("tenants_iprestrictions", "tenant", "id")
    ];

    private readonly RelationInfo[] _tableRelations =
    [
        new("tenants_tenants", "id", "tenants_quota", "tenant"),
        new("core_user", "id", "tenants_tenants", "owner_id", null, null, RelationImportance.Low),
        new("core_user", "id", "core_user", "created_by")
    ];

    public override void PrepareData(DataTable data, BackupCorrection backupCorrection)
    {
        switch (data.TableName)
        {
            case "tenants_quotarow":
                {
                    for (var i = 0; i < data.Rows.Count; i++)
                    {
                        var path = Convert.ToString(data.Rows[i]["path"]);
                        var tag = Convert.ToString(data.Rows[i]["tag"]);
                        var userId = Guid.Parse(Convert.ToString(data.Rows[i]["user_id"]));

                        if (path == backupCorrection.QuotaRowTableDocumentsPath &&
                            tag == backupCorrection.QuotaRowTableDocumentsTag &&
                            backupCorrection.QuotaRowTable.TryGetValue(userId, out var correction))
                        {
                            data.Rows[i]["counter"] = correction;
                        }
                    }

                    break;
                }
        }
    }

    protected override async Task<(bool, Dictionary<string, object>)> TryPrepareRow(bool dump, DbConnection connection, ColumnMapper columnMapper,
        TableInfo table, DataRowInfo row)
    {
        if (table.Name == "tenants_tenants" && string.IsNullOrEmpty(Convert.ToString(row["payment_id"])))
        {
            var oldTenantID = Convert.ToInt32(row["id"]);
            columnMapper.SetMapping("tenants_tenants", "payment_id", row["payment_id"], await coreSettings.GetKeyAsync(oldTenantID));
        }

        return await base.TryPrepareRow(dump, connection, columnMapper, table, row);
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, ref object value)
    {
        //we insert tenant as suspended so it can't be accessed before restore operation is finished
        if (table.Name.Equals("tenants_tenants", StringComparison.InvariantCultureIgnoreCase) &&
            columnName.Equals("status", StringComparison.InvariantCultureIgnoreCase))
        {
            value = (int)TenantStatus.Restoring;

            return true;
        }

        if (table.Name.Equals("tenants_tenants", StringComparison.InvariantCultureIgnoreCase) &&
            columnName.Equals("last_modified", StringComparison.InvariantCultureIgnoreCase))
        {
            value = DateTime.UtcNow;

            return true;
        }

        if (table.Name.Equals("tenants_quotarow", StringComparison.InvariantCultureIgnoreCase) &&
            columnName.Equals("last_modified", StringComparison.InvariantCultureIgnoreCase))
        {
            value = DateTime.UtcNow;

            return true;
        }

        if (table.Name is "core_user" or "core_group" && columnName == "last_modified")
        {
            value = DateTime.UtcNow.AddMinutes(2);

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);
    }
}