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

namespace ASC.Data.Backup.Tasks;

[Scope]
public class RestorePortalTask(DbFactory dbFactory,
        ILogger<RestorePortalTask> options,
        ILogger<RestoreDbModuleTask> logger,
        StorageFactory storageFactory,
        StorageFactoryConfig storageFactoryConfig,
        CoreBaseSettings coreBaseSettings,
        LicenseReader licenseReader,
        TenantManager tenantManager,
        AscCacheNotify ascCacheNotify,
        ModuleProvider moduleProvider,
        BackupRepository backupRepository,
        TenantExtra tenantExtra)
    : PortalTaskBase(dbFactory, options, storageFactory, storageFactoryConfig, moduleProvider)
{
    public bool ReplaceDate { get; set; }
    public bool Dump { get; set; }
    public string BackupFilePath { get; private set; }
    public string UpgradesPath { get; private set; }
    public bool UnblockPortalAfterCompleted { get; set; }

    private ColumnMapper _columnMapper;
    private string _region;
    private bool _expectDump;
    private CancellationToken _cancellationToken;

    public void Init(string region, string fromFilePath, bool expectDump, int tenantId = -1, ColumnMapper columnMapper = null, string upgradesPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromFilePath);

        if (!File.Exists(fromFilePath))
        {
            throw new FileNotFoundException("file not found at given path");
        }

        BackupFilePath = fromFilePath;
        UpgradesPath = upgradesPath;
        _columnMapper = columnMapper ?? new ColumnMapper();
        _region = region;
        _cancellationToken = cancellationToken;
        _expectDump = expectDump; 
        Init(tenantId);
    }

    public override async Task RunJob()
    {
        options.DebugBeginRestorePortal();

        options.DebugBeginRestoreData();

        using (var dataReader = DataOperatorFactory.GetReadOperator(BackupFilePath, true, _cancellationToken))
        {
            await using (var entry = dataReader.GetEntry(KeyHelper.GetDumpKey()))
            {
                Dump = entry != null;
            }

            if (Dump && !coreBaseSettings.Standalone)
            {
                throw new ArgumentException(BackupResource.BackupNotFound);
            }

            if (Dump != _expectDump)
            {
                throw new ArgumentException(BackupResource.BackupInvalid);
            }

            if (Dump)
            {
                await tenantExtra.DemandAccessSpacePermissionAsync();
                await RestoreFromDump(dataReader);
            }
            else
            {
                var modulesToProcess = GetModulesToProcess().ToList();
                SetStepsCount(ProcessStorage ? modulesToProcess.Count + 1 : modulesToProcess.Count);

                foreach (var module in modulesToProcess)
                {
                    using var restoreTask = new RestoreDbModuleTask(logger, module, dataReader, _columnMapper, DbFactory, ReplaceDate, Dump, _region, StorageFactory, StorageFactoryConfig, ModuleProvider);

                    restoreTask.ProgressChanged = args => SetCurrentStepProgress(args.Progress);

                    foreach (var tableName in _ignoredTables)
                    {
                        restoreTask.IgnoreTable(tableName);
                    }

                    await restoreTask.RunJob();
                }
                await backupRepository.MigrationBackupRecordsAsync(TenantId, _columnMapper.GetTenantMapping(), _region);
            }

            options.DebugEndRestoreData();

            if (ProcessStorage)
            {
                if (coreBaseSettings.Standalone)
                {
                    options.DebugClearCache();
                    await ascCacheNotify.ClearCacheAsync();
                }

                await DoRestoreStorage(dataReader);
            }

            if (UnblockPortalAfterCompleted)
            {
                SetTenantActive(_columnMapper.GetTenantMapping());
            }
        }

        if (coreBaseSettings.Standalone && Dump)
        {
            options.DebugRefreshLicense();
            try
            {
                await licenseReader.RejectLicenseAsync();
            }
            catch (Exception ex)
            {
                options.ErrorRunJob(ex);
            }

            options.DebugClearCache();
            await ascCacheNotify.ClearCacheAsync();
        }

        options.DebugEndRestorePortal();
    }

    private async Task RestoreFromDump(IDataReadOperator dataReader)
    {
        var keyBase = KeyHelper.GetDatabaseSchema();
        var keys = dataReader.GetEntries(keyBase).Select(Path.GetFileName).ToList();
        var dbs = dataReader.GetDirectories("").Where(r => Path.GetFileName(r).StartsWith("mailservice")).Select(Path.GetFileName).ToList();
        var upgrades = new List<string>();

        if (!string.IsNullOrEmpty(UpgradesPath) && Directory.Exists(UpgradesPath))
        {
            upgrades = Directory.GetFiles(UpgradesPath).ToList();
        }

        var stepscount = keys.Count * 2 + upgrades.Count;

        foreach (var db in dbs)
        {
            var keys1 = dataReader.GetEntries(db + "/" + keyBase).Select(Path.GetFileName).ToList();
            stepscount += keys1.Count * 2;
        }

        SetStepsCount(ProcessStorage ? stepscount + 1 : stepscount);

        if (ProcessStorage)
        {
            var storageModules = StorageFactoryConfig.GetModuleList(_region).Where(IsStorageModuleAllowed).ToList();
            var tenants = await tenantManager.GetTenantsAsync(false);

            stepscount += storageModules.Count * tenants.Count;

            SetStepsCount(stepscount + 1);

            await DoDeleteStorageAsync(storageModules, tenants);
        }
        else
        {
            SetStepsCount(stepscount);
        }

        for (var i = 0; i < keys.Count; i += TasksLimit)
        {
            var tasks = new List<Task>(TasksLimit * 2);

            for (var j = 0; j < TasksLimit && i + j < keys.Count; j++)
            {
                var key1 = Path.Combine(KeyHelper.GetDatabaseSchema(), keys[i + j]);
                var key2 = Path.Combine(KeyHelper.GetDatabaseData(), keys[i + j]);
                tasks.Add(keys[i + j] == WebhooksLogsTable
                    ? RestoreWebhooksLogsFromDumpFileAsync(dataReader, key1, key2)
                    : RestoreFromDumpFile(dataReader, key1, key2));
            }

            Task.WaitAll(tasks.ToArray(), _cancellationToken);

            _cancellationToken.ThrowIfCancellationRequested();
        }

        var comparer = new SqlComparer();
        foreach (var u in upgrades.OrderBy(Path.GetFileName, comparer))
        {
            RunMysqlFile(u, true);
            await SetStepCompleted();
        }

        // the journal was loaded with its schedule cleared statement by statement (RestoreWebhooksLogsFromDumpFileAsync);
        // this pass takes the leases left on rows that were not due, and anything an upgrade script above scheduled
        await ClearWebhookScheduleAsync();
    }

    private const string WebhooksLogsTable = "webhooks_logs";

    // The webhook journal of a dump carries the schedule of the source's senders, and the live WorkerService claims
    // every row that is due on an active portal within seconds, while the other tables, the portals among them, are
    // still loading and the upgrades have not run. So each statement of its data is committed in one transaction with
    // the update that takes its rows off the schedule (CreateClearWebhookScheduleCommand): no sender ever sees a
    // restored row with the schedule it had on the source, whatever order the tables load in, and a failure of the
    // update rolls back the rows it was for and fails the restore instead of leaving them due. The table's own
    // schema file goes first and leaves it empty (drop and create), so nothing of the live journal stays due either.
    private async Task RestoreWebhooksLogsFromDumpFileAsync(IDataReadOperator dataReader, string schemaFileName, string dataFileName)
    {
        options.DebugRestoreFrom(schemaFileName);
        await using (var stream = dataReader.GetEntry(schemaFileName))
        {
            await RunMysqlFile(stream, null);
        }

        await SetStepCompleted();

        options.DebugRestoreFrom(dataFileName);
        await using (var stream = dataReader.GetEntry(dataFileName))
        {
            if (stream != null)
            {
                await LoadWebhooksLogsDataAsync(stream);
            }
        }

        await SetStepCompleted();
    }

    private async Task LoadWebhooksLogsDataAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);

        await using var connection = DbFactory.OpenConnection();
        var setup = connection.CreateCommand();
        setup.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
        await setup.ExecuteNonQueryAsync(_cancellationToken);

        // a dump of a version without the delivery schedule has nothing to clear: its table has no such columns
        var clearSchedule = true;
        var cleared = 0;

        await foreach (var statement in ReadDumpStatementsAsync(reader))
        {
            for (var attempt = 0; ; attempt++)
            {
                var inserted = false;

                try
                {
                    await using var tx = await connection.BeginTransactionAsync(_cancellationToken);

                    var insert = connection.CreateCommand();
                    insert.Transaction = tx;
                    insert.CommandText = statement;
                    await insert.ExecuteNonQueryAsync(_cancellationToken);

                    inserted = true;

                    if (clearSchedule)
                    {
                        try
                        {
                            cleared += await CreateClearWebhookScheduleCommand(connection, tx, scheduledOnly: true).ExecuteNonQueryAsync(_cancellationToken);
                        }
                        catch (DbException ex) when (IsMissingSchemaObject(ex))
                        {
                            // MySQL keeps the transaction, and the inserted rows with it, after a failed statement
                            options.WarningCantClearWebhookSchedule(ex);
                            clearSchedule = false;
                        }
                    }

                    await tx.CommitAsync(_cancellationToken);

                    break;
                }
                catch (Exception ex) when (ex is not OperationCanceledException && attempt < 5)
                {
                    // a deadlock or a lock wait with a claim of the live sender: the transaction is rolled back as a
                    // whole, the rows of the statement with it, so the statement runs again from the start
                    await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && !inserted)
                {
                    // a statement that keeps failing is skipped, as RunMysqlFile skips one of any other table
                    options.ErrorRestore(ex);

                    break;
                }
            }
        }

        options.DebugClearWebhookSchedule(cleared);
    }

    /// <summary>
    /// The statements of a dump file, read the way <see cref="PortalTaskBase.RunMysqlFile(Stream, string, string)"/>
    /// reads them: one ends at a line that ends with the delimiter.
    /// </summary>
    internal static async IAsyncEnumerable<string> ReadDumpStatementsAsync(StreamReader reader, string delimiter = ";")
    {
        string commandText;

        while ((commandText = await reader.ReadLineAsync()) != null)
        {
            if (!commandText.EndsWith(delimiter))
            {
                var sb = new StringBuilder(commandText);
                var newline = "";

                while (!newline.EndsWith(delimiter))
                {
                    newline = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(newline))
                    {
                        break;
                    }

                    sb.Append(newline);
                }

                commandText = sb.ToString();
            }

            yield return commandText;
        }
    }

    // A dump brings back a past state of every portal, as an archive does (WebStudioModuleSpecifics.TryPrepareRow):
    // the portals may have delivered the pending events after the dump was taken, so the schedule and the leases of
    // the source's senders are dropped, and a row that was never attempted says why it never was. The dump keeps the
    // ids, so the rows are changed in place. response_payload goes first: MySQL assigns left to right and would
    // otherwise see next_attempt_on already cleared. scheduledOnly limits it to the rows a sender could claim, which
    // the next_attempt_on index finds without reading the rest of the journal.
    private static DbCommand CreateClearWebhookScheduleCommand(DbConnection connection, DbTransaction transaction, bool scheduledOnly)
    {
        var command = connection.CreateCommand().WithTimeout(120);
        command.Transaction = transaction;
        command.CommandText =
            "update webhooks_logs " +
            "set " +
            "  response_payload = case when response_payload is null and attempts = 0 and next_attempt_on is not null then @note else response_payload end, " +
            "  next_attempt_on = null, " +
            "  lease_id = null " +
            (scheduledOnly ? "where next_attempt_on is not null" : "where next_attempt_on is not null or lease_id is not null");

        var note = command.CreateParameter();
        note.ParameterName = "@note";
        note.Value = DbWorker.RestoredFromBackupNote;
        command.Parameters.Add(note);

        return command;
    }

    private async Task ClearWebhookScheduleAsync()
    {
        try
        {
            await using var connection = DbFactory.OpenConnection();

            var count = await CreateClearWebhookScheduleCommand(connection, null, scheduledOnly: false).ExecuteNonQueryAsync(_cancellationToken);
            options.DebugClearWebhookSchedule(count);
        }
        catch (DbException ex) when (IsMissingSchemaObject(ex))
        {
            // a dump of a version without the delivery schedule has nothing to clear. Any other fault (a timeout, a
            // deadlock) fails the restore: left as it is, the whole schedule of every restored portal would go out again
            options.WarningCantClearWebhookSchedule(ex);
        }
    }

    // unknown column or table, by the SQLSTATE of MySQL (42S22, 42S02) and of PostgreSQL (42703, 42P01)
    private static bool IsMissingSchemaObject(DbException ex)
    {
        return ex.SqlState is "42S22" or "42S02" or "42703" or "42P01";
    }

    private async Task RestoreFromDumpFile(IDataReadOperator dataReader, string fileName1, string fileName2 = null, string db = null)
    {
        options.DebugRestoreFrom(fileName1);
        await using (var stream = dataReader.GetEntry(fileName1))
        {
            await RunMysqlFile(stream, db);
        }
        await SetStepCompleted();

        options.DebugRestoreFrom(fileName2);
        if (fileName2 != null)
        {
            await using (var stream = dataReader.GetEntry(fileName2))
            {
                await RunMysqlFile(stream, db);
            }

            await SetStepCompleted();
        }
    }

    public List<object[]> ExecuteList(DbCommand command)
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

    private ConnectionStringSettings GetConnectionString(int id, string connectionString)
    {
        connectionString += ";convert zero datetime=True";
        return new ConnectionStringSettings("mailservice-" + id, connectionString, "MySql.Data.MySqlClient");
    }

    private class SqlComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y)
            {
                return 0;
            }

            if (!string.IsNullOrEmpty(x))
            {
                var splittedX = x.Split('.');
                if (splittedX.Length <= 2)
                {
                    return -1;
                }

                if (splittedX[1] == "upgrade")
                {
                    return 1;
                }

                if (splittedX[1].StartsWith("upgrade") && !string.IsNullOrEmpty(y))
                {
                    var splittedY = y.Split('.');
                    if (splittedY.Length <= 2)
                    {
                        return 1;
                    }

                    if (splittedY[1] == "upgrade")
                    {
                        return -1;
                    }

                    if (splittedY[1].StartsWith("upgrade"))
                    {
                        return string.Compare(x, y, StringComparison.Ordinal);
                    }
                }

                return -1;
            }

            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }

    private async Task DoRestoreStorage(IDataReadOperator dataReader)
    {
        options.DebugBeginRestoreStorage();

        var fileGroups = GetFilesToProcess(dataReader).GroupBy(file => file.Module).ToList();
        var groupsProcessed = 0;
        foreach (var group in fileGroups)
        {
            foreach (var file in group)
            {
                var storage = await StorageFactory.GetStorageAsync(Dump ? file.Tenant : _columnMapper.GetTenantMapping(), group.Key);
                var quotaController = storage.QuotaController;
                storage.SetQuotaController(null);

                try
                {
                    var adjustedPath = file.Path;
                    var module = ModuleProvider.GetByStorageModule(file.Module, file.Domain);
                    if (module == null || module.TryAdjustFilePath(Dump, _columnMapper, ref adjustedPath))
                    {
                        var key = file.GetZipKey();
                        if (Dump)
                        {
                            key = CrossPlatform.PathCombine(KeyHelper.GetStorage(), key);
                        }

                        await using var stream = dataReader.GetEntry(key);
                        try
                        {
                            await storage.SaveAsync(file.Domain, adjustedPath, module != null ? module.PrepareData(key, stream, _columnMapper) : stream, _cancellationToken);
                        }
                        catch (Exception error)
                        {
                            options.WarningCantRestoreFile(file.Module, file.Path, error);
                        }
                    }
                }
                finally
                {
                    if (quotaController != null)
                    {
                        storage.SetQuotaController(quotaController);
                    }
                }
            }

            await SetCurrentStepProgress((int)(++groupsProcessed * 100 / (double)fileGroups.Count));
        }

        if (fileGroups.Count == 0)
        {
            await SetStepCompleted();
        }

        options.DebugEndRestoreStorage();
    }

    private async Task DoDeleteStorageAsync(List<string> storageModules, IEnumerable<Tenant> tenants)
    {
        options.DebugBeginDeleteStorage();

        foreach (var tenant in tenants)
        {
            foreach (var module in storageModules)
            {
                var storage = await StorageFactory.GetStorageAsync(tenant.Id, module, _region);
                var domains = StorageFactoryConfig.GetDomainList(module, region: _region).ToList();

                domains.Add(string.Empty); //instead storage.DeleteFiles("\\", "*.*", true);

                foreach (var domain in domains)
                {
                    await ActionInvoker.TryAsync(
                        async state =>
                        {
                            if (await storage.IsDirectoryAsync((string)state))
                            {
                                await storage.DeleteFilesAsync((string)state, "\\", "*.*", true);
                            }
                        },
                        domain,
                        5,
                        onFailure: error => Logger.WarningCanNotDeleteFilesForDomain(domain, error)
                    );
                }

                await SetStepCompleted();
            }
        }

        Logger.DebugEndDeleteStorage();
    }

    private static List<BackupFileInfo> GetFilesToProcess(IDataReadOperator dataReader)
    {
        using var stream = dataReader.GetEntry(KeyHelper.GetStorageRestoreInfoZipKey());
        if (stream == null)
        {
            return [];
        }

        using var reader = new StreamReader(stream);
        var restoreInfo = XElement.Load(reader);

        return restoreInfo.Elements("file").Select(BackupFileInfo.FromXElement).ToList();
    }

    private void SetTenantActive(int tenantId)
    {
        using var connection = DbFactory.OpenConnection();
        var commandText = string.Format(
            "update tenants_tenants " +
            "set " +
            "  status={0}, " +
            "  last_modified='{1}', " +
            "  statuschanged='{1}' " +
            "where id = '{2}'",
            (int)TenantStatus.Active,
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            tenantId);

        var command = connection.CreateCommand().WithTimeout(120);
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}