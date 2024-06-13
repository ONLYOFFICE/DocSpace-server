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

[Scope]
public class BackupPortalTask(
    DbFactory dbFactory,
    IDbContextFactory<BackupsContext> dbContextFactory,
    ILogger<BackupPortalTask> logger,
    TenantManager tenantManager,
    CoreBaseSettings coreBaseSettings,
    StorageFactory storageFactory,
    StorageFactoryConfig storageFactoryConfig,
    ModuleProvider moduleProvider,
    TempStream tempStream)
    : PortalTaskBase(dbFactory, logger, storageFactory, storageFactoryConfig, moduleProvider)
{
    private string BackupFilePath { get; set; }
    private int Limit { get; set; }

    private const int MaxLength = 250;
    private const int BatchLimit = 5000;

    private bool _dump = coreBaseSettings.Standalone;

    public void Init(int tenantId, string toFilePath, int limit, IDataWriteOperator writeOperator, bool dump)
    {
        ArgumentException.ThrowIfNullOrEmpty(toFilePath);

        BackupFilePath = toFilePath;
        Limit = limit;
        WriteOperator = writeOperator;
        _dump = dump;

        Init(tenantId);
    }

    public override async Task RunJob()
    {
        logger.DebugBeginBackup(TenantId);
        await tenantManager.SetCurrentTenantAsync(TenantId);

        await using (WriteOperator)
        {
            if (_dump)
            {
                await DoDump(WriteOperator);
            }
            else
            {
                var modulesToProcess = GetModulesToProcess().ToList();
                var files = GetFiles();
                SetStepsCount(1);
                var count = await files.CountAsync() + modulesToProcess.Select(m => m.Tables.Count(t => !_ignoredTables.Contains(t.Name) && t.InsertMethod != InsertMethod.None)).Sum();

                var completedCount = await DoBackupModule(WriteOperator, modulesToProcess, count);
                if (ProcessStorage)
                {
                    await DoBackupStorageAsync(WriteOperator, files, completedCount, count);
                }
            }
        }

        logger.DebugEndBackup(TenantId);
    }

    private List<object[]> ExecuteList(DbCommand command)
    {
        var list = new List<object[]>();
        using var result = command.ExecuteReader();
        while (result.Read())
        {
            var objects = new object[result.FieldCount];
            result.GetValues(objects);
            list.Add(objects);
        }

        return list;
    }

    private async Task DoDump(IDataWriteOperator writer)
    {
        var databases = new Dictionary<Tuple<string, string>, List<string>>();

        await using (var connection = DbFactory.OpenConnection())
        {
            var command = connection.CreateCommand();
            command.CommandText = "show tables";
            var tables = ExecuteList(command).Select(r => Convert.ToString(r[0])).ToList();
            databases.Add(new Tuple<string, string>("default", DbFactory.ConnectionStringSettings()), tables);
        }

        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(true.ToString())))
        {
            await writer.WriteEntryAsync(KeyHelper.GetDumpKey(), stream, () => Task.CompletedTask);
        }

        IAsyncEnumerable<BackupFileInfo> files = null;

        var stepscount = 0;
        foreach (var db in databases)
        {
            stepscount += db.Value.Count * 4; // (schema + data) * (dump + zip)
        }

        if (ProcessStorage)
        {
            var tenants = (await tenantManager.GetTenantsAsync(false)).Select(r => r.Id);
            foreach (var t in tenants)
            {
                files = files == null ? GetFiles(t) : files.Union(GetFiles(t));
            }

            var count = await files.CountAsync();
            stepscount += count * 2 + 1;
            logger.DebugFilesCount(count);
        }

        SetStepsCount(stepscount);

        foreach (var db in databases)
        {
            await DoDump(writer, db.Key.Item1, db.Key.Item2, db.Value);
        }

        var dir = Path.GetDirectoryName(BackupFilePath);
        var subDir = Path.Combine(dir, Path.GetFileNameWithoutExtension(BackupFilePath));
        logger.DebugDirRemoveStart(subDir);
        Directory.Delete(subDir, true);
        logger.DebugDirRemoveEnd(subDir);

        if (ProcessStorage)
        {
            await DoDumpStorage(writer, files);
        }
    }

    private async Task DoDump(IDataWriteOperator writer, string dbName, string connectionString, List<string> tables)
    {
        var excluded = ModuleProvider.AllModules.Where(r => _ignoredModules.Contains(r.ModuleName)).SelectMany(r => r.Tables).Select(r => r.Name).ToList();
        excluded.AddRange(_ignoredTables);
        excluded.Add("res_");

        var dir = Path.GetDirectoryName(BackupFilePath);
        var subDir = CrossPlatform.PathCombine(dir, Path.GetFileNameWithoutExtension(BackupFilePath));
        string schemeDir;
        string dataDir;
        if (dbName == "default")
        {
            schemeDir = Path.Combine(subDir, KeyHelper.GetDatabaseSchema());
            dataDir = Path.Combine(subDir, KeyHelper.GetDatabaseData());
        }
        else
        {
            schemeDir = Path.Combine(subDir, dbName, KeyHelper.GetDatabaseSchema());
            dataDir = Path.Combine(subDir, dbName, KeyHelper.GetDatabaseData());
        }

        if (!Directory.Exists(schemeDir))
        {
            Directory.CreateDirectory(schemeDir);
        }

        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        var dict = new Dictionary<string, int>();
        foreach (var table in tables)
        {
            dict.Add(table, SelectCount(table, connectionString));
        }

        tables.Sort((pair1, pair2) => dict[pair1].CompareTo(dict[pair2]));

        for (var i = 0; i < tables.Count; i += TasksLimit)
        {
            var tasks = new List<Task>(TasksLimit * 2);
            for (var j = 0; j < TasksLimit && i + j < tables.Count; j++)
            {
                var t = tables[i + j];
                var ignore = excluded.Exists(t.StartsWith);
                tasks.Add(Task.Run(async () => await DumpTableScheme(t, schemeDir, connectionString, ignore)));
                if (!ignore)
                {
                    tasks.Add(Task.Run(async () => await DumpTableData(t, dataDir, dict[t], connectionString)));
                }
                else
                {
                    await SetStepCompleted(2);
                }
            }

            Task.WaitAll(tasks.ToArray());

            await ArchiveDir(writer, subDir);
        }
    }

    private async Task DumpTableScheme(string t, string dir, string connectionString, bool ignore)
    {
        try
        {
            logger.DebugDumpTableSchemeStart(t);
            await using (var connection = DbFactory.OpenConnection(connectionString: connectionString))
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SHOW CREATE TABLE `{t}`";
                var createScheme = ExecuteList(command);
                var creates = new StringBuilder();
                if (ignore)
                {
                    creates.Append(createScheme
                        .Select(r => Convert.ToString(r[1]).Replace("CREATE TABLE ", "CREATE TABLE IF NOT EXISTS "))
                        .FirstOrDefault());
                    creates.Append(';');
                }
                else
                {
                    creates.Append($"DROP TABLE IF EXISTS `{t}`;");
                    creates.AppendLine();
                    creates.Append(createScheme
                        .Select(r => Convert.ToString(r[1]))
                        .FirstOrDefault());
                    creates.Append(';');
                }

                var path = CrossPlatform.PathCombine(dir, t);
                await using (var stream = File.OpenWrite(path))
                {
                    var bytes = Encoding.UTF8.GetBytes(creates.ToString());
                    stream.Write(bytes, 0, bytes.Length);
                }

                await SetStepCompleted();
            }

            logger.DebugDumpTableSchemeStop(t);
        }
        catch (Exception e)
        {
            logger.ErrorDumpTableScheme(e);
        }
    }

    private int SelectCount(string t, string dbName)
    {
        try
        {
            using var connection = DbFactory.OpenConnection(connectionString: dbName);
            using var analyzeCommand = connection.CreateCommand();
            analyzeCommand.CommandText = $"analyze table {t}";
            analyzeCommand.ExecuteNonQuery();
            using var command = connection.CreateCommand();
            command.CommandText = $"select TABLE_ROWS from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{t}' and TABLE_SCHEMA = '{connection.Database}'";

            return int.Parse(command.ExecuteScalar().ToString());
        }
        catch (Exception e)
        {
            logger.ErrorSelectCount(e);
            throw;
        }
    }

    private async Task DumpTableData(string t, string dir, int count, string connectionString)
    {
        try
        {
            if (count == 0)
            {
                logger.DebugDumpTableDataStop(t);
                await SetStepCompleted(2);

                return;
            }

            logger.DebugDumpTableDataStart(t);
            bool searchWithPrimary;
            string primaryIndex;
            var primaryIndexStep = 0;
            var primaryIndexStart = 0;

            List<string> columns;

            await using (var connection = DbFactory.OpenConnection(connectionString: connectionString))
            {
                var command = connection.CreateCommand();
                command.CommandText = string.Format($"SHOW COLUMNS FROM `{t}`");
                columns = ExecuteList(command).Select(r => "`" + Convert.ToString(r[0]) + "`").ToList();
            }

            await using (var connection = DbFactory.OpenConnection())
            {
                var command = connection.CreateCommand();
                command.CommandText = $"select COLUMN_NAME from information_schema.`COLUMNS` where TABLE_SCHEMA = '{connection.Database}' and TABLE_NAME = '{t}' and COLUMN_KEY = 'PRI' and DATA_TYPE = 'int'";
                primaryIndex = ExecuteList(command).ConvertAll(r => Convert.ToString(r[0])).FirstOrDefault();
            }

            await using (var connection = DbFactory.OpenConnection())
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SHOW INDEXES FROM {t} WHERE COLUMN_NAME='{primaryIndex}' AND seq_in_index=1";
                var isLeft = ExecuteList(command);
                searchWithPrimary = isLeft.Count == 1;
            }

            if (searchWithPrimary)
            {
                await using var connection = DbFactory.OpenConnection();
                var command = connection.CreateCommand();
                command.CommandText = $"select max({primaryIndex}), min({primaryIndex}) from {t}";
                var minMax = ExecuteList(command).ConvertAll(r => new Tuple<int, int>(Convert.ToInt32(r[0]), Convert.ToInt32(r[1]))).FirstOrDefault();
                primaryIndexStart = minMax.Item2;
                primaryIndexStep = (minMax.Item1 - minMax.Item2) / count;

                if (primaryIndexStep < Limit)
                {
                    primaryIndexStep = Limit;
                }
            }

            var path = CrossPlatform.PathCombine(dir, t);

            var offset = 0;

            do
            {
                List<object[]> result;

                if (searchWithPrimary)
                {
                    result = GetDataWithPrimary(t, columns, primaryIndex, primaryIndexStart, primaryIndexStep, connectionString);
                    primaryIndexStart += primaryIndexStep;
                }
                else
                {
                    result = GetData(t, columns, offset, connectionString);
                }

                offset += Limit;

                var resultCount = result.Count;

                if (resultCount == 0)
                {
                    break;
                }

                SaveToFile(path, t, columns, result);
            } while (true);


            await SetStepCompleted();
            logger.DebugDumpTableDataStop(t);
        }
        catch (Exception e)
        {
            logger.ErrorDumpTableData(e);
            throw;
        }
    }

    private List<object[]> GetData(string t, IEnumerable<string> columns, int offset, string connectionString)
    {
        using var connection = DbFactory.OpenConnection(connectionString: connectionString);
        var command = connection.CreateCommand();
        var selects = string.Join(',', columns);
        command.CommandText = $"select {selects} from {t} LIMIT {offset}, {Limit}";

        return ExecuteList(command);
    }

    private List<object[]> GetDataWithPrimary(string t, IEnumerable<string> columns, string primary, int start, int step, string connectionString)
    {
        using var connection = DbFactory.OpenConnection(connectionString: connectionString);
        var command = connection.CreateCommand();
        var selects = string.Join(',', columns);
        command.CommandText = $"select {selects} from {t} where {primary} BETWEEN  {start} and {start + step} ";

        return ExecuteList(command);
    }

    private ConnectionStringSettings GetConnectionString(int id, string connectionString)
    {
        connectionString += ";convert zero datetime=True";
        return new ConnectionStringSettings("mailservice-" + id, connectionString, "MySql.Data.MySqlClient");
    }

    private void SaveToFile(string path, string t, IReadOnlyCollection<string> columns, List<object[]> data)
    {
        logger.DebugSaveTable(t);
        List<object[]> portion;
        while ((portion = data.Take(BatchLimit).ToList()).Count > 0)
        {
            using (var sw = new StreamWriter(path, true))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.QuoteChar = '\'';
                writer.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                sw.Write("REPLACE INTO `{0}` ({1}) VALUES ", t, string.Join(",", columns));
                sw.WriteLine();

                for (var j = 0; j < portion.Count; j++)
                {
                    var obj = portion[j];
                    sw.Write("(");

                    for (var i = 0; i < obj.Length; i++)
                    {
                        if (obj[i] is byte[] byteArray && byteArray.Length != 0)
                        {
                            sw.Write("0x");
                            foreach (var b in byteArray)
                                sw.Write("{0:x2}", b);
                        }
                        else
                        {
                            if (obj[i] is string s)
                            {
                                sw.Write("'" + s.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("'", "\\'").Replace("\n", "\\n") + "'");
                            }
                            else
                            {
                                var ser = new JsonSerializer();
                                ser.Serialize(writer, obj[i]);
                            }
                        }

                        if (i != obj.Length - 1)
                        {
                            sw.Write(",");
                        }
                    }

                    sw.Write(")");

                    sw.Write(j != portion.Count - 1 ? "," : ";");

                    sw.WriteLine();
                }
            }

            data = data.Skip(BatchLimit).ToList();
        }
    }

    private async Task DoDumpStorage(IDataWriteOperator writer, IAsyncEnumerable<BackupFileInfo> files)
    {
        logger.DebugBeginBackupStorage();

        var dir = Path.GetDirectoryName(BackupFilePath);
        var subDir = CrossPlatform.PathCombine(dir, Path.GetFileNameWithoutExtension(BackupFilePath));

        var storageDir = CrossPlatform.PathCombine(subDir, KeyHelper.GetStorage());

        if (!Directory.Exists(storageDir))
        {
            Directory.CreateDirectory(storageDir);
        }

        var tasks = new List<Task>(TasksLimit);
        var restoreInfoXml = new XElement("storage_restore");
        await foreach (var file in files)
        {
            if (tasks.Count < TasksLimit)
            {
                tasks.Add(Task.Run(() => DoDumpFileAsync(file, storageDir)));
            }
            else
            {
                Task.WaitAll(tasks.ToArray());
                tasks.Clear();
                await ArchiveDir(writer, subDir);
            }

            restoreInfoXml.Add(file.ToXElement());
        }

        if (tasks.Count != 0)
        {
            Task.WaitAll(tasks.ToArray());
            tasks.Clear();
            await ArchiveDir(writer, subDir);
        }

        Directory.Delete(storageDir, true);

        var tmpPath = CrossPlatform.PathCombine(subDir, KeyHelper.GetStorageRestoreInfoZipKey());
        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath));

        await using (var tmpFile = new FileStream(tmpPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose))
        {
            restoreInfoXml.WriteTo(tmpFile);
            await writer.WriteEntryAsync(KeyHelper.GetStorageRestoreInfoZipKey(), tmpFile, async () => await SetStepCompleted());
        }

        Directory.Delete(subDir, true);

        logger.DebugEndBackupStorage();
    }

    private async Task DoDumpFileAsync(BackupFileInfo file, string dir)
    {
        var storage = await StorageFactory.GetStorageAsync(file.Tenant, file.Module);
        var filePath = CrossPlatform.PathCombine(dir, file.GetZipKey());
        var dirName = Path.GetDirectoryName(filePath);

        logger.DebugBackupFile(filePath);

        if (!Directory.Exists(dirName) && !string.IsNullOrEmpty(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        if (!WorkContext.IsMono && filePath.Length > MaxLength)
        {
            filePath = @"\\?\" + filePath;
        }

        await using (var fileStream = await storage.GetReadStreamAsync(file.Domain, file.Path))
        await using (var tmpFile = File.OpenWrite(filePath))
        {
            await fileStream.CopyToAsync(tmpFile);
        }

        await SetStepCompleted();
    }

    private async Task ArchiveDir(IDataWriteOperator writer, string subDir)
    {
        logger.DebugArchiveDirStart(subDir);
        foreach (var enumerateFile in Directory.EnumerateFiles(subDir, "*", SearchOption.AllDirectories))
        {
            var f = enumerateFile;
            if (!WorkContext.IsMono && enumerateFile.Length > MaxLength)
            {
                f = @"\\?\" + f;
            }

            await using var tmpFile = new FileStream(f, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.DeleteOnClose);
            await writer.WriteEntryAsync(enumerateFile[subDir.Length..], tmpFile, async () => await SetStepCompleted());
        }

        logger.DebugArchiveDirEnd(subDir);
    }

    private IAsyncEnumerable<BackupFileInfo> GetFiles()
    {
        return GetFiles(TenantId);
    }

    private async IAsyncEnumerable<BackupFileInfo> GetFiles(int tenantId)
    {
        var files = GetFilesToProcess(tenantId).Distinct();

        await using var backupRecordContext = await dbContextFactory.CreateDbContextAsync();
        var exclude = await Queries.BackupRecordsAsync(backupRecordContext, tenantId).ToListAsync();

        files = files.Where(f => !exclude.Exists(e => f.Path.Replace('\\', '/').Contains($"/file_{e.StoragePath}/")));

        await foreach (var file in files)
        {
            yield return file;
        }
    }

    private async Task<int> DoBackupModule(IDataWriteOperator writer, List<IModuleSpecifics> modules, int count)
    {
        var tablesProcessed = 0;
        foreach (var module in modules)
        {
            logger.DebugBeginSavingDataForModule(module.ModuleName);
            var tablesToProcess = module.Tables.Where(t => !_ignoredTables.Contains(t.Name) && t.InsertMethod != InsertMethod.None).ToList();

            await using (var connection = DbFactory.OpenConnection())
            {
                foreach (var table in tablesToProcess)
                {
                    logger.DebugBeginLoadTable(table.Name);
                    using var data = new DataTable(table.Name);
                    ActionInvoker.Try(
                        state =>
                        {
                            data.Clear();
                            int counts;
                            var offset = 0;
                            do
                            {
                                var t = (TableInfo)state;
                                var dataAdapter = DbFactory.CreateDataAdapter();
                                dataAdapter.SelectCommand = module.CreateSelectCommand(connection.Fix(), TenantId, t, Limit, offset).WithTimeout(600);
                                counts = ((DbDataAdapter)dataAdapter).Fill(data);
                                offset += Limit;
                            } while (counts == Limit);
                        },
                        table,
                        maxAttempts: 5,
                        onFailure: error => throw ThrowHelper.CantBackupTable(table.Name, error),
                        onAttemptFailure: logger.WarningBackupAttemptFailure);

                    foreach (var col in data.Columns.Cast<DataColumn>().Where(col => col.DataType == typeof(DateTime)))
                    {
                        col.DateTimeMode = DataSetDateTime.Unspecified;
                    }

                    module.PrepareData(data);

                    logger.DebugEndLoadTable(table.Name);

                    logger.DebugBeginSavingTable(table.Name);

                    await using (var file = tempStream.Create())
                    {
                        data.WriteXml(file, XmlWriteMode.WriteSchema);
                        data.Clear();

                        await writer.WriteEntryAsync(KeyHelper.GetTableZipKey(module, data.TableName), file, SetProgress);
                    }

                    async Task SetProgress()
                    {
                        await SetCurrentStepProgress((int)(++tablesProcessed * 100 / (double)count));
                    }

                    logger.DebugEndSavingTable(table.Name);
                }
            }

            logger.DebugEndSavingDataForModule(module.ModuleName);
        }
        return tablesProcessed;
    }

    private async Task DoBackupStorageAsync(IDataWriteOperator writer, IAsyncEnumerable<BackupFileInfo> files, int completedCount, int count)
    {
        logger.DebugBeginBackupStorage();

        var filesProcessed = completedCount;

        async Task SetProgress()
        {
            await SetCurrentStepProgress((int)(++filesProcessed * 100 / (double)count));
        }

        await using var tmpFile = tempStream.Create();
        var bytes = "<storage_restore>"u8.ToArray();
        await tmpFile.WriteAsync(bytes);
        await foreach (var module in files.GroupBy(r=> r.Module))
        {
            var storage = await StorageFactory.GetStorageAsync(TenantId, module.Key);
            await foreach (var file in module)
            {
                await writer.WriteEntryAsync(file.GetZipKey(), file.Domain, file.Path, storage, SetProgress);

                var restoreInfoXml = file.ToXElement();
                restoreInfoXml.WriteTo(tmpFile);
            }
        }

        bytes = "</storage_restore>"u8.ToArray();
        await tmpFile.WriteAsync(bytes);
        await writer.WriteEntryAsync(KeyHelper.GetStorageRestoreInfoZipKey(), tmpFile, () => Task.CompletedTask);

        logger.DebugEndBackupStorage();
    }
}

static file class Queries
{
    public static readonly Func<BackupsContext, int, IAsyncEnumerable<BackupRecord>> BackupRecordsAsync = Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
        (BackupsContext ctx, int tenantId) =>
            ctx.Backups.Where(b => b.TenantId == tenantId
                                   && b.StorageType == 0
                                   && b.StoragePath != null));
}