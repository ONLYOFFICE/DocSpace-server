// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Data.Backup.Tasks.Modules;

public class FilesModuleSpecifics(ILogger<ModuleProvider> logger, Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Files;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _tableRelations;

    private const string _bunchRightNodeStartProject = "projects/project/";
    private const string _bunchRightNodeStartCrmOpportunity = "crm/opportunity/";
    private const string _bunchRightNodeStartMy = "files/my/";
    private const string _bunchRightNodeStartTrash = "files/trash/";
    private const string _bunchRightNodeStartPrivacy = "files/privacy/";

    private static readonly Regex _regexIsInteger = new(@"^\d+$", RegexOptions.Compiled);

    private readonly TableInfo[] _tables =
    [
        new("files_file", "tenant_id", "id", IdType.Integer)
            {
                UserIDColumns = ["create_by", "modified_by"],
                DateColumns = new Dictionary<string, bool> {{"create_on", false}, {"modified_on", false}}
            },
            new("files_folder", "tenant_id", "id")
            {
                UserIDColumns = ["create_by", "modified_by"],
                DateColumns = new Dictionary<string, bool> {{"create_on", false}, {"modified_on", false}}
            },
            new("files_bunch_objects", "tenant_id"),
            new("files_folder_tree"),
            new("files_security", "tenant_id") {UserIDColumns = ["owner"] },
            new("files_thirdparty_account", "tenant_id", "id")
            {
                UserIDColumns = ["user_id"],
                DateColumns = new Dictionary<string, bool> {{"create_on", false}}
            },
            new("files_thirdparty_id_mapping", "tenant_id"),
            new("files_room_settings", "tenant_id")
    ];

    private readonly RelationInfo[] _tableRelations =
    [
        new("core_user", "id", "files_bunch_objects", "right_node", typeof(TenantsModuleSpecifics),
                x =>
                {
                    var rightNode = Convert.ToString(x["right_node"]);

                    return rightNode.StartsWith(_bunchRightNodeStartMy) || rightNode.StartsWith(_bunchRightNodeStartTrash) || rightNode.StartsWith(_bunchRightNodeStartPrivacy);
                }),
            new("core_user", "id", "files_security", "subject", typeof(TenantsModuleSpecifics)),
            new("core_group", "id", "files_security", "subject", typeof(TenantsModuleSpecifics)),

            new("files_folder", "id", "files_bunch_objects", "left_node"),
            new("files_folder", "id", "files_room_settings", "room_id"),
            new("files_folder", "id", "files_file", "folder_id"),
            new("files_folder", "id", "files_folder", "parent_id"),
            new("files_folder", "id", "files_folder_tree", "folder_id"),
            new("files_folder", "id", "files_folder_tree", "parent_id"),
            new("files_file", "id", "files_security", "entry_id",
                x => Convert.ToInt32(x["entry_type"]) == 2 && _regexIsInteger.IsMatch(Convert.ToString(x["entry_id"]))),

            new("files_folder", "id", "files_security", "entry_id",
                x => Convert.ToInt32(x["entry_type"]) == 1 && _regexIsInteger.IsMatch(Convert.ToString(x["entry_id"]))),

            new("files_thirdparty_id_mapping", "hash_id", "files_security", "entry_id",
                x => !_regexIsInteger.IsMatch(Convert.ToString(x["entry_id"]))),

            new("files_thirdparty_account", "id", "files_thirdparty_id_mapping", "id"),
            new("files_thirdparty_account", "id", "files_thirdparty_id_mapping", "hash_id")
    ];

    public override bool TryAdjustFilePath(bool dump, ColumnMapper columnMapper, ref string filePath)
    {
        var match = Regex.Match(filePath.Replace('\\', '/'), @"^folder_\d+/file_(?'fileId'\d+)/(?'versionExtension'v\d+/[\.\w]+)$", RegexOptions.Compiled);
        if (match.Success)
        {
            var fileId = columnMapper.GetMapping("files_file", "id", match.Groups["fileId"].Value);
            if (fileId == null)
            {
                if (!dump)
                {
                    return false;
                }

                fileId = match.Groups["fileId"].Value;
            }

            filePath = string.Format("folder_{0}/file_{1}/{2}", (Convert.ToInt32(fileId) / 1000 + 1) * 1000, fileId, match.Groups["versionExtension"].Value);

            return true;
        }

        return false;
    }

    public override void PrepareData(DataTable data)
    {
        if (data.TableName == "files_thirdparty_account")
        {
            var providerColumn = data.Columns.Cast<DataColumn>().Single(c => c.ColumnName == "provider");
            var pwdColumn = data.Columns.Cast<DataColumn>().Single(c => c.ColumnName == "password");
            var tokenColumn = data.Columns.Cast<DataColumn>().Single(c => c.ColumnName == "token");
            for (var i = 0; i < data.Rows.Count; i++)
            {
                var row = data.Rows[i];
                try
                {
                    row[pwdColumn] = Helpers.CreateHash2(row[pwdColumn] as string);
                    row[tokenColumn] = Helpers.CreateHash2(row[tokenColumn] as string);
                }
                catch (Exception ex)
                {
                    logger.ErrorCanNotPrepareData(row[providerColumn] as string, ex);
                    data.Rows.Remove(row);
                    i--;
                }
            }
        }
    }

    protected override string GetSelectCommandConditionText(int tenantId, TableInfo table)
    {
        if (table.Name == "files_folder_tree")
        {
            return "inner join files_folder as t1 on t1.id = t.folder_id where t1.tenant_id = " + tenantId;
        }
        if (table.Name == "files_file")
        {
            // do not backup previus backup files
            return "where not exists(select 1 from backup_backup b where b.tenant_id = t.tenant_id and b.storage_path = t.id) and t.tenant_id = " + tenantId;
        }

        return base.GetSelectCommandConditionText(tenantId, table);
    }

    protected override async Task<(bool, Dictionary<string, object>)> TryPrepareRow(bool dump, DbConnection connection, ColumnMapper columnMapper,
        TableInfo table, DataRowInfo row)
    { 
        if (row.TableName == "files_thirdparty_id_mapping")
        {
            //todo: think...
            var preparedRow = new Dictionary<string, object>();

            object folderId = null;
            var ids = string.Join("-|", Selectors.All.Select(s => s.Id));
            var sboxId = Regex.Replace(row[2].ToString(), @"(?<=(?:" + $"{ids}-" + @"))\d+", match =>
            {
                folderId = columnMapper.GetMapping("files_thirdparty_account", "id", match.Value);

                return Convert.ToString(folderId);
            }, RegexOptions.Compiled);

            if (folderId == null)
            {
                return (false, null);
            }

            var hashBytes = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(sboxId));
            var hashedId = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            preparedRow.Add("hash_id", hashedId);
            preparedRow.Add("id", sboxId);
            preparedRow.Add("tenant_id", columnMapper.GetTenantMapping());

            columnMapper.SetMapping("files_thirdparty_id_mapping", "hash_id", row["hash_id"], hashedId);

            return (true, preparedRow);
        }

        return await base.TryPrepareRow(dump, connection, columnMapper, table, row);
    }

    protected override bool TryPrepareValue(bool dump, DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, IEnumerable<RelationInfo> relations, ref object value)
    {
        var relationList = relations.ToList();
        if (relationList.All(x => x.ChildTable == "files_security" && x.ChildColumn == "subject"))
        {
            //note: value could be ShareForEveryoneID and in that case result should be always false
            var strVal = Convert.ToString(value);
            if (Helpers.IsEmptyOrSystemUser(strVal) || Helpers.IsEmptyOrSystemGroup(strVal))
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

            return true;
        }

        return base.TryPrepareValue(dump, connection, columnMapper, table, columnName, relationList, ref value);
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, RelationInfo relation, ref object value)
    {
        if (relation.ChildTable == "files_bunch_objects" && relation.ChildColumn == "right_node")
        {
            var strValue = Convert.ToString(value);

            var start = GetStart(strValue);
            if (start == null)
            {
                return false;
            }

            var entityId = columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, strValue[start.Length..]);
            if (entityId == null)
            {
                return false;
            }

            value = strValue[..start.Length] + entityId;

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, relation, ref value);
    }

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, TableInfo table, string columnName, ref object value)
    {
        if (table.Name == "files_thirdparty_account" && columnName is "password" or "token" && value != null)
        {
            try
            {
                value = Helpers.CreateHash(value as string); // save original hash
            }
            catch (Exception err)
            {
                logger.ErrorCanNotPrepareValue(value, err);
                value = null;
            }
            return true;
        }
        if (table.Name == "files_thirdparty_account" && columnName is "color" or "folder_id" && value == null)
        {
            value = string.Empty;
            return true;
        }
        if (table.Name == "files_folder" && columnName is "create_by" or "modified_by")
        {
            base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, table, columnName, ref value);
    }

    private static string GetStart(string value)
    {
        var allStarts = new[] { _bunchRightNodeStartProject, _bunchRightNodeStartMy, _bunchRightNodeStartPrivacy, _bunchRightNodeStartTrash, _bunchRightNodeStartCrmOpportunity };

        return allStarts.FirstOrDefault(value.StartsWith);
    }
}



