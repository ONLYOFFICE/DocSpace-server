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

namespace ASC.Data.Backup.Tasks;

public class RestoreDbModuleTask : PortalTaskBase
{
    private const int TransactionLength = 10000;

    private readonly IDataReadOperator _reader;
    private readonly ILogger<RestoreDbModuleTask> _logger;
    private readonly IModuleSpecifics _module;
    private readonly ColumnMapper _columnMapper;
    private readonly bool _replaceDate;
    private readonly bool _dump;
    private readonly string _region;

    public RestoreDbModuleTask(
        ILogger<RestoreDbModuleTask> logger,
        IModuleSpecifics module,
        IDataReadOperator reader,
        ColumnMapper columnMapper,
        DbFactory factory,
        bool replaceDate,
        bool dump,
        string region,
        StorageFactory storageFactory,
        StorageFactoryConfig storageFactoryConfig,
        ModuleProvider moduleProvider)
        : base(factory, logger, storageFactory, storageFactoryConfig, moduleProvider)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _columnMapper = columnMapper ?? throw new ArgumentNullException(nameof(columnMapper));
        DbFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger;
        _module = module;
        _replaceDate = replaceDate;
        _dump = dump;
        _region = region;
        Init(-1);
    }

    public override async Task RunJob()
    {
        _logger.DebugBeginRestoreDataForModule(_module.ModuleName);
        SetStepsCount(_module.Tables.Count(t => !_ignoredTables.Contains(t.Name)));

        await using (var connection = DbFactory.OpenConnection(region:_region))
        {
            foreach (var table in _module.GetTablesOrdered().Where(t => !_ignoredTables.Contains(t.Name) && t.InsertMethod != InsertMethod.None))
            {
                _logger.DebugBeginRestoreTable(table.Name);

                var transactionData = new TransactionData();
                await ActionInvoker.TryAsync(state => RestoreTable(connection.Fix(), (TableInfo)state, transactionData), table, 5,
                    onAttemptFailure: _ => _columnMapper.Rollback(),
                    onFailure: error => throw ThrowHelper.CantRestoreTable(table.Name, error));

                await SetStepCompleted();
                _logger.DebugRowsInserted(transactionData.RowsInserted, table.Name);
            }
        }

        _logger.DebugEndRestoreDataForModule(_module.ModuleName);
    }

    private string[] ExecuteArray(DbCommand command)
    {
        var list = new List<string>();
        using (var result = command.ExecuteReader())
        {
            while (result.Read())
            {
                list.Add(result.GetString(0));
            }
        }

        return list.ToArray();
    }

    private async Task RestoreTable(DbConnection connection, TableInfo tableInfo, TransactionData transactionData)
    {
        SetColumns(connection, tableInfo);

        await using var stream = _reader.GetEntry(KeyHelper.GetTableZipKey(_module, tableInfo.Name));
        var lowImportanceRelations = _module
            .TableRelations
            .Where(r => string.Equals(r.ParentTable, tableInfo.Name, StringComparison.InvariantCultureIgnoreCase))
            .Where(r => r.Importance == RelationImportance.Low && !r.IsSelfRelation())
            .Select(r => Tuple.Create(r, _module.Tables.Single(t => t.Name == r.ChildTable)))
            .ToList();

        foreach (
            var rows in
                GetRows(tableInfo, stream)
                    .Skip(transactionData.TransactionsCommited * TransactionLength)
                    .MakeParts(TransactionLength))
        {
            await using var transaction = await connection.BeginTransactionAsync();
            var rowsSuccess = 0;
            foreach (var row in rows)
            {
                if (_replaceDate)
                {
                    foreach (var column in tableInfo.DateColumns)
                    {
                        _columnMapper.SetDateMapping(tableInfo.Name, column, row[column.Key]);
                    }
                }

                object oldIdValue = null;
                object newIdValue;

                if (tableInfo.HasIdColumn())
                {
                    oldIdValue = row[tableInfo.IdColumn];
                    newIdValue = _columnMapper.GetMapping(tableInfo.Name, tableInfo.IdColumn, oldIdValue);
                    if (newIdValue == null)
                    {
                        if (tableInfo.IdType == IdType.Guid)
                        {
                            newIdValue = Guid.NewGuid().ToString("D");
                        }
                        else if (tableInfo.IdType == IdType.Integer)
                        {
                            var command = connection.CreateCommand();
                            command.CommandText = $"select max({tableInfo.IdColumn}) from {tableInfo.Name};";
                            newIdValue = (int)await command.WithTimeout(120).ExecuteScalarAsync() + 1;
                        }
                    }
                    if (newIdValue != null)
                    {
                        _columnMapper.SetMapping(tableInfo.Name, tableInfo.IdColumn, oldIdValue,
                            newIdValue);
                    }
                }

                var insertCommand = await _module.CreateInsertCommand(_dump, connection, _columnMapper, tableInfo, row);
                if (insertCommand == null)
                {
                    _logger.WarningCantCreateCommand(tableInfo, row);
                    _columnMapper.Rollback();

                    continue;
                }
                await insertCommand.WithTimeout(120).ExecuteNonQueryAsync();
                rowsSuccess++;

                if (tableInfo.HasIdColumn() && tableInfo.IdType == IdType.Autoincrement)
                {
                    var lastIdCommand = DbFactory.CreateLastInsertIdCommand();
                    lastIdCommand.Connection = connection;
                    newIdValue = Convert.ToInt32(await lastIdCommand.ExecuteScalarAsync());
                    _columnMapper.SetMapping(tableInfo.Name, tableInfo.IdColumn, oldIdValue, newIdValue);
                }

                if (tableInfo.Name == "files_thirdparty_account")
                {
                    try
                    {
                        var ids = string.Join("-|", Selectors.All.Select(s => s.Id));
                        var sboxId = Regex.Replace(row[12].ToString(), @"(?<=(?:" + $"{ids}-" + @"))\d+", match =>
                        {
                            var folderId = _columnMapper.GetMapping(tableInfo.Name, tableInfo.IdColumn, match.Value);

                            return Convert.ToString(folderId);
                        }, RegexOptions.Compiled);

                        var command = connection.CreateCommand();
                        command.CommandText = $"update {tableInfo.Name} set folder_id = '{sboxId}' where {tableInfo.IdColumn} = '{_columnMapper.GetMapping(tableInfo.Name, tableInfo.IdColumn, row[0])}'";
                        await command.WithTimeout(120).ExecuteNonQueryAsync();
                    }
                    catch(Exception e)
                    {

                    }
                }

                _columnMapper.Commit();

                foreach (var relation in lowImportanceRelations)
                {
                    if (!relation.Item2.HasTenantColumn())
                    {
                        _logger.WarningTableDoesNotContainTenantIdColumn(relation.Item2.Name);

                        continue;
                    }

                    var oldValue = row[relation.Item1.ParentColumn];
                    var newValue = _columnMapper.GetMapping(relation.Item1.ParentTable,
                        relation.Item1.ParentColumn, oldValue);
                    var command = connection.CreateCommand();
                    command.CommandText = string.Format("update {0} set {1} = {2} where {1} = {3} and {4} = {5}",
                            relation.Item1.ChildTable,
                            relation.Item1.ChildColumn,
                            newValue is string ? "'" + newValue + "'" : newValue,
                            oldValue is string ? "'" + oldValue + "'" : oldValue,
                            relation.Item2.TenantColumn,
                            _columnMapper.GetTenantMapping());
                    await command.WithTimeout(120).ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
            transactionData.TransactionsCommited++;
            transactionData.RowsInserted += rowsSuccess;
        }
    }

    private IEnumerable<DataRowInfo> GetRows(TableInfo table, Stream xmlStream)
    {
        if (xmlStream == null)
        {
            return Enumerable.Empty<DataRowInfo>();
        }

        var rows = DataRowInfoReader.ReadFromStream(xmlStream);

        var selfRelation = _module.TableRelations.SingleOrDefault(x => x.ChildTable == table.Name && x.IsSelfRelation());
        if (selfRelation != null)
        {
            rows = rows
                .ToTree(x => x[selfRelation.ParentColumn], x => x[selfRelation.ChildColumn])
                .SelectMany(OrderNode);
        }

        return rows;
    }

    private static IEnumerable<DataRowInfo> OrderNode(TreeNode<DataRowInfo> node)
    {
        var result = new List<DataRowInfo> { node.Entry };
        result.AddRange(node.Children.SelectMany(OrderNode));

        return result;
    }

    private void SetColumns(DbConnection connection, TableInfo table)
    {
        var showColumnsCommand = DbFactory.CreateShowColumnsCommand(table.Name);
        showColumnsCommand.Connection = connection;

        table.Columns = ExecuteArray(showColumnsCommand);
    }

    private class TransactionData
    {
        public int TransactionsCommited { get; set; }
        public int RowsInserted { get; set; }
    }
}
