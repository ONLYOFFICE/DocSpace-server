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

namespace ASC.Files.Core.Helpers;

[Scope]
public class FormFillingReportCreator(
    ILogger<FormFillingReportCreator> logger,
    ExportToXLSX exportToXLSX,
    ExternalDatabaseClient externalDatabaseClient,
    BuiltinFormsDatabaseClient builtinFormsDatabaseClient,
    IDaoFactory daoFactory,
    SocketManager socketManager,
    IHttpClientFactory clientFactory,
    TenantManager tenantManager,
    AuthContext authContext,
    IEventBus eventBus,
    FactoryIndexerForm factoryIndexerForm,
    FactoryIndexerFormMetadata factoryIndexerFormMetadata,
    CoreBaseSettings coreBaseSettings,
    IDistributedLockProvider distributedLockProvider,
    FormFieldsExtractor formFieldsExtractor,
    CommonLinkUtility commonLinkUtility,
    IFusionCacheProvider cacheProvider)
{
    private static readonly TimeSpan _tableGapsCheckInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _tableGapsAttemptDuration = TimeSpan.FromDays(1);

    private readonly IFusionCache _cache = cacheProvider.GetMemoryCache();

    private static readonly TimeSpan _fieldsReadTimeout = TimeSpan.FromSeconds(10);

    private static readonly Regex _builtinTableNameRegex = new(@"^form_(\d+)_v(\d+)$", RegexOptions.Compiled);

    private static readonly HashSet<string> _checkboxOffStates =
        new(StringComparer.OrdinalIgnoreCase) { "off", "false", "0", "no", "n", "unchecked" };

    public async Task UpdateFormFillingReport<T>(int originalFormId, int originalFormVersion, int roomId, int resultFormNumber, string formsDataUrl, File<T> formsDataFile, bool sendFormToExternalDB, bool settingsSaveFormAsXLSX)
    {
        await GetSubmitFormsData(formsDataFile, originalFormId, originalFormVersion, roomId, resultFormNumber, formsDataUrl);

        await MigrateFormVersionAsync(roomId, originalFormId, originalFormVersion);

        var fileId = formsDataFile.Id is int id ? id : 0;
        if (sendFormToExternalDB && externalDatabaseClient.IsEnabled())
        {
            await PublishExternalDbSubmissionAsync(sendFormToExternalDB, originalFormId, originalFormVersion, roomId, fileId, resultFormNumber, formsDataUrl);
        }
        else
        {
            await PublishBuiltinDbSubmissionAsync(originalFormId, originalFormVersion, roomId, fileId, resultFormNumber, formsDataUrl);
        }

        if (settingsSaveFormAsXLSX)
        {
            await exportToXLSX.UpdateXlsxReport(roomId, originalFormId, originalFormVersion, isNewFile: false);
        }
    }

    public async Task SyncExternalDbAsync(Folder<int> room, int originalFormId, int originalFormVersion)
    {
        await PublishExternalDbSubmissionAsync(room.SettingsSendFormToExternalDB, originalFormId, originalFormVersion,
            room.Id, fileId: 0, resultFormNumber: 0, formsDataUrl: null);

        // The built-in database is completed from the index at once too, without waiting for the next use of the form.
        if (GetBuiltinDb() != null)
        {
            await eventBus.PublishAsync(new BuiltinDbFormSubmissionIntegrationEvent(
                authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, originalFormVersion,
                room.Id, fileId: 0, resultFormNumber: 0, formsDataUrl: null, commonLinkUtility.ServerRootPath));
        }
    }

    private async Task PublishExternalDbSubmissionAsync(bool sendFormToExternalDB, int originalFormId, int originalFormVersion, int roomId, int fileId, int resultFormNumber, string formsDataUrl)
    {
        if (!sendFormToExternalDB || !externalDatabaseClient.IsEnabled())
        {
            return;
        }

        await eventBus.PublishAsync(new ExternalDbFormSubmissionIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, originalFormVersion,
            roomId, fileId, resultFormNumber, formsDataUrl, commonLinkUtility.ServerRootPath));
    }

    // Unlike the external database, the built-in one ignores the room's send-to-database setting. Without a database the
    // event still decides the table of the form version, which the spreadsheet report follows.
    private async Task PublishBuiltinDbSubmissionAsync(int originalFormId, int originalFormVersion, int roomId, int fileId, int resultFormNumber, string formsDataUrl)
    {
        await eventBus.PublishAsync(new BuiltinDbFormSubmissionIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, originalFormVersion,
            roomId, fileId, resultFormNumber, formsDataUrl, commonLinkUtility.ServerRootPath));

        // The first submission makes the form's responses analysable.
        if (resultFormNumber == 1 && GetBuiltinDb() != null)
        {
            var originalForm = await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId);
            if (originalForm != null)
            {
                await socketManager.UpdateFileAsync(originalForm);
            }
        }
    }

    public async Task ExportToExternalDbAsync(int fileId, int originalFormId, int originalFormVersion, int roomId, int resultFormNumber, string formsDataUrl)
    {
        var formFilling = await DecideVersionTableAsync(externalDatabaseClient, originalFormId, originalFormVersion);
        var tableName = await WriteSubmissionAsync(externalDatabaseClient, formFilling, fileId, originalFormId, originalFormVersion, roomId, formsDataUrl);
        if (tableName != null)
        {
            await SetExternalDbTableNameAsync(originalFormId, tableName);
        }
    }

    // Without a database for the submission only the table of the version is decided.
    public async Task ExportToBuiltinDbAsync(int fileId, int originalFormId, int originalFormVersion, int roomId, string formsDataUrl)
    {
        var db = GetBuiltinDb();
        var formFilling = await DecideVersionTableAsync(db, originalFormId, originalFormVersion);
        if (db != null)
        {
            await WriteSubmissionAsync(db, formFilling, fileId, originalFormId, originalFormVersion, roomId, formsDataUrl);
        }
    }

    // Returns the table the submission went to, null when it was left out.
    private async Task<string> WriteSubmissionAsync(ExternalDatabaseClient db, FormFillingProperties<int> formFilling, int fileId, int originalFormId, int originalFormVersion, int roomId, string formsDataUrl)
    {
        // A confirmed rename can move the version to another table meanwhile, so the table is resolved again inside its lock.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var tableVersion = GetTableVersion(formFilling, originalFormVersion);
            var tableName = GetTableName(originalFormId, tableVersion);

            await using (await AcquireTableLockAsync(tableName))
            {
                formFilling = await GetFormFillingAsync(originalFormId);
                if (GetTableVersion(formFilling, originalFormVersion) != tableVersion)
                {
                    continue;
                }

                // A new table is filled from the search index, which already holds this submission. Without the index the
                // built-in one is left uncreated rather than created incomplete; the external one takes the submission,
                // and its gap sync completes the table later.
                if (!await EnsureTableLockedAsync(db, tableName, originalFormId, tableVersion, roomId, formFilling)
                    && db is BuiltinFormsDatabaseClient)
                {
                    return null;
                }

                var (row, columns) = await ReadSubmissionAsync(fileId, formsDataUrl);

                await db.CreateTableAndUpsertAsync(
                    tableName,
                    columns.Select(c => c with { Name = MapToTableColumn(formFilling, originalFormVersion, c.Name) }).ToList(),
                    MapToTableColumns(formFilling, originalFormVersion, row),
                    keyColumn: "form_id");

                return tableName;
            }
        }

        logger.WarnSubmissionSkipped(fileId, originalFormId, originalFormVersion);
        return null;
    }

    // The built-in database, when it takes the submissions: no external database is connected.
    private BuiltinFormsDatabaseClient GetBuiltinDb()
    {
        return !externalDatabaseClient.IsEnabled() && builtinFormsDatabaseClient.IsEnabled() ? builtinFormsDatabaseClient : null;
    }

    // The database the submissions of the room go to; none when the external one is connected but the room does not
    // send to it.
    private async Task<ExternalDatabaseClient> GetFormsDbAsync(int roomId)
    {
        return externalDatabaseClient.IsEnabled()
            ? GetFormsDb(await daoFactory.GetFolderDao<int>().GetFolderAsync(roomId))
            : GetBuiltinDb();
    }

    private ExternalDatabaseClient GetFormsDb(Folder<int> room)
    {
        if (!externalDatabaseClient.IsEnabled())
        {
            return GetBuiltinDb();
        }

        return room is { SettingsSendFormToExternalDB: true } ? externalDatabaseClient : null;
    }

    /// <summary>
    /// The built-in table of the form version, restored from the search index when it was dropped as idle or never
    /// created. Null when there is nothing to restore.
    /// </summary>
    public async Task<string> EnsureBuiltinDbTableAsync(int originalFormId, int originalFormVersion, int roomId)
    {
        var formFilling = await GetFormFillingAsync(originalFormId);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var tableVersion = GetTableVersion(formFilling, originalFormVersion);
            var tableName = GetTableName(originalFormId, tableVersion);

            await TouchBuiltinDbTableAsync(tableName);
            if (await builtinFormsDatabaseClient.TableExistsAsync(tableName))
            {
                await FillBuiltinDbTableGapsAsync(originalFormId, originalFormVersion, tableVersion, tableName, roomId, formFilling);
                return tableName;
            }

            await using (await AcquireTableLockAsync(tableName))
            {
                formFilling = await GetFormFillingAsync(originalFormId);
                if (GetTableVersion(formFilling, originalFormVersion) != tableVersion)
                {
                    continue;
                }

                return await EnsureTableLockedAsync(builtinFormsDatabaseClient, tableName, originalFormId, tableVersion, roomId, formFilling) ? tableName : null;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds to the built-in table of the form version the submissions the search index holds and the table lacks, or
    /// restores the table, at once rather than at most once in a few minutes as on use.
    /// </summary>
    public async Task SyncBuiltinDbTableAsync(int originalFormId, int originalFormVersion, int roomId)
    {
        if (GetBuiltinDb() == null)
        {
            return;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var formFilling = await GetFormFillingAsync(originalFormId);
            var tableVersion = GetTableVersion(formFilling, originalFormVersion);
            var tableName = GetTableName(originalFormId, tableVersion);

            await using (await AcquireTableLockAsync(tableName))
            {
                formFilling = await GetFormFillingAsync(originalFormId);
                if (GetTableVersion(formFilling, originalFormVersion) != tableVersion)
                {
                    continue;
                }

                if (await EnsureTableLockedAsync(builtinFormsDatabaseClient, tableName, originalFormId, tableVersion, roomId, formFilling))
                {
                    await FillTableGapsLockedAsync(builtinFormsDatabaseClient, tableName, originalFormId, originalFormVersion, tableVersion, roomId, formFilling);
                }

                return;
            }
        }
    }

    // Submissions whose export failed are added from the index, at most once in a few minutes per table. Best effort.
    private async Task FillBuiltinDbTableGapsAsync(int originalFormId, int originalFormVersion, int tableVersion, string tableName, int roomId, FormFillingProperties<int> formFilling)
    {
        try
        {
            var tenantId = tenantManager.GetCurrentTenantId();
            await _cache.GetOrSetAsync<bool>(GetTableGapsCacheKey(tenantId, tableName), async (_, _) =>
            {
                var (missing, counts) = await CountMissingRowsAsync(builtinFormsDatabaseClient, tableName, originalFormId, GetTableVersions(formFilling, tableVersion), roomId);
                var attemptKey = GetTableGapsAttemptCacheKey(tenantId, tableName);
                if (missing != true || (await _cache.TryGetAsync<string>(attemptKey)).GetValueOrDefault() == counts)
                {
                    return true;
                }

                // Rows the index holds but that cannot be built stay missing; they are not tried again until the counts change.
                await _cache.SetAsync(attemptKey, counts, _tableGapsAttemptDuration);

                await using (await AcquireTableLockAsync(tableName))
                {
                    formFilling = await GetFormFillingAsync(originalFormId);
                    if (GetTableVersion(formFilling, originalFormVersion) == tableVersion)
                    {
                        await FillTableGapsLockedAsync(builtinFormsDatabaseClient, tableName, originalFormId, originalFormVersion, tableVersion, roomId, formFilling);
                    }
                }

                return true;
            }, _tableGapsCheckInterval);
        }
        catch (Exception e)
        {
            logger.WarnTableGapFillFailed(e, tableName);
        }
    }

    public static string GetTableGapsCacheKey(int tenantId, string tableName) => tenantId + "formsdbgaps" + tableName;

    public static string GetTableGapsAttemptCacheKey(int tenantId, string tableName) => tenantId + "formsdbgapsattempt" + tableName;

    // Under the table lock. A built-in table is marked as used before the existence check, so the idle-table cleanup
    // cannot drop it in between.
    private async Task<bool> EnsureTableLockedAsync(ExternalDatabaseClient db, string tableName, int originalFormId, int tableVersion, int roomId, FormFillingProperties<int> formFilling)
    {
        if (db is BuiltinFormsDatabaseClient)
        {
            await TouchBuiltinDbTableAsync(tableName);
        }

        return await db.TableExistsAsync(tableName)
            || await RestoreTableAsync(db, tableName, originalFormId, tableVersion, roomId, formFilling);
    }

    // Best effort: a failed mark must not fail the submission or the analysis.
    private async Task TouchBuiltinDbTableAsync(string tableName)
    {
        try
        {
            await builtinFormsDatabaseClient.TouchTableAsync(tableName);
        }
        catch (Exception e)
        {
            logger.WarnBuiltinDbTouchFailed(e, tableName);
        }
    }

    /// <summary>
    /// Drops a built-in table unused since <paramref name="cutoff"/> (UTC) only when the search index can rebuild it
    /// whole on the next use: the table is the one its versions write to, and the index holds at least as many of their
    /// submissions as the table, fewer than one rebuild reads. The table of a form that no longer exists is dropped.
    /// </summary>
    public async Task<bool> DropIdleBuiltinTableAsync(string tableName, DateTime cutoff)
    {
        var name = _builtinTableNameRegex.Match(tableName);
        if (!name.Success
            || !int.TryParse(name.Groups[1].Value, out var originalFormId)
            || !int.TryParse(name.Groups[2].Value, out var tableVersion))
        {
            return false;
        }

        await using (await AcquireTableLockAsync(tableName))
        {
            if (await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId) == null)
            {
                return await builtinFormsDatabaseClient.DropIdleTableAsync(tableName, cutoff) == IdleTableDrop.Dropped;
            }

            // A table whose versions now write elsewhere waits for its merge, which checks every row before dropping.
            var formFilling = await GetFormFillingAsync(originalFormId);
            if (formFilling is not { RoomId: > 0 } || GetTableVersion(formFilling, tableVersion) != tableVersion)
            {
                return false;
            }

            var versions = GetTableVersions(formFilling, tableVersion);
            var (success, count) = await factoryIndexerForm.TryCountAsync(r =>
                r.Where(s => s.RoomId, formFilling.RoomId)
                 .Where(s => s.OriginalFormId, originalFormId)
                 .In(s => s.OriginalFormVersion, versions));

            if (!success)
            {
                logger.WarnIdleTableKeptIndexUnavailable(tableName);
                return false;
            }

            if (count >= BaseIndexer<DbFormsItemDataSearch>.QueryLimit)
            {
                logger.InfoIdleTableKeptTooLarge(tableName, count);
                return false;
            }

            var result = await builtinFormsDatabaseClient.DropIdleTableAsync(tableName, cutoff, count);
            if (result == IdleTableDrop.MoreRowsThanIndex)
            {
                logger.WarnIdleTableKeptIndexBehind(tableName, count);
            }

            return result == IdleTableDrop.Dropped;
        }
    }

    // Created and filled in one transaction; holds every version linked to the table, under the latest column names.
    private async Task<bool> RestoreTableAsync(ExternalDatabaseClient db, string tableName, int originalFormId, int tableVersion, int roomId, FormFillingProperties<int> formFilling)
    {
        var versions = GetTableVersions(formFilling, tableVersion);

        await factoryIndexerForm.RefreshAsync();
        var (countSuccess, count) = await factoryIndexerForm.TryCountAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .In(s => s.OriginalFormVersion, versions));

        if (!countSuccess || count == 0)
        {
            return false;
        }

        if (count >= BaseIndexer<DbFormsItemDataSearch>.QueryLimit)
        {
            logger.WarnTableRestoreTooLarge(tableName, count);
            return false;
        }

        var (success, submissions) = await factoryIndexerForm.TrySelectAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .In(s => s.OriginalFormVersion, versions)
             .Limit(0, BaseIndexer<DbFormsItemDataSearch>.QueryLimit));

        if (!success || submissions.Count == 0)
        {
            return false;
        }

        var (columns, rows, _) = await BuildTableRowsAsync(originalFormId, formFilling, versions, submissions, tableName);
        if (rows.Count == 0)
        {
            return false;
        }

        await db.CreateTableAndUpsertAsync(tableName, columns.Values.ToList(), rows.Select(r => r.Row).ToList(), keyColumn: "form_id");

        return true;
    }

    // The rows of the submissions under the latest column names of their table, and the columns of the versions. Each
    // version is converted by its own metadata: a date keeps the format it was submitted in. Incomplete when the fields
    // of a version with submissions are unknown.
    private async Task<(Dictionary<string, DbColumnDefinition> Columns, List<(int Id, Dictionary<string, object> Row)> Rows, bool Incomplete)> BuildTableRowsAsync(
        int originalFormId, FormFillingProperties<int> formFilling, IEnumerable<int> versions, IReadOnlyCollection<DbFormsItemDataSearch> submissions, string tableName)
    {
        var culture = tenantManager.GetCurrentTenant().GetCulture();
        var columns = new Dictionary<string, DbColumnDefinition>();
        var rows = new List<(int Id, Dictionary<string, object> Row)>();
        var incomplete = false;

        foreach (var version in versions.Order())
        {
            var versionSubmissions = submissions.Where(s => s.OriginalFormVersion == version).ToList();
            var normalizedMeta = await GetExportMetadataAsync(originalFormId, version, versionSubmissions);
            if (normalizedMeta.Count == 0 && versionSubmissions.Count > 0)
            {
                incomplete = true;
                continue;
            }

            foreach (var column in BuildColumnDefinitions(normalizedMeta))
            {
                var tableColumn = column with { Name = MapToTableColumn(formFilling, version, column.Name) };
                columns[tableColumn.Name] = columns.TryGetValue(tableColumn.Name, out var earlier)
                    ? MergeColumn(earlier, tableColumn)
                    : tableColumn;
            }

            foreach (var item in versionSubmissions)
            {
                var row = BuildIndexedRowData(item, normalizedMeta, culture, tableName);
                if (row != null)
                {
                    rows.Add((item.Id, MapToTableColumns(formFilling, version, row)));
                }
            }
        }

        return (columns, rows, incomplete);
    }

    // A column shared by several versions accepts the answers of all of them, as a merge of their tables widens it.
    internal static DbColumnDefinition MergeColumn(DbColumnDefinition earlier, DbColumnDefinition later)
    {
        if (earlier.Type != DbColumnType.Enum || later.Type != DbColumnType.Enum)
        {
            return later;
        }

        // A version that took any text leaves the column without a list.
        return earlier.EnumValues is { Count: > 0 } && later.EnumValues is { Count: > 0 }
            ? later with { EnumValues = later.EnumValues.Union(earlier.EnumValues, StringComparer.Ordinal).ToList() }
            : later with { EnumValues = null };
    }

    private Task<IDistributedLockHandle> AcquireTableLockAsync(string tableName)
    {
        return distributedLockProvider.TryAcquireFairLockAsync($"forms_db_table_{tenantManager.GetCurrentTenantId()}_{tableName}");
    }

    // The first submission of a version decides at the latest: it can come before the background check or without a start.
    private async Task<FormFillingProperties<int>> DecideVersionTableAsync(ExternalDatabaseClient db, int originalFormId, int originalFormVersion)
    {
        var formFilling = await GetFormFillingAsync(originalFormId);
        if (formFilling == null
            || formFilling.PendingVersionLinks?.Exists(p => p.Version == originalFormVersion) != true
               && (originalFormVersion < formFilling.FieldsCheckedVersion
                   || !NeedsCheck(formFilling, await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId, originalFormVersion))))
        {
            return formFilling;
        }

        return await UpdateVersionStateAsync(originalFormId, originalFormVersion, async (form, state) =>
        {
            var changed = NeedsCheck(state, form) && await CheckVersionAsync(db, form, state);
            return await ApplyOnFirstSubmissionAsync(db, form, state) || changed;
        });
    }

    /// <summary>
    /// Asks the background check of the current version of a started original form when it was not compared yet; a form
    /// being edited is not started, so its saves read no fields. Runs whatever database takes the submissions, since the
    /// spreadsheet report follows the same decision.
    /// </summary>
    public async Task RequestVersionCheckAsync(File<int> form, FormFillingProperties<int> formFilling)
    {
        if (formFilling is not { StartFilling: true } || formFilling.OriginalFormId != form.Id || !NeedsCheck(formFilling, form))
        {
            return;
        }

        await PublishVersionCheckAsync(form.Id);
    }

    private Task PublishVersionCheckAsync(int originalFormId)
    {
        return eventBus.PublishAsync(new FormVersionCheckIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, commonLinkUtility.ServerRootPath));
    }

    /// <summary>
    /// Compares the fields of the current version of an original form in a form-filling room with the last version that
    /// has data and decides the table of the version; moves the decided versions that have submissions into the shared
    /// table.
    /// </summary>
    public async Task CheckVersionFieldsAsync(int originalFormId)
    {
        var form = await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId);
        if (form is not { IsPdf: true }
            || await DocSpaceHelper.GetParentRoom(form, daoFactory.GetFolderDao<int>()) is not { FolderType: FolderType.FillingFormsRoom } room)
        {
            return;
        }

        var db = GetFormsDb(room);

        await UpdateVersionStateAsync(originalFormId, null, async (current, state) =>
        {
            if (state.OriginalFormId != current.Id)
            {
                return false;
            }

            var changed = NeedsCheck(state, current) && await CheckVersionAsync(db, current, state);
            foreach (var link in state.PendingVersionLinks?.ToList() ?? [])
            {
                if (await HasDataAsync(db, originalFormId, link.Version, state.RoomId))
                {
                    await MergeVersionAsync(db, originalFormId, state, link);
                    changed = true;
                }
            }

            return changed;
        });
    }

    // Every change of the state is made from properties read under the version lock.
    private async Task<FormFillingProperties<int>> UpdateVersionStateAsync(int originalFormId, int? version, Func<File<int>, FormFillingProperties<int>, Task<bool>> change)
    {
        await using (await AcquireVersionLockAsync(originalFormId))
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var state = await GetFormFillingAsync(originalFormId);
            var form = version.HasValue ? await fileDao.GetFileAsync(originalFormId, version.Value) : await fileDao.GetFileAsync(originalFormId);
            if (form == null || state == null)
            {
                return state;
            }

            var pending = GetPendingVersions(state);
            if (!await change(form, state))
            {
                return state;
            }

            await SaveVersionStateAsync(originalFormId, state);

            // The report showed a pending version with the shared ones; the database refused it, so it gets its sheet back.
            if (pending.Exists(v => state.VersionLinks?.Exists(l => l.Version == v) != true && !GetPendingVersions(state).Contains(v)))
            {
                await RequestReportRebuildAsync(state.RoomId, form);
            }

            return state;
        }
    }

    private static List<int> GetPendingVersions(FormFillingProperties<int> formFilling)
    {
        return (formFilling?.PendingVersionLinks ?? []).Select(p => p.Version).ToList();
    }

    // Best effort, on behalf of the form owner: the next submission rebuilds the report anyway.
    private async Task RequestReportRebuildAsync(int roomId, File<int> form)
    {
        try
        {
            if (await daoFactory.GetFolderDao<int>().GetFolderAsync(roomId) is { SettingsSaveFormAsXLSX: true })
            {
                await eventBus.PublishAsync(new FormFillingReportIntegrationEvent(
                    form.CreateBy, tenantManager.GetCurrentTenantId(), roomId, form.Id, form.Version, commonLinkUtility.ServerRootPath));
            }
        }
        catch (Exception e)
        {
            logger.WarnFormReportRebuildFailed(e, form.Id);
        }
    }

    // A version compared already is compared again only when it was overwritten (a save that replaces the version).
    private static bool NeedsCheck(FormFillingProperties<int> formFilling, File<int> form)
    {
        return formFilling != null && form != null
            && (form.Version > formFilling.FieldsCheckedVersion
                || form.Version == formFilling.FieldsCheckedVersion && form.ModifiedOn.Ticks != formFilling.FieldsCheckedStamp);
    }

    private Task<IDistributedLockHandle> AcquireVersionLockAsync(int originalFormId)
    {
        return distributedLockProvider.TryAcquireFairLockAsync($"forms_db_version_{tenantManager.GetCurrentTenantId()}_{originalFormId}");
    }

    // Under the version lock, so the state replaces the saved one; the lock of the submissions keeps the other properties.
    private async Task SaveVersionStateAsync(int originalFormId, FormFillingProperties<int> state)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"fillform_{state.RoomId}_{originalFormId}"))
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var current = await fileDao.GetProperties(originalFormId);
            if (current?.FormFilling == null)
            {
                return;
            }

            CopyVersionState(state, current.FormFilling);
            await fileDao.SaveProperties(originalFormId, current);
        }
    }

    /// <summary>
    /// Saves form properties read earlier without losing the version links saved meanwhile in the background.
    /// </summary>
    public async Task SaveFormPropertiesAsync<T>(T formId, EntryProperties<T> properties)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        if (properties?.FormFilling == null)
        {
            await fileDao.SaveProperties(formId, properties);
            return;
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"fillform_{properties.FormFilling.RoomId}_{formId}"))
        {
            var current = (await fileDao.GetProperties(formId))?.FormFilling;
            if (current != null)
            {
                CopyVersionState(current, properties.FormFilling);
            }

            await fileDao.SaveProperties(formId, properties);
        }
    }

    private static void CopyVersionState<T>(FormFillingProperties<T> from, FormFillingProperties<T> to)
    {
        to.FieldsCheckedVersion = from.FieldsCheckedVersion;
        to.FieldsCheckedStamp = from.FieldsCheckedStamp;
        to.VersionLinks = from.VersionLinks;
        to.PendingVersionLinks = from.PendingVersionLinks;
        to.EditorFieldRenames = from.EditorFieldRenames;
    }

    private async Task<FormFillingProperties<int>> GetFormFillingAsync(int originalFormId)
    {
        return (await daoFactory.GetFileDao<int>().GetProperties(originalFormId))?.FormFilling;
    }

    // The version whose table the form version writes to: the version it continues, or its own.
    internal static int GetTableVersion(FormFillingProperties<int> formFilling, int version)
    {
        return formFilling?.VersionLinks?.FirstOrDefault(l => l.Version == version)?.TableVersion ?? version;
    }

    /// <summary>The versions of the form whose responses are in the table the given version writes to.</summary>
    public static List<int> GetVersionsInTable(FormFillingProperties<int> formFilling, int version)
    {
        return GetTableVersions(formFilling, GetTableVersion(formFilling, version)).Order().ToList();
    }

    /// <summary>
    /// The earlier versions of the form that have responses outside the table the given version writes to. Null when the
    /// search index cannot tell, so that nothing is said about responses it did not show.
    /// </summary>
    public async Task<List<int>> GetVersionsWithResponsesApartAsync(int originalFormId, FormFillingProperties<int> formFilling, int version)
    {
        var inTable = GetVersionsInTable(formFilling, version);
        var roomId = formFilling.RoomId;

        // Only a version with metadata can have responses; a version saved and never filled has none to count. Usually
        // none or one version is apart, so they are counted one by one.
        var (success, metadata) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.RoomId, roomId));
        if (!success)
        {
            return null;
        }

        var apart = new List<int>();
        foreach (var candidate in metadata.Select(m => m.OriginalFormVersion).Where(v => v < version && !inTable.Contains(v)).Distinct().Order())
        {
            var (counted, count) = await CountSubmissionsAsync(originalFormId, candidate, roomId);
            if (!counted)
            {
                return null;
            }

            if (count > 0)
            {
                apart.Add(candidate);
            }
        }

        return apart;
    }

    internal static int[] GetTableVersions(FormFillingProperties<int> formFilling, int tableVersion)
    {
        return (formFilling?.VersionLinks ?? [])
            .Where(l => l.TableVersion == tableVersion)
            .Select(l => l.Version)
            .Prepend(tableVersion)
            .ToArray();
    }

    // A column of the version under its name in the table: the renames of the later versions linked to the same table
    // are applied in order.
    internal static string MapToTableColumn(FormFillingProperties<int> formFilling, int version, string column)
    {
        foreach (var link in GetLaterLinks(formFilling, version))
        {
            var rename = link.Renames?.FirstOrDefault(r => NormalizeColumnName(r.OldKey) == column);
            if (rename != null)
            {
                column = NormalizeColumnName(rename.NewKey);
            }
        }

        return column;
    }

    private static IEnumerable<FormVersionLink> GetLaterLinks(FormFillingProperties<int> formFilling, int version)
    {
        var tableVersion = GetTableVersion(formFilling, version);

        return (formFilling?.VersionLinks ?? [])
            .Where(l => l.TableVersion == tableVersion && l.Version > version)
            .OrderBy(l => l.Version);
    }

    private static Dictionary<string, object> MapToTableColumns(FormFillingProperties<int> formFilling, int version, Dictionary<string, object> row)
    {
        return row.ToDictionary(kv => MapToTableColumn(formFilling, version, kv.Key), kv => kv.Value);
    }

    private async Task<(Dictionary<string, object> Row, List<DbColumnDefinition> Columns)> ReadSubmissionAsync(int fileId, string formsDataUrl)
    {
#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
        using var exportRequest = new HttpRequestMessage
        {
            RequestUri = new Uri(formsDataUrl),
            Method = HttpMethod.Get
        };
        using var response = await httpClient.SendAsync(exportRequest);

        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsStringAsync();
        var parsed = ParseSubmitAndMetadata(data);

        parsed.Data.FormsData = parsed.Data.FormsData
            .Where(IsExportableField)
            .ToList();

        var normalizedMeta = NormalizeMetadata(parsed.MetaData).ToList();
        var culture = tenantManager.GetCurrentTenant().GetCulture();

        return (BuildRowData(parsed.Data, normalizedMeta, fileId, culture, coreBaseSettings.EnabledCultures), BuildColumnDefinitions(normalizedMeta).ToList());
    }

    // Records the link the version may take. False when the index could not be read, so the version is checked again later.
    private async Task<bool> CheckVersionAsync(ExternalDatabaseClient db, File<int> form, FormFillingProperties<int> state)
    {
        try
        {
            // A version linked or with a table of its own keeps its decision.
            if (state.VersionLinks?.Any(l => l.Version == form.Version) == true
                || await TableExistsOrUnknownAsync(db, GetTableName(form.Id, form.Version)))
            {
                ConsumeEditorRenames(state, form.Version);
                MarkChecked(state, form);
                return true;
            }

            var (link, comparedVersion) = await CompareWithPreviousVersionAsync(form, state);
            if (link == null)
            {
                return false;
            }

            // An earlier version left without submissions has nothing to decide: this one was compared past it.
            foreach (var stale in state.PendingVersionLinks?.Where(p => p.Version < form.Version).ToList() ?? [])
            {
                if (!await HasDataAsync(db, form.Id, stale.Version, state.RoomId))
                {
                    state.PendingVersionLinks.Remove(stale);
                }
            }

            ApplyVersionDecision(state, form.Version, link, comparedVersion);

            MarkChecked(state, form);
            return true;
        }
        catch (Exception e)
        {
            logger.WarnVersionCheckFailed(e, form.Id, form.Version);
            return false;
        }
    }

    // The sessions saved up to the compared version went into its link; a session the form moved past unsaved never will.
    internal static void ConsumeEditorRenames(FormFillingProperties<int> state, int version)
    {
        state.EditorFieldRenames?.RemoveAll(r => r.SavedVersion <= version || r.SavedVersion == null && r.OpenedVersion < version);
        if (state.EditorFieldRenames?.Count == 0)
        {
            state.EditorFieldRenames = null;
        }
    }

    /// <summary>
    /// The decision about a version, applied to the state. A link carries the editor sessions saved up to it on, so they
    /// go. A version that took none, with no data of its own, is compared past by the next one, which still needs the
    /// sessions after the version this one was compared with: their keys are the ones its own sessions start from.
    /// </summary>
    internal static void ApplyVersionDecision(FormFillingProperties<int> state, int version, FormVersionLink link, int comparedVersion)
    {
        ConsumeEditorRenames(state, link.Version > 0 || comparedVersion == 0 ? version : comparedVersion);

        var pending = state.PendingVersionLinks ??= [];
        pending.RemoveAll(p => p.Version == version);

        if (link.Version > 0)
        {
            pending.Add(link);
        }
    }

    // Under the version lock: the editor sessions of the form open up to the saved version end with this save.
    internal static bool MarkEditorRenamesSaved(FormFillingProperties<int> state, int savedVersion)
    {
        var changed = false;
        foreach (var session in state.EditorFieldRenames?.Where(r => r.SavedVersion == null && r.OpenedVersion <= savedVersion) ?? [])
        {
            session.SavedVersion = savedVersion;
            changed = true;
        }

        return changed;
    }

    private static void MarkChecked(FormFillingProperties<int> state, File<int> form)
    {
        state.FieldsCheckedVersion = form.Version;
        state.FieldsCheckedStamp = form.ModifiedOn.Ticks;
    }

    // Data of the version: its own table, or submissions in the index. An unreadable index counts as data.
    private async Task<bool> HasDataAsync(ExternalDatabaseClient db, int originalFormId, int version, int roomId)
    {
        if (await TableExistsOrUnknownAsync(db, GetTableName(originalFormId, version)))
        {
            return true;
        }

        var (success, count) = await CountSubmissionsAsync(originalFormId, version, roomId);

        return !success || count > 0;
    }

    // An unreachable database leaves the decision to the search index, as when there is none.
    private async Task<bool> TableExistsOrUnknownAsync(ExternalDatabaseClient db, string tableName)
    {
        if (db == null)
        {
            return false;
        }

        try
        {
            return await db.TableExistsAsync(tableName);
        }
        catch (Exception e) when (e is DbException or TimeoutException)
        {
            logger.WarnFormsDbUnavailable(e, tableName);
            return false;
        }
    }

    private Task<(bool Success, long Count)> CountSubmissionsAsync(int originalFormId, int version, int roomId)
    {
        return factoryIndexerForm.TryCountAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, version));
    }

    // The link the version may take, one with no version when it takes none, and the version it was compared with (0 when
    // none). A null link when the index could not be read.
    private async Task<(FormVersionLink Link, int ComparedVersion)> CompareWithPreviousVersionAsync(File<int> form, FormFillingProperties<int> formFilling)
    {
        var none = new FormVersionLink();
        var roomId = formFilling.RoomId;

        await factoryIndexerFormMetadata.RefreshAsync();
        var (metaSuccess, metaResult) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, form.Id)
             .Where(s => s.RoomId, roomId));
        if (!metaSuccess)
        {
            return (null, 0);
        }

        // Compared with the last version whose data is in a table: submitted or linked. A version saved in between and
        // never filled is skipped, so its key changes are asked about together with the later ones.
        DbFormsMetadataSearch previous = null;
        foreach (var candidate in metaResult.Where(m => m.OriginalFormVersion < form.Version && m.Metadata?.Any() == true).OrderByDescending(m => m.OriginalFormVersion))
        {
            if (formFilling.VersionLinks?.Any(l => l.Version == candidate.OriginalFormVersion) == true)
            {
                previous = candidate;
                break;
            }

            var (submittedSuccess, submitted) = await CountSubmissionsAsync(form.Id, candidate.OriginalFormVersion, roomId);
            if (!submittedSuccess)
            {
                return (null, 0);
            }

            if (submitted > 0)
            {
                previous = candidate;
                break;
            }
        }

        if (previous == null)
        {
            return (none, 0);
        }

        var (currentSuccess, currentSubmitted) = await CountSubmissionsAsync(form.Id, form.Version, roomId);
        if (!currentSuccess)
        {
            return (null, 0);
        }

        // A submitted version is described in the index by its submissions; an unfilled one is read from its file, which
        // may have been overwritten since it was last read.
        List<FormMetadata> metadata;
        if (currentSubmitted > 0)
        {
            metadata = (await GetVersionMetadataAsync(form.Id, form.Version)).ToList();
        }
        else
        {
            using var cts = new CancellationTokenSource(_fieldsReadTimeout);
            var json = await formFieldsExtractor.ExtractFieldsJsonAsync(form, cts.Token);
            metadata = ParseSubmitAndMetadata(json).MetaData;

            // Indexed now, so that the next version is compared with this one even if this one is never filled.
            if (metadata.Count > 0)
            {
                await factoryIndexerFormMetadata.IndexAsync(new DbFormsMetadataSearch
                {
                    Id = DbFormsMetadataSearch.ComputeId(form.Id, form.Version),
                    TenantId = tenantManager.GetCurrentTenantId(),
                    OriginalFormId = form.Id,
                    OriginalFormVersion = form.Version,
                    RoomId = roomId,
                    Metadata = metadata
                });
            }
        }

        // Empty for a file that is not an ONLYOFFICE form and for a form without fields.
        if (metadata.Count == 0)
        {
            return (none, 0);
        }

        var tableVersion = GetTableVersion(formFilling, previous.OriginalFormVersion);
        var renames = MatchFields(form.Version, tableVersion, previous, metadata, formFilling,
            GetTableColumns(formFilling, tableVersion, metaResult, form.Version));
        if (renames == null)
        {
            return (none, previous.OriginalFormVersion);
        }

        return (new FormVersionLink
        {
            Version = form.Version,
            TableVersion = tableVersion,
            Renames = renames
        }, previous.OriginalFormVersion);
    }

    /// <summary>
    /// The fields of the version renamed since the last version with data; null when the version needs a table of its
    /// own: a field kept its name under another type, a field took the name of another one, or no field is left. The
    /// renames the editor tracked by field id in the sessions saved since count, with those of the versions in between,
    /// decided the same way. Without the editor, a form whose fields kept their order, types and options while only keys
    /// changed is the same form under new keys; otherwise a field with a new name is a new field, so answers to different
    /// questions do not share a column.
    /// </summary>
    internal static List<FormFieldRename> MatchFields(int version, int tableVersion, DbFormsMetadataSearch previousVersion, List<FormMetadata> current, FormFillingProperties<int> formFilling,
        IReadOnlyCollection<string> tableColumns = null)
    {
        var since = previousVersion.OriginalFormVersion;
        var decided = (formFilling.PendingVersionLinks ?? [])
            .Where(p => p.Version > since && p.Version < version && p.TableVersion == tableVersion)
            .Select(p => (Version: p.Version, OpenedVersion: 0, p.Renames));
        // The sessions saved as one version, by several users, in the order they began to report.
        var sessions = (formFilling.EditorFieldRenames ?? [])
            .Where(r => r.SavedVersion > since && r.SavedVersion <= version && r.OpenedVersion >= since)
            .Select(r => (Version: r.SavedVersion.Value, r.OpenedVersion, r.Renames))
            .ToList();

        // A key of the previous version to its key after the known renames, applied in the order of the versions.
        var known = new Dictionary<string, string>();
        var renamedAt = new Dictionary<string, int>();
        foreach (var (savedVersion, openedVersion, renames) in decided.Concat(sessions).OrderBy(r => r.Version))
        {
            ComposeRenames(known, renamedAt, renames, savedVersion, openedVersion);
        }

        var tracked = sessions.Count > 0;

        var before = NormalizeMetadata(previousVersion.Metadata).ToList();
        var after = NormalizeMetadata(current).ToList();

        // Columns of the table no field of the previous version writes to: those of fields an earlier version removed.
        var decidedState = WithDecidedLinksBefore(formFilling, version);
        var occupied = before.Select(f => MapToTableColumn(decidedState, since, NormalizeColumnName(f.Key))).ToHashSet();
        var foreign = (tableColumns ?? []).Where(c => !occupied.Contains(c)).ToHashSet();

        var applied = new List<FormFieldRename>();
        foreach (var (oldKey, newKey) in known.Where(k => k.Key != k.Value && before.Exists(f => f.Key == k.Key) && after.Exists(f => f.Key == k.Value)))
        {
            // Swapped names, or the name of another field of the previous version: its column holds that field's answers.
            // The same holds for the column a removed field left in the table.
            if (before.Exists(f => f.Key != oldKey && NormalizeColumnName(f.Key) == NormalizeColumnName(newKey))
                || foreign.Contains(NormalizeColumnName(newKey)))
            {
                return null;
            }

            applied.Add(new FormFieldRename { OldKey = oldKey, NewKey = newKey });
        }

        // Fields of the previous version given one key share a column now: the first of them in the form takes it, and the
        // others keep their earlier answers under their old names. Two columns cannot be joined.
        applied = applied
            .GroupBy(a => NormalizeColumnName(a.NewKey))
            .Select(g => g.MinBy(a => before.FindIndex(f => f.Key == a.OldKey)))
            .ToList();

        var renamed = before
            .Select(f => applied.Find(a => a.OldKey == f.Key) is { } a
                ? new FormMetadata { Key = a.NewKey, Type = f.Type, Format = f.Format, PossibleValues = f.PossibleValues }
                : f)
            .ToList();

        if (!tracked && FormFieldMatcher.MatchSameLayout(renamed, after) is { } positional)
        {
            if (positional.Exists(r => foreign.Contains(NormalizeColumnName(r.NewKey))))
            {
                return null;
            }

            return applied.Concat(positional).OrderBy(r => after.FindIndex(f => f.Key == r.NewKey)).ToList();
        }

        if (FormFieldMatcher.CountShared(renamed, after) is null or 0)
        {
            return null;
        }

        return applied.OrderBy(r => after.FindIndex(f => f.Key == r.NewKey)).ToList();
    }

    /// <summary>
    /// The columns of the table under their latest names: the fields of every version linked to it before the given one,
    /// a field later versions removed among them. A decided link renames them; the fields of its version become columns
    /// only with its first submission.
    /// </summary>
    internal static HashSet<string> GetTableColumns(FormFillingProperties<int> formFilling, int tableVersion, IEnumerable<DbFormsMetadataSearch> metadata, int version)
    {
        var decidedState = WithDecidedLinksBefore(formFilling, version);
        var linked = new FormFillingProperties<int> { VersionLinks = formFilling?.VersionLinks?.Where(l => l.Version < version).ToList() };
        var byVersion = (metadata ?? []).GroupBy(m => m.OriginalFormVersion).ToDictionary(g => g.Key, g => g.First().Metadata);

        return GetTableVersions(linked, tableVersion)
            .Where(v => v < version && byVersion.ContainsKey(v))
            .SelectMany(v => NormalizeMetadata(byVersion[v] ?? []).Select(f => MapToTableColumn(decidedState, v, NormalizeColumnName(f.Key))))
            .ToHashSet();
    }

    // The decided links of the versions before the given one: a version checked again does not count its own earlier
    // decision.
    private static FormFillingProperties<int> WithDecidedLinksBefore(FormFillingProperties<int> formFilling, int version)
    {
        return WithDecidedLinks(new FormFillingProperties<int>
        {
            VersionLinks = formFilling?.VersionLinks?.Where(l => l.Version < version).ToList(),
            PendingVersionLinks = formFilling?.PendingVersionLinks?.Where(p => p.Version < version).ToList()
        });
    }

    // The renames of one version or one editor session happen at once, so swapped keys stay swapped. A session reports
    // from the version it opened: a key a decided link renamed away in that version or before now names a field the
    // earlier versions did not have. Only a decided link vouches that the key was vacated: it was matched against the
    // saved fields, while a session's last report may be older than the version it was saved as.
    private static void ComposeRenames(Dictionary<string, string> known, Dictionary<string, int> renamedAt, IEnumerable<FormFieldRename> renames,
        int version, int openedVersion)
    {
        var composed = new List<(string Origin, string NewKey)>();
        foreach (var rename in renames ?? [])
        {
            var origin = known.FirstOrDefault(k => k.Value == rename.OldKey).Key;
            if (origin == null && renamedAt.TryGetValue(rename.OldKey, out var renamedVersion) && renamedVersion <= openedVersion)
            {
                continue;
            }

            composed.Add((origin ?? rename.OldKey, rename.NewKey));
        }

        foreach (var (origin, newKey) in composed)
        {
            known[origin] = newKey;
            if (origin == newKey)
            {
                renamedAt.Remove(origin);
            }
            // The version the key was vacated at, not the latest one that renamed the field further.
            else if (openedVersion == 0)
            {
                renamedAt.TryAdd(origin, version);
            }
        }
    }

    // An editor opens the form this many times at most before the version is saved; more is not a person at work.
    internal const int MaxEditorSessionsPerUser = 20;

    /// <summary>
    /// Keeps the fields a user renamed in an editor session, which the editor tracks by field id, relative to the version
    /// it opened; the version the session is saved as then continues the earlier answers of these fields under their new
    /// names. A later report of the same session replaces the earlier one, and an older report arriving late is dropped;
    /// the sessions of one user are kept apart by their id, one opened on top of another's unsaved renames continues
    /// them. A version the form does not have yet is ignored.
    /// </summary>
    public async Task SetEditorFieldRenamesAsync(int originalFormId, int openedVersion, long sequence, Guid userId, string sessionId, List<FormFieldRename> renames)
    {
        await UpdateVersionStateAsync(originalFormId, null, (form, state) =>
        {
            if (openedVersion > form.Version)
            {
                return Task.FromResult(false);
            }

            if (IsEditorSessionLimitReached(state, openedVersion, userId, sessionId))
            {
                logger.WarnEditorSessionLimitReached(originalFormId, openedVersion, userId);
                return Task.FromResult(false);
            }

            return Task.FromResult(KeepEditorRenames(state, openedVersion, sequence, userId, sessionId, renames));
        });
    }

    // A new session of a user who already has the most unsaved sessions on the version; an empty id is no id.
    internal static bool IsEditorSessionLimitReached(FormFillingProperties<int> state, int openedVersion, Guid userId, string sessionId)
    {
        sessionId = string.IsNullOrEmpty(sessionId) ? null : sessionId;
        var sessions = (state.EditorFieldRenames ?? []).Where(r => r.OpenedVersion == openedVersion && r.UserId == userId && r.SavedVersion == null).ToList();

        return sessions.Count >= MaxEditorSessionsPerUser && !sessions.Exists(r => r.SessionId == sessionId);
    }

    // Each session keeps its place in the list, which is the order the sessions began to report in: the renames of a
    // session opened on top of another's are joined after them. A report without a session id, or with an empty one,
    // stands for all the sessions of the user on the version, as the editors before the session id did.
    internal static bool KeepEditorRenames(FormFillingProperties<int> state, int openedVersion, long sequence, Guid userId, string sessionId, List<FormFieldRename> renames)
    {
        if (IsEditorSessionLimitReached(state, openedVersion, userId, sessionId))
        {
            return false;
        }

        sessionId = string.IsNullOrEmpty(sessionId) ? null : sessionId;
        var sessions = state.EditorFieldRenames ??= [];
        var session = sessions.Find(r => r.OpenedVersion == openedVersion && r.UserId == userId && r.SavedVersion == null && r.SessionId == sessionId);
        if (session == null)
        {
            session = new FormEditorFieldRenames { OpenedVersion = openedVersion, UserId = userId, SessionId = sessionId };
            sessions.Add(session);
        }
        else if (sequence <= session.Sequence)
        {
            return false;
        }

        session.Sequence = sequence;
        session.Renames = renames
            .Where(r => !string.IsNullOrEmpty(r.OldKey) && !string.IsNullOrEmpty(r.NewKey) && r.OldKey != r.NewKey)
            .Select(r => new FormFieldRename { OldKey = r.OldKey, NewKey = r.NewKey })
            .ToList();

        return true;
    }

    /// <summary>
    /// Ties the field renames of the editor sessions open up to a save of the original form to the version it saved. Best
    /// effort: it never fails the save, and a form with no such session is left alone.
    /// </summary>
    public async Task MarkEditorRenamesSavedAsync(File<int> form, FormFillingProperties<int> formFilling)
    {
        if (formFilling?.EditorFieldRenames?.Exists(r => r.SavedVersion == null) != true)
        {
            return;
        }

        try
        {
            await UpdateVersionStateAsync(form.Id, form.Version, (saved, state) => Task.FromResult(MarkEditorRenamesSaved(state, saved.Version)));
        }
        catch (Exception e)
        {
            logger.WarnEditorRenamesNotSaved(e, form.Id, form.Version);
        }
    }

    // The pending decision of the version, taken with its first submission. Returns whether the state changed.
    private async Task<bool> ApplyOnFirstSubmissionAsync(ExternalDatabaseClient db, File<int> form, FormFillingProperties<int> state)
    {
        var link = state.PendingVersionLinks?.Find(p => p.Version == form.Version);
        if (link == null)
        {
            return false;
        }

        if (db != null && await db.TableExistsAsync(GetTableName(form.Id, form.Version)))
        {
            // Decided after its first submissions, before the background move ran.
            await MergeVersionAsync(db, form.Id, state, link);
        }
        else
        {
            state.PendingVersionLinks.Remove(link);
            await LinkVersionAsync(db, form, state, link);
        }

        return true;
    }

    // Under the table lock and saved inside it, so a submission exported meanwhile maps to the columns it finds. Without
    // a database the link only groups the versions in the spreadsheet report.
    private async Task LinkVersionAsync(ExternalDatabaseClient db, File<int> form, FormFillingProperties<int> state, FormVersionLink link)
    {
        var tableVersion = GetTableVersion(state, link.TableVersion);
        if (db == null)
        {
            link.TableVersion = tableVersion;
            AddLink(state, link);
            return;
        }

        // Without the description of the version its added columns are unknown, so it keeps a table of its own.
        var current = NormalizeMetadata(await GetVersionMetadataAsync(form.Id, form.Version)).ToList();
        if (current.Count == 0)
        {
            return;
        }

        var tableName = GetTableName(form.Id, tableVersion);

        try
        {
            await using (await AcquireTableLockAsync(tableName))
            {
                // Its submissions would stay apart from the shared table.
                if (await db.TableExistsAsync(GetTableName(form.Id, form.Version)))
                {
                    return;
                }

                var change = await db.AlterTableAsync(tableName, GetColumnRenames([link]), BuildColumnDefinitions(current));
                if (change == FormTableChange.Conflict)
                {
                    logger.WarnTableColumnConflict(tableName, form.Version);
                    return;
                }

                link.TableVersion = tableVersion;
                AddLink(state, link);

                await SaveVersionStateAsync(form.Id, state);
            }
        }
        catch (Exception e)
        {
            logger.WarnVersionLinkFailed(e, tableName, form.Version);
            state.VersionLinks?.RemoveAll(l => l.Version == link.Version);
        }
    }

    // Under the version lock. The table of the version and of the versions continuing it are moved into the shared table:
    // rows are copied first and the links saved next. A built-in table is dropped last, only with all its rows in the
    // target; an external one is left to its owner.
    private async Task MergeVersionAsync(ExternalDatabaseClient db, int originalFormId, FormFillingProperties<int> state, FormVersionLink link)
    {
        var version = link.Version;
        var tableVersion = GetTableVersion(state, link.TableVersion);
        var targetTable = GetTableName(originalFormId, tableVersion);
        var sourceTable = GetTableName(originalFormId, version);

        // The renames of a later version linked to the target are relative to keys this version never had.
        if (state.VersionLinks?.Any(l => l.TableVersion == tableVersion && l.Version > version) == true)
        {
            logger.WarnTableColumnConflict(targetTable, version);
            state.PendingVersionLinks.Remove(link);
            await SaveVersionStateAsync(originalFormId, state);
            return;
        }

        var continuing = (state.VersionLinks ?? []).Where(l => l.TableVersion == version).OrderBy(l => l.Version).ToList();

        if (db == null)
        {
            state.PendingVersionLinks.Remove(link);
            RetargetLinks(state, link, continuing, tableVersion);
            return;
        }

        try
        {
            await using (await AcquireTableLockAsync(targetTable))
            await using (await AcquireTableLockAsync(sourceTable))
            {
                // A table dropped as idle is restored from the index first; without the index the move waits for a later try.
                if (!await EnsureTableLockedAsync(db, targetTable, originalFormId, tableVersion, state.RoomId, state)
                    || !await EnsureTableLockedAsync(db, sourceTable, originalFormId, version, state.RoomId, state))
                {
                    logger.WarnTableMergeDeferred(sourceTable, targetTable);
                    return;
                }

                var change = await db.MergeTableAsync(targetTable, sourceTable, GetColumnRenames(continuing.Prepend(link)));
                if (change == FormTableChange.Missing)
                {
                    logger.WarnTableMergeDeferred(sourceTable, targetTable);
                    return;
                }

                state.PendingVersionLinks.Remove(link);

                if (change == FormTableChange.Conflict)
                {
                    logger.WarnTableColumnConflict(targetTable, version);
                    await SaveVersionStateAsync(originalFormId, state);
                    return;
                }

                RetargetLinks(state, link, continuing, tableVersion);
                await SaveVersionStateAsync(originalFormId, state);

                if (db is BuiltinFormsDatabaseClient builtin)
                {
                    if (!await builtin.DropMergedTableAsync(sourceTable, targetTable))
                    {
                        logger.WarnBuiltinDbMergedTableKept(sourceTable, targetTable);
                    }
                }
                else
                {
                    await SetExternalDbTableNameAsync(originalFormId, targetTable);
                }
            }
        }
        catch (Exception e) when (ExternalDatabaseClient.IsDataViolation(e))
        {
            // A value of the version is not among the options of the shared column: the version keeps its table.
            logger.WarnTableMergeFailed(e, sourceTable, targetTable);
            state.PendingVersionLinks.Remove(link);
            await SaveVersionStateAsync(originalFormId, state);
        }
        catch (Exception e)
        {
            logger.WarnTableMergeFailed(e, sourceTable, targetTable);
        }
    }

    internal static void RetargetLinks(FormFillingProperties<int> state, FormVersionLink link, List<FormVersionLink> continuing, int tableVersion)
    {
        link.TableVersion = tableVersion;
        foreach (var continued in continuing)
        {
            continued.TableVersion = tableVersion;
        }

        AddLink(state, link);
    }

    internal static void AddLink(FormFillingProperties<int> state, FormVersionLink link)
    {
        var links = state.VersionLinks ??= [];
        links.RemoveAll(l => l.Version == link.Version);
        links.Add(link);
    }

    // The renames of the links in order, as column names.
    internal static List<(string OldName, string NewName)> GetColumnRenames(IEnumerable<FormVersionLink> links)
    {
        return links
            .SelectMany(l => l.Renames ?? [])
            .Select(r => (NormalizeColumnName(r.OldKey), NormalizeColumnName(r.NewKey)))
            .ToList();
    }

    private async Task SetExternalDbTableNameAsync(int originalFormId, string tableName)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var properties = await fileDao.GetProperties(originalFormId);
        if (properties?.FormFilling == null || properties.FormFilling.ExternalDbTableName == tableName)
        {
            return;
        }

        properties.FormFilling.ExternalDbTableName = tableName;
        await SaveFormPropertiesAsync(originalFormId, properties);

        var originalForm = await fileDao.GetFileAsync(originalFormId);
        if (originalForm != null)
        {
            await socketManager.UpdateFileAsync(originalForm);
        }
    }

    /// <summary>
    /// Adds to the external table of the form version the submissions the search index holds and the table lacks, for
    /// every version sharing that table, under its latest column names. False when the index could not be read or a row
    /// failed.
    /// </summary>
    public async Task<bool> ExportMissingFromOpenSearchAsync(int originalFormId, int originalFormVersion, int roomId)
    {
        var formFilling = await GetFormFillingAsync(originalFormId);
        var tableVersion = GetTableVersion(formFilling, originalFormVersion);
        var tableName = GetTableName(originalFormId, tableVersion);

        bool success, exported;
        await using (await AcquireTableLockAsync(tableName))
        {
            (success, exported) = await FillTableGapsLockedAsync(externalDatabaseClient, tableName, originalFormId, originalFormVersion, tableVersion, roomId, formFilling);
        }

        // The gap sync can be the first to create the table (e.g. from the sync button), so record it here too.
        if (exported)
        {
            await SetExternalDbTableNameAsync(originalFormId, tableName);
        }

        return success;
    }

    // Under the table lock. Returns whether every missing row was written and whether any was.
    private async Task<(bool Success, bool Exported)> FillTableGapsLockedAsync(ExternalDatabaseClient db, string tableName, int originalFormId, int originalFormVersion, int tableVersion, int roomId, FormFillingProperties<int> formFilling)
    {
        var versions = GetTableVersions(formFilling, tableVersion);

        await factoryIndexerForm.RefreshAsync();
        var hasMissing = await HasMissingRowsAsync(db, tableName, originalFormId, versions, roomId);
        if (hasMissing != true)
        {
            return (hasMissing == false, false);
        }

        var existingIds = await db.GetExistingFormIdsAsync(tableName);

        var (success, allSubmissions) = await factoryIndexerForm.TrySelectAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .In(s => s.OriginalFormVersion, versions)
             .Limit(0, BaseIndexer<DbFormsItemDataSearch>.QueryLimit));

        if (!success)
        {
            return (false, false);
        }

        var missing = allSubmissions.Where(item => !existingIds.Contains(item.Id)).ToList();
        if (missing.Count == 0)
        {
            return (true, false);
        }

        var (columns, rows, incomplete) = await BuildTableRowsAsync(
            originalFormId, formFilling, missing.Select(item => item.OriginalFormVersion).Distinct(), missing, tableName);
        var hadFailure = incomplete;
        var exported = false;

        // An existing table gets the columns a version added; a conflicting one leaves the rows out.
        if (rows.Count > 0
            && await db.AlterTableAsync(tableName, [], columns.Values) == FormTableChange.Conflict)
        {
            logger.WarnTableColumnConflict(tableName, originalFormVersion);
            return (false, false);
        }

        if (rows.Count == 0)
        {
            return (!hadFailure, false);
        }

        try
        {
            await db.CreateTableAndUpsertAsync(tableName, columns.Values, rows.Select(r => r.Row).ToList(), keyColumn: "form_id");
            return (!hadFailure, true);
        }
        catch (Exception e)
        {
            // A batch fails as a whole; row by row the rows that fit still go in and the others are named.
            logger.WarnGapSyncBatchFailed(e, tableName);
        }

        foreach (var (id, row) in rows)
        {
            try
            {
                await db.CreateTableAndUpsertAsync(tableName, columns.Values, row, keyColumn: "form_id");
                exported = true;
            }
            catch (Exception ex)
            {
                logger.ErrorGapSyncUpsertFailed(ex, id, tableName);
                hadFailure = true;
            }
        }

        return (!hadFailure, exported);
    }

    private async Task<bool?> HasMissingRowsAsync(ExternalDatabaseClient db, string tableName, int originalFormId, int[] versions, int roomId)
    {
        return (await CountMissingRowsAsync(db, tableName, originalFormId, versions, roomId)).Missing;
    }

    // Missing is null when the index cannot be counted; the counts tell one attempt from the next.
    private async Task<(bool? Missing, string Counts)> CountMissingRowsAsync(ExternalDatabaseClient db, string tableName, int originalFormId, int[] versions, int roomId)
    {
        var dbCount = await db.GetTableCountAsync(tableName);
        var (success, osCount) = await factoryIndexerForm.TryCountAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .In(s => s.OriginalFormVersion, versions));

        return (success ? osCount > dbCount : null, $"{dbCount}:{osCount}");
    }

    private async Task<List<FormMetadata>> GetExportMetadataAsync(int originalFormId, int originalFormVersion, IReadOnlyCollection<DbFormsItemDataSearch> submissions)
    {
        await factoryIndexerFormMetadata.RefreshAsync();
        var (metaSuccess, metaResult) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, originalFormVersion));

        var rawMetadata = metaSuccess ? metaResult.FirstOrDefault()?.Metadata ?? [] : [];
        var normalizedMeta = NormalizeMetadata(rawMetadata).ToList();

        if (normalizedMeta.Count == 0)
        {
            var derivedMeta = submissions
                .Where(s => s.FormsData != null)
                .SelectMany(s => s.FormsData)
                .Where(f => !string.IsNullOrEmpty(f.Key) && !string.IsNullOrEmpty(f.Type) && IsExportableField(f))
                .GroupBy(f => f.Key)
                .Select(g => new FormMetadata { Key = g.Key, Type = g.First().Type });
            normalizedMeta = NormalizeMetadata(derivedMeta).ToList();
        }

        return normalizedMeta;
    }

    private Dictionary<string, object> BuildIndexedRowData(DbFormsItemDataSearch item, IEnumerable<FormMetadata> normalizedMeta, CultureInfo culture, string tableName)
    {
        if (item.FormsData == null)
        {
            logger.WarnGapSyncSkippedNoData(item.Id, tableName);
            return null;
        }

        var filteredData = new SubmitFormsData
        {
            FormsData = item.FormsData
                .Where(IsExportableField)
                .ToList()
        };

        return BuildRowData(filteredData, normalizedMeta, item.Id, culture, coreBaseSettings.EnabledCultures, item.CreateOn);
    }

    public async Task<IEnumerable<FormsItemData>> GetFormsFields(int folderId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder?.FolderType != FolderType.FormFillingFolderDone)
        {
            return [];
        }

        var fileDao = daoFactory.GetFileDao<int>();
        var file = await fileDao.GetFilesAsync([folderId], FilterType.Pdf, false, Guid.Empty, null, null, false).FirstOrDefaultAsync();
        if (file == null)
        {
            return [];
        }

        var (success, result) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.Id, file.Id));

        if (success)
        {
            return result.SelectMany(r => r.FormsData);
        }

        return [];
    }

    public async Task<IEnumerable<DbFormsItemDataSearch>> GetFormFillingResults(int roomId, int originalFormId, int originalFormVersion)
    {
        await factoryIndexerForm.RefreshAsync();
        var (success, result) = await factoryIndexerForm.TrySelectAsync(r => r
            .Where(s => s.RoomId, roomId)
            .Where(s => s.OriginalFormId, originalFormId)
            .Where(s => s.OriginalFormVersion, originalFormVersion));

        return success ? SortByFormNumber(result) : [];
    }

    /// <summary>
    /// All submissions of the form split into report sheets, newest first: the versions linked to one table form one sheet
    /// under their latest field keys, whichever database takes the submissions. Null when the search index cannot be read.
    /// </summary>
    public async Task<List<FormReportSheet>> GetReportSheetsAsync(int roomId, int originalFormId)
    {
        await factoryIndexerForm.RefreshAsync();
        var (success, result) = await factoryIndexerForm.TrySelectAsync(r => r
            .Where(s => s.RoomId, roomId)
            .Where(s => s.OriginalFormId, originalFormId)
            .Limit(0, BaseIndexer<DbFormsItemDataSearch>.QueryLimit));
        if (!success)
        {
            return null;
        }

        if (result.Count == 0)
        {
            return [];
        }

        if (result.Count >= BaseIndexer<DbFormsItemDataSearch>.QueryLimit)
        {
            logger.WarnFormReportTruncated(originalFormId, result.Count);
        }

        var formFilling = WithDecidedLinks(await GetFormFillingAsync(originalFormId));

        return SortByFormNumber(result)
            .GroupBy(s => GetTableVersion(formFilling, s.OriginalFormVersion))
            .Select(g => new FormReportSheet(
                g.Select(s => s.OriginalFormVersion).Distinct().Order().ToList(),
                g.ToList(),
                (version, key) => MapToLatestKey(formFilling, version, key)))
            .OrderByDescending(sheet => sheet.Versions[^1])
            .ToList();
    }

    // The links as decided: a decision waiting for the first submission of its version or for the database already groups
    // the report, which reads the index alone.
    internal static FormFillingProperties<int> WithDecidedLinks(FormFillingProperties<int> formFilling)
    {
        var decided = formFilling?.PendingVersionLinks?.OrderBy(p => p.Version).ToList();
        if (decided is not { Count: > 0 })
        {
            return formFilling;
        }

        var links = new FormFillingProperties<int>
        {
            VersionLinks = (formFilling.VersionLinks ?? [])
                .Select(l => new FormVersionLink { Version = l.Version, TableVersion = l.TableVersion, Renames = l.Renames })
                .ToList()
        };

        foreach (var link in decided)
        {
            var tableVersion = GetTableVersion(links, link.TableVersion);
            foreach (var continued in links.VersionLinks.Where(l => l.TableVersion == link.Version))
            {
                continued.TableVersion = tableVersion;
            }

            links.VersionLinks.RemoveAll(l => l.Version == link.Version);
            links.VersionLinks.Add(new FormVersionLink { Version = link.Version, TableVersion = tableVersion, Renames = link.Renames });
        }

        return links;
    }

    // A field key of the version under its name in the latest version linked to the same table.
    internal static string MapToLatestKey(FormFillingProperties<int> formFilling, int version, string key)
    {
        foreach (var link in GetLaterLinks(formFilling, version))
        {
            key = link.Renames?.FirstOrDefault(r => r.OldKey == key)?.NewKey ?? key;
        }

        return key;
    }

    private static List<DbFormsItemDataSearch> SortByFormNumber(IReadOnlyCollection<DbFormsItemDataSearch> result)
    {
        return result
            .Select(item =>
            {
                var formValue = item.FormsData?.FirstOrDefault(f => f.Key == "FormNumber")?.Value;
                int.TryParse(formValue, out var number);
                return (item, number);
            })
            .OrderBy(x => x.number)
            .Select(x => x.item)
            .ToList();
    }

    public async Task MigrateFormVersionAsync(int roomId, int originalFormId, int targetVersion)
    {
        await factoryIndexerForm.RefreshAsync();

        var (oldSuccess, oldRecords) = await factoryIndexerForm.TrySelectAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Or(
                 s => s.NotExists(x => x.OriginalFormVersion),
                 s => s.Where(x => x.OriginalFormVersion, 0))
             .Limit(0, 1));

        if (!oldSuccess || oldRecords.Count == 0)
        {
            return;
        }

        var (newSuccess, newRecords) = await factoryIndexerForm.TrySelectAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, targetVersion)
             .Limit(0, 1));

        if (!newSuccess || newRecords.Count == 0)
        {
            return;
        }

        var oldKeys = (oldRecords.First().FormsData ?? [])
            .Where(f => !string.IsNullOrEmpty(f.Type))
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newKeys = (newRecords.First().FormsData ?? [])
            .Where(f => !string.IsNullOrEmpty(f.Type))
            .Select(f => f.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (newKeys.Count == 0 || !oldKeys.SetEquals(newKeys))
        {
            return;
        }

        var migrateData = new DbFormsItemDataSearch { OriginalFormVersion = targetVersion };

        await factoryIndexerForm.UpdateAsync(
            migrateData,
            r => r.Where(s => s.RoomId, roomId)
                   .Where(s => s.OriginalFormId, originalFormId)
                   .NotExists(s => s.OriginalFormVersion),
            immediately: true,
            s => (object)s.OriginalFormVersion);

        await factoryIndexerForm.UpdateAsync(
            migrateData,
            r => r.Where(s => s.RoomId, roomId)
                   .Where(s => s.OriginalFormId, originalFormId)
                   .Where(s => s.OriginalFormVersion, 0),
            immediately: true,
            s => (object)s.OriginalFormVersion);

        var metaMigrateData = new DbFormsMetadataSearch { OriginalFormVersion = targetVersion };

        await factoryIndexerFormMetadata.UpdateAsync(
            metaMigrateData,
            r => r.Where(s => s.RoomId, roomId)
                   .Where(s => s.OriginalFormId, originalFormId)
                   .NotExists(s => s.OriginalFormVersion),
            immediately: true,
            s => (object)s.OriginalFormVersion);

        await factoryIndexerFormMetadata.UpdateAsync(
            metaMigrateData,
            r => r.Where(s => s.RoomId, roomId)
                   .Where(s => s.OriginalFormId, originalFormId)
                   .Where(s => s.OriginalFormVersion, 0),
            immediately: true,
            s => (object)s.OriginalFormVersion);
    }

    public static string GetTableName(int originalFormId, int originalFormVersion)
        => $"form_{originalFormId}_v{originalFormVersion}";

    /// <summary>The table the submissions of the form version go to: the table of the version it continues, or its own.</summary>
    public static string GetVersionTableName(FormFillingProperties<int> formFilling, int originalFormId, int originalFormVersion)
        => GetTableName(originalFormId, GetTableVersion(formFilling, originalFormVersion));

    public async Task<IEnumerable<DbColumnDefinition>> GetColumnDefinitionsAsync(int originalFormId, int originalFormVersion)
    {
        return BuildColumnDefinitions(NormalizeMetadata(await GetVersionMetadataAsync(originalFormId, originalFormVersion)));
    }

    public async Task<(IEnumerable<FormMetadata> Metadata, IEnumerable<SubmitFormsData> Submissions)> GetFormSnapshotAsync(int roomId, int originalFormId, int originalFormVersion)
    {
        var metadata = await GetVersionMetadataAsync(originalFormId, originalFormVersion);
        var submissions = await GetFormFillingResults(roomId, originalFormId, originalFormVersion);

        return (metadata, submissions);
    }

    private async Task<(SubmitFormsData fromData, List<FormMetadata> fromMetaData)> GetSubmitFormsData<T>(
        File<T> formsDataFile,
        int originalFormId,
        int originalFormVersion,
        int roomId,
        int resultFormNumber,
        string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsStringAsync();

        return await IndexSubmissionAsync(formsDataFile, originalFormId, originalFormVersion, roomId, resultFormNumber, data, DateTime.UtcNow);
    }

    /// <summary>
    /// Indexes a completed form missing from the search index, with the forms data read from the file; false when the
    /// index refused it. Nothing is refreshed: the caller refreshes the indexes once for all the forms it recovers. The
    /// description of the version is written only when asked, for a version that has none: the one the editor wrote on
    /// submission is kept.
    /// </summary>
    public async Task<bool> IndexRecoveredSubmissionAsync(
        File<int> formsDataFile,
        int originalFormId,
        int originalFormVersion,
        int roomId,
        int resultFormNumber,
        string formsDataJson,
        DateTime filledOn,
        bool indexMetadata)
    {
        var submission = CreateSubmissionDocuments(formsDataFile, originalFormId, originalFormVersion, roomId, resultFormNumber, formsDataJson, filledOn);

        // The description goes first, so that a response is never indexed without the columns of its version.
        return (!indexMetadata || await factoryIndexerFormMetadata.Index(submission.Metadata, false))
            && await factoryIndexerForm.Index(submission.Item, false);
    }

    /// <summary>
    /// The keys of the fields indexed for the form version; null when the version has no indexed description.
    /// </summary>
    public async Task<HashSet<string>> TryGetIndexedFieldKeysAsync(int originalFormId, int originalFormVersion)
    {
        var keys = GetExportableKeys(await GetVersionMetadataAsync(originalFormId, originalFormVersion));

        return keys.Count > 0 ? keys : null;
    }

    // Empty when the version has no description or the index cannot be read.
    private async Task<IEnumerable<FormMetadata>> GetVersionMetadataAsync(int originalFormId, int originalFormVersion)
    {
        await factoryIndexerFormMetadata.RefreshAsync();
        var (success, result) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, originalFormVersion));

        return success ? result.FirstOrDefault()?.Metadata ?? [] : [];
    }

    /// <summary>
    /// The keys of the fields in forms data JSON, as the report lays out its columns.
    /// </summary>
    public HashSet<string> GetFieldKeys(string formsDataJson)
    {
        return GetExportableKeys(ParseSubmitAndMetadata(formsDataJson).MetaData);
    }

    private static HashSet<string> GetExportableKeys(IEnumerable<FormMetadata> metadata)
    {
        return metadata
            .Where(m => !string.IsNullOrEmpty(m.Key) && IsExportableType(m.Type))
            .Select(m => m.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<(SubmitFormsData fromData, List<FormMetadata> fromMetaData)> IndexSubmissionAsync<T>(
        File<T> formsDataFile,
        int originalFormId,
        int originalFormVersion,
        int roomId,
        int resultFormNumber,
        string data,
        DateTime createOn)
    {
        var submission = CreateSubmissionDocuments(formsDataFile, originalFormId, originalFormVersion, roomId, resultFormNumber, data, createOn);

        if (submission.Item != null)
        {
            await factoryIndexerForm.IndexAsync(submission.Item);
            await factoryIndexerFormMetadata.IndexAsync(submission.Metadata);
        }

        return (submission.Data, submission.MetaData);
    }

    // The documents are null for a file of a third-party storage.
    private (DbFormsItemDataSearch Item, DbFormsMetadataSearch Metadata, SubmitFormsData Data, List<FormMetadata> MetaData) CreateSubmissionDocuments<T>(
        File<T> formsDataFile,
        int originalFormId,
        int originalFormVersion,
        int roomId,
        int resultFormNumber,
        string data,
        DateTime createOn)
    {
        var formNumber = new List<FormsItemData>
        {
            new()
            {
                Key = "FormNumber",
                Value = resultFormNumber.ToString()
            }
        };

        var parsed = ParseSubmitAndMetadata(data);

        var fromData = parsed.Data;
        var fromMetaData = parsed.MetaData;

        fromData.FormsData = fromData.FormsData
            .Where(f => f.Type != "picture" && f.Type != "signature")
            .ToList();

        if (formsDataFile.Id is not int id || formsDataFile.ParentId is not int parentId)
        {
            return (null, null, fromData, fromMetaData);
        }

        var tenantId = tenantManager.GetCurrentTenantId();

        var item = new DbFormsItemDataSearch
        {
            Id = id,
            TenantId = tenantId,
            ParentId = parentId,
            OriginalFormId = originalFormId,
            OriginalFormVersion = originalFormVersion,
            RoomId = roomId,
            CreateOn = createOn,
            FormsData = formNumber.Concat(fromData.FormsData)
        };

        var metadata = new DbFormsMetadataSearch
        {
            Id = DbFormsMetadataSearch.ComputeId(originalFormId, originalFormVersion),
            TenantId = tenantId,
            OriginalFormId = originalFormId,
            OriginalFormVersion = originalFormVersion,
            RoomId = roomId,
            Metadata = fromMetaData
        };

        return (item, metadata, fromData, fromMetaData);
    }

    private (SubmitFormsData Data, List<FormMetadata> MetaData)ParseSubmitAndMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        if (!root.TryGetProperty("formsdata", out var formsArray) ||
            formsArray.ValueKind != JsonValueKind.Array)
        {
            return (new SubmitFormsData
            {
                FormsData = []
            }, []);
        }

        var formsDataList = new List<FormsItemData>(formsArray.GetArrayLength());
        var metaDataList = new List<FormMetadata>(formsArray.GetArrayLength());

        foreach (var form in formsArray.EnumerateArray())
        {
            var key = form.TryGetProperty("key", out var keyProp)
                ? keyProp.GetString()
                : null;

            var type = form.TryGetProperty("type", out var typeProp)
                ? typeProp.GetString()
                : null;

            var tag = form.TryGetProperty("tag", out var tagProp)
                ? tagProp.GetString()
                : null;

            var value = form.TryGetProperty("value", out var valueProp)
                ? valueProp.ToString()
                : null;

            formsDataList.Add(new FormsItemData
            {
                Key = key,
                Tag = tag,
                Value = value,
                Type = type
            });

            var metadata = new FormMetadata
            {
                Key = key ?? "",
                Type = type ?? "",
                Format = form.TryGetProperty("format", out var formatProp)
                    ? formatProp.GetString()
                    : null
            };

            if (form.TryGetProperty("options", out var optionsProp) &&
                optionsProp.ValueKind == JsonValueKind.Array)
            {
                var possibleValues = new List<string>();

                foreach (var option in optionsProp.EnumerateArray())
                {
                    if (option.ValueKind == JsonValueKind.Object &&
                        option.TryGetProperty("value", out var valProp))
                    {
                        possibleValues.Add(valProp.ToString());
                    }
                    else
                    {
                        possibleValues.Add(option.ToString());
                    }
                }

                metadata.PossibleValues = possibleValues;
            }

            metaDataList.Add(metadata);
        }

        return (
            new SubmitFormsData
            {
                FormsData = formsDataList
            },
            metaDataList
        );
    }

    private static IEnumerable<FormMetadata> NormalizeMetadata(IEnumerable<FormMetadata> metaData)
    {
        return metaData
            .Where(m => m.Type != "picture" && m.Type != "signature")
            .GroupBy(m => m.Type == "radio"
                ? $"radio::{NormalizeColumnName(m.Key)}"
                : NormalizeColumnName(m.Key))
            .Select(g => g.First());
    }

    private static IEnumerable<DbColumnDefinition> BuildColumnDefinitions(IEnumerable<FormMetadata> metaData)
    {
        yield return new DbColumnDefinition("form_id", DbColumnType.Integer, IsPrimaryKey: true);
        yield return new DbColumnDefinition("created_on", DbColumnType.DateTime);

        foreach (var field in metaData)
        {
            yield return BuildColumnDefinition(field);
        }
    }

    internal static DbColumnDefinition BuildColumnDefinition(FormMetadata field)
    {
        var (type, enumValues) = field.Type switch
        {
            "checkBox" => (DbColumnType.Boolean, (IReadOnlyList<string>)null),
            "dateTime" => (HasTimeComponent(field.Format) ? DbColumnType.DateTime : DbColumnType.Date, null),
            "comboBox" or "dropDownList" or "radio" => (DbColumnType.Enum, (IReadOnlyList<string>)field.PossibleValues),
            _ => (DbColumnType.Text, null)
        };
        var label = string.IsNullOrEmpty(field.Key) ? null : field.Key;
        return new DbColumnDefinition(NormalizeColumnName(field.Key), type, enumValues, Label: label);
    }

    internal static bool IsExportableField(FormsItemData f) => f.Type != "picture" && f.Type != "signature";

    private static bool IsExportableType(string type) => type != "picture" && type != "signature";

    private static bool HasTimeComponent(string format) =>
        !string.IsNullOrEmpty(format) && (format.Contains('H') || format.Contains('h'));

    private static Dictionary<string, object> BuildRowData(SubmitFormsData data, IEnumerable<FormMetadata> metaData, int formId, CultureInfo culture, IReadOnlyList<CultureInfo> enabledCultures, DateTime? createdOn = null)
    {
        var result = new Dictionary<string, object>
        {
            ["form_id"] = formId,
            ["created_on"] = createdOn ?? DateTime.UtcNow
        };

        var metaByKey = metaData.ToDictionary(m => NormalizeColumnName(m.Key));

        foreach (var item in data.FormsData)
        {
            var column = NormalizeColumnName(item.Key);
            if (!metaByKey.TryGetValue(column, out var meta))
            {
                continue;
            }
            result[column] = ConvertFieldValue(item.Value, meta, culture, enabledCultures) ?? DBNull.Value;
        }

        return result;
    }

    private static string NormalizeColumnName(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return "col_field";
        }

        // Transliterate any Unicode script to ASCII (Cyrillic, Chinese, Japanese, Korean, Arabic, etc.)
        var transliterated = AnyAscii.Transliteration.Transliterate(key);

        // Replace any character that is not alphanumeric or underscore with underscore
        var name = Regex.Replace(transliterated, @"[^a-zA-Z0-9_]", "_");

        // Collapse consecutive underscores and strip leading/trailing ones
        name = Regex.Replace(name, @"_+", "_").Trim('_');

        if (name.Length == 0)
        {
            return "col_field";
        }

        // col_ prefix guarantees the name never collides with any SQL reserved word
        name = "col_" + name;

        // Truncate to 64 characters (MySQL/PostgreSQL identifier limit)
        if (name.Length > 64)
        {
            name = name[..64].TrimEnd('_');
        }

        return name.ToLower();
    }

    private static object ConvertFieldValue(string value, FormMetadata meta, CultureInfo culture, IReadOnlyList<CultureInfo> enabledCultures)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return meta.Type switch
        {
            "checkBox" => ParseCheckbox(value),
            "dateTime" => ParseDate(value, meta.Format, culture, enabledCultures),
            "comboBox" or "dropDownList" or "radio" => NormalizeEnumValue(value, meta.PossibleValues),
            _ => value
        };
    }

    private static bool ParseCheckbox(string value) =>
        bool.TryParse(value, out var b) ? b : !string.IsNullOrWhiteSpace(value) && !_checkboxOffStates.Contains(value.Trim());

    private static object NormalizeEnumValue(string value, List<string> options)
    {
        if (options is not { Count: > 0 })
        {
            return value;
        }
        return options.FirstOrDefault(o => string.Equals(o, value, StringComparison.OrdinalIgnoreCase)) ?? value;
    }

    private static DateTime? ParseDate(string value, string format, CultureInfo culture, IReadOnlyList<CultureInfo> enabledCultures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            var dotNetFormat = format
                .Replace("DD", "dd")
                .Replace("YYYY", "yyyy")
                .Replace("YY", "yy");

            if (DateTime.TryParseExact(value, dotNetFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt)
                || DateTime.TryParseExact(value, dotNetFormat, culture, DateTimeStyles.AllowWhiteSpaces, out dt))
            {
                return dt;
            }
        }

        // Numeric dates stop at portal culture and invariant to avoid a day/month swap.
        if (DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsed))
        {
            return parsed;
        }

        // A localized month name is in the filler's language, not the portal's: retry against the
        // portal's enabled languages. Month spellings differ, so no cross-language mix-up.
        if (value.Any(char.IsLetter))
        {
            foreach (var enabled in enabledCultures)
            {
                if (DateTime.TryParse(value, enabled, DateTimeStyles.AllowWhiteSpaces, out var localized))
                {
                    return localized;
                }
            }
        }

        return null;
    }

    public class BoolToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.String => reader.GetString(),
                _ => throw new JsonException("Unexpected token type")
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

}

/// <summary>
/// The submissions of the form versions shown on one sheet of the report, and the key each field of a version has
/// on that sheet.
/// </summary>
public record FormReportSheet(IReadOnlyList<int> Versions, IReadOnlyList<DbFormsItemDataSearch> Submissions, Func<int, string, string> MapKey);