public class FilesModuleSpecifics2(Helpers helpers) : ModuleSpecificsBase(helpers)
{
    public override ModuleName ModuleName => ModuleName.Files2;
    public override IEnumerable<TableInfo> Tables => _tables;
    public override IEnumerable<RelationInfo> TableRelations => _rels;

    private static readonly Regex _regexIsInteger = new(@"^\d+$", RegexOptions.Compiled);
    private const string _tagStartMessage = "Message";
    private const string _tagStartTask = "Task";
    private const string _tagStartProject = "Project";
    private const string _tagStartRelationshipEvent = "RelationshipEvent_";

    private readonly TableInfo[] _tables =
    [
        new("files_tag", "tenant_id", "id") {UserIDColumns = ["owner"] },
            new("files_tag_link", "tenant_id")
            {
                UserIDColumns = ["create_by"],
                DateColumns = new Dictionary<string, bool> {{"create_on", false}}
            }
    ];

    private readonly RelationInfo[] _rels =
    [
        new("files_tag", "id", "files_tag_link", "tag_id", typeof(FilesModuleSpecifics)),

            new("files_file", "id", "files_tag_link", "entry_id", typeof(FilesModuleSpecifics),
                x => Convert.ToInt32(x["entry_type"]) == 2 && _regexIsInteger.IsMatch(Convert.ToString(x["entry_id"]))),

            new("files_folder", "id", "files_tag_link", "entry_id",typeof(FilesModuleSpecifics),
                x => Convert.ToInt32(x["entry_type"]) == 1 && _regexIsInteger.IsMatch(Convert.ToString(x["entry_id"]))),

            new("files_thirdparty_id_mapping", "hash_id", "files_tag_link", "entry_id", typeof(FilesModuleSpecifics),
                x => !_regexIsInteger.IsMatch(Convert.ToString(x["entry_id"])))
    ];

    protected override bool TryPrepareValue(DbConnection connection, ColumnMapper columnMapper, RelationInfo relation, ref object value)
    {
        if (relation.ChildTable == "files_tag" && relation.ChildColumn == "name")
        {
            var str = Convert.ToString(value);
            var start = GetStart(str);
            if (start == null)
            {
                return false;
            }
            var entityId = columnMapper.GetMapping(relation.ParentTable, relation.ParentColumn, str[start.Length..]);
            if (entityId == null)
            {
                return false;
            }
            value = str[..start.Length] + entityId;

            return true;
        }

        return base.TryPrepareValue(connection, columnMapper, relation, ref value);
    }

    private static string GetStart(string value)
    {
        var allStarts = new[] { _tagStartMessage, _tagStartTask, _tagStartRelationshipEvent, _tagStartProject };

        return allStarts.FirstOrDefault(value.StartsWith);
    }
}
