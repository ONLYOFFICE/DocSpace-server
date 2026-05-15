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

public class CoreModuleSpecifics : ModuleSpecificsBase
{
    public override ModuleName ModuleName => ModuleName.Core;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private readonly RelationInfo[] _tableRelations;
    private readonly Helpers _helpers;
    private readonly TableInfo[] _tables =
    [
        new("core_acl", "tenant") {InsertMethod = InsertMethod.Ignore},
        new("core_subscription", "tenant"),
        new("core_subscriptionmethod", "tenant"),
        new("core_userphoto", "tenant") {UserIDColumns = ["userid"] },
        new("core_usersecurity", "tenant") {UserIDColumns = ["userid"] },
        new("core_usergroup", "tenant") {UserIDColumns = ["userid"] },
        new("backup_schedule", "tenant_id"),
        new("core_settings", "tenant"),
        new("core_user_relations", "tenant") { UserIDColumns = ["source_user_id", "target_user_id"] }
    ];

    public CoreModuleSpecifics(Helpers helpers) : base(helpers)
    {
        _helpers = helpers;
        _tableRelations =
        [
            new RelationInfo("core_user", "id", "core_acl", "subject", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_group", "id", "core_acl", "subject", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_user", "id", "core_subscription", "recipient", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_group", "id", "core_subscription", "recipient", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_user", "id", "core_subscriptionmethod", "recipient", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_group", "id", "core_subscriptionmethod", "recipient", typeof(TenantsModuleSpecifics)),
            new RelationInfo("core_group", "id", "core_usergroup", "groupid", typeof(TenantsModuleSpecifics),
                x => !helpers.IsEmptyOrSystemGroup(Convert.ToString(x["groupid"]))),
            new RelationInfo("files_folder", "id", "backup_schedule", "storage_base_path", typeof(FilesModuleSpecifics),
                x => IsDocumentsStorageType(Convert.ToString(x["storage_type"])))
        ];
    }

    protected override string GetSelectCommandConditionText(int tenantId, TableInfo table)
    {
        if (table.Name == "core_settings")
        {
            return string.Format("where t.{0} = {1} and id not in ('{2}')", table.TenantColumn, tenantId, LicenseReader.CustomerIdKey);
        }

        return base.GetSelectCommandConditionText(tenantId, table);
    }

    protected override async Task<(bool, Dictionary<string, object>)> TryPrepareRow(bool dump, DbConnection connection, ColumnMapper columnMapper,
        TableInfo table, DataRowInfo row)
    {
        if (table.Name == "core_acl" && int.Parse((string)row["tenant"]) == -1)
        {
            return (false, null);
        }

        return await base.TryPrepareRow(dump, connection, columnMapper, table, row);
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, ref object value)
    {
        if (table.Name == "core_usergroup" && columnName == "last_modified")
        {
            value = DateTime.UtcNow;

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, RelationInfo relation, ref object value)
    {
        if (relation.ChildTable == "core_acl" && relation.ChildColumn == "object")
        {
            var valParts = Convert.ToString(value).Split('|');

            var entityId = columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, valParts[1]);
            if (entityId == null)
            {
                return false;
            }

            value = string.Format("{0}|{1}", valParts[0], entityId);

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, relation, ref value);
    }

    protected override bool TryPrepareValue(bool dump, DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, IEnumerable<RelationInfo> relations, ref object value)
    {
        var relationList = relations.ToList();

        if (relationList.TrueForAll(x => x.ChildTable == "core_subscription" && x.ChildColumn == "object" && x.ParentTable.StartsWith("projects_")))
        {
            var valParts = Convert.ToString(value).Split('_');

            var projectId = columnMapper.GetMapping("projects_projects", "id", valParts[2]);
            if (projectId == null)
            {
                return false;
            }

            var firstRelation = relationList.First(x => x.ParentTable != "projects_projects");
            var entityId = columnMapper.GetMapping(firstRelation.ParentTable, firstRelation.ParentColumn, valParts[1]);
            if (entityId == null)
            {
                return false;
            }

            value = string.Format("{0}_{1}_{2}", valParts[0], entityId, projectId);

            return true;
        }

        if (relationList.TrueForAll(x => x.ChildTable == "core_subscription" && x.ChildColumn == "recipient")
            || relationList.TrueForAll(x => x.ChildTable == "core_subscriptionmethod" && x.ChildColumn == "recipient")
            || relationList.TrueForAll(x => x.ChildTable == "core_acl" && x.ChildColumn == "subject"))
        {
            var strVal = Convert.ToString(value);
            if (_helpers.IsEmptyOrSystemUser(strVal) || _helpers.IsEmptyOrSystemGroup(strVal))
            {
                return true;
            }

            foreach (var relation in relationList)
            {
                var mapping = columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, value);
                if (mapping != null)
                {
                    value = mapping;

                    return true;
                }
            }

            return false;
        }

        return base.TryPrepareValue(dump, connection, columnMapper, table, columnName, relationList, ref value);
    }

    private static bool IsDocumentsStorageType(string strStorageType)
    {
        var storageType = int.Parse(strStorageType);

        return storageType is 0 or 1;
    }
}