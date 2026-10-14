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
    CommonLinkUtility commonLinkUtility)
{
    private static readonly TimeSpan _fieldsReadTimeout = TimeSpan.FromSeconds(10);

    private static readonly HashSet<string> _checkboxOffStates =
        new(StringComparer.OrdinalIgnoreCase) { "off", "false", "0", "no", "n", "unchecked" };

    public async Task UpdateFormFillingReport<T>(int originalFormId, int originalFormVersion, int roomId, int resultFormNumber, string formsDataUrl, File<T> formsDataFile, bool sendFormToExternalDB, bool settingsSaveFormAsXLSX)
    {
        await GetSubmitFormsData(formsDataFile, originalFormId, originalFormVersion, roomId, resultFormNumber, formsDataUrl);

        await MigrateFormVersionAsync(roomId, originalFormId, originalFormVersion);

        var fileId = formsDataFile.Id is int id ? id : 0;
        await PublishExternalDbSubmissionAsync(sendFormToExternalDB, originalFormId, originalFormVersion, roomId, fileId, resultFormNumber, formsDataUrl);
        await PublishBuiltinDbSubmissionAsync(originalFormId, originalFormVersion, roomId, fileId, resultFormNumber, formsDataUrl);

        if (settingsSaveFormAsXLSX)
        {
            await exportToXLSX.UpdateXlsxReport(roomId, originalFormId, originalFormVersion, isNewFile: false);
        }
    }

    public Task SyncExternalDbAsync(Folder<int> room, int originalFormId, int originalFormVersion)
    {
        return PublishExternalDbSubmissionAsync(room.SettingsSendFormToExternalDB, originalFormId, originalFormVersion,
            room.Id, fileId: 0, resultFormNumber: 0, formsDataUrl: null);
    }

    private async Task PublishExternalDbSubmissionAsync(bool sendFormToExternalDB, int originalFormId, int originalFormVersion, int roomId, int fileId, int resultFormNumber, string formsDataUrl)
    {
        if (!sendFormToExternalDB || !externalDatabaseClient.IsEnabled())
        {
            return;
        }

        await eventBus.PublishAsync(new ExternalDbFormSubmissionIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, originalFormVersion,
            roomId, fileId, resultFormNumber, formsDataUrl));
    }

    // Unlike the external database, the built-in one ignores the room's send-to-database setting.
    private async Task PublishBuiltinDbSubmissionAsync(int originalFormId, int originalFormVersion, int roomId, int fileId, int resultFormNumber, string formsDataUrl)
    {
        if (externalDatabaseClient.IsEnabled() || !builtinFormsDatabaseClient.IsEnabled())
        {
            return;
        }

        await eventBus.PublishAsync(new BuiltinDbFormSubmissionIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, originalFormVersion,
            roomId, fileId, resultFormNumber, formsDataUrl));

        // The first submission makes the form's responses analysable.
        if (resultFormNumber == 1)
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
        var (row, columns) = await ReadSubmissionAsync(fileId, formsDataUrl);
        var tableName = GetTableName(originalFormId, originalFormVersion);

        await externalDatabaseClient.CreateTableAndUpsertAsync(tableName, columns, row, keyColumn: "form_id");
        await SetExternalDbTableNameAsync(originalFormId, tableName);
    }

    public async Task ExportToBuiltinDbAsync(int fileId, int originalFormId, int originalFormVersion, int roomId, string formsDataUrl)
    {
        var formFilling = await DecideBuiltinDbVersionAsync(originalFormId, originalFormVersion);

        // A confirmed rename can move the version to another table meanwhile, so the table is resolved again inside its lock.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var tableVersion = GetBuiltinTableVersion(formFilling, originalFormVersion);
            var tableName = GetTableName(originalFormId, tableVersion);

            await using (await AcquireBuiltinDbTableLockAsync(tableName))
            {
                formFilling = await GetFormFillingAsync(originalFormId);
                if (GetBuiltinTableVersion(formFilling, originalFormVersion) != tableVersion)
                {
                    continue;
                }

                // A new table is filled from the search index, which already holds this submission; without the index it
                // is left uncreated rather than created incomplete.
                if (!await EnsureBuiltinDbTableLockedAsync(tableName, originalFormId, tableVersion, roomId, formFilling))
                {
                    return;
                }

                var (row, columns) = await ReadSubmissionAsync(fileId, formsDataUrl);

                await builtinFormsDatabaseClient.CreateTableAndUpsertAsync(
                    tableName,
                    columns.Select(c => c with { Name = MapToTableColumn(formFilling, originalFormVersion, c.Name) }).ToList(),
                    MapToTableColumns(formFilling, originalFormVersion, row),
                    keyColumn: "form_id");

                return;
            }
        }

        logger.WarnBuiltinDbSubmissionSkipped(fileId, originalFormId, originalFormVersion);
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
            var tableVersion = GetBuiltinTableVersion(formFilling, originalFormVersion);
            var tableName = GetTableName(originalFormId, tableVersion);

            await TouchBuiltinDbTableAsync(tableName);
            if (await builtinFormsDatabaseClient.TableExistsAsync(tableName))
            {
                return tableName;
            }

            await using (await AcquireBuiltinDbTableLockAsync(tableName))
            {
                formFilling = await GetFormFillingAsync(originalFormId);
                if (GetBuiltinTableVersion(formFilling, originalFormVersion) != tableVersion)
                {
                    continue;
                }

                return await EnsureBuiltinDbTableLockedAsync(tableName, originalFormId, tableVersion, roomId, formFilling) ? tableName : null;
            }
        }

        return null;
    }

    // Under the table lock. Marked as used before the existence check, so the idle-table cleanup cannot drop it in between.
    private async Task<bool> EnsureBuiltinDbTableLockedAsync(string tableName, int originalFormId, int tableVersion, int roomId, FormFillingProperties<int> formFilling)
    {
        await TouchBuiltinDbTableAsync(tableName);

        return await builtinFormsDatabaseClient.TableExistsAsync(tableName)
            || await RestoreBuiltinDbTableAsync(tableName, originalFormId, tableVersion, roomId, formFilling);
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

    // Created and filled in one transaction; holds every version linked to the table, under the latest column names.
    private async Task<bool> RestoreBuiltinDbTableAsync(string tableName, int originalFormId, int tableVersion, int roomId, FormFillingProperties<int> formFilling)
    {
        var versions = GetBuiltinTableVersions(formFilling, tableVersion);

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
            logger.WarnBuiltinDbRestoreTooLarge(tableName, count);
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

        var culture = tenantManager.GetCurrentTenant().GetCulture();
        var columns = new Dictionary<string, DbColumnDefinition>();
        var rows = new List<Dictionary<string, object>>();

        // Each version is converted by its own metadata: a date keeps the format it was submitted in.
        foreach (var version in versions.OrderBy(v => v))
        {
            var versionSubmissions = submissions.Where(s => s.OriginalFormVersion == version).ToList();
            var normalizedMeta = await GetExportMetadataAsync(originalFormId, version, versionSubmissions);

            foreach (var column in BuildColumnDefinitions(normalizedMeta))
            {
                var tableColumn = column with { Name = MapToTableColumn(formFilling, version, column.Name) };
                columns[tableColumn.Name] = tableColumn;
            }

            rows.AddRange(versionSubmissions
                .Select(item => BuildIndexedRowData(item, normalizedMeta, culture, tableName))
                .OfType<Dictionary<string, object>>()
                .Select(row => MapToTableColumns(formFilling, version, row)));
        }

        if (rows.Count == 0)
        {
            return false;
        }

        await builtinFormsDatabaseClient.CreateTableAndUpsertAsync(tableName, columns.Values.ToList(), rows, keyColumn: "form_id");

        return true;
    }

    private Task<IDistributedLockHandle> AcquireBuiltinDbTableLockAsync(string tableName)
    {
        return distributedLockProvider.TryAcquireFairLockAsync($"forms_db_table_{tenantManager.GetCurrentTenantId()}_{tableName}");
    }

    // The first submission of a version decides at the latest: it can come before the background check or without a start.
    private async Task<FormFillingProperties<int>> DecideBuiltinDbVersionAsync(int originalFormId, int originalFormVersion)
    {
        var formFilling = await GetFormFillingAsync(originalFormId);
        if (formFilling == null
            || !HasDecisionToApply(formFilling, originalFormVersion)
               && (originalFormVersion < formFilling.BuiltinDbCheckedVersion
                   || !NeedsCheck(formFilling, await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId, originalFormVersion))))
        {
            return formFilling;
        }

        return await UpdateBuiltinDbStateAsync(originalFormId, originalFormVersion, async (form, state) =>
        {
            var changed = NeedsCheck(state, form) && await CheckVersionAsync(form, state);
            return await ApplyOnFirstSubmissionAsync(form, state) || changed;
        });
    }

    /// <summary>
    /// Asks the background check of the current version of a started original form when it was not compared yet; a form
    /// being edited is not started, so its saves read no fields.
    /// </summary>
    public async Task RequestBuiltinDbVersionCheckAsync(File<int> form, FormFillingProperties<int> formFilling)
    {
        if (formFilling is not { StartFilling: true } || formFilling.OriginalFormId != form.Id || !NeedsCheck(formFilling, form)
            || externalDatabaseClient.IsEnabled() || !builtinFormsDatabaseClient.IsEnabled())
        {
            return;
        }

        await PublishBuiltinDbVersionCheckAsync(form.Id);
    }

    private Task PublishBuiltinDbVersionCheckAsync(int originalFormId)
    {
        return eventBus.PublishAsync(new BuiltinDbFormVersionIntegrationEvent(
            authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId(), originalFormId, commonLinkUtility.ServerRootPath));
    }

    /// <summary>
    /// Compares the fields of the current version of an original form in a form-filling room with the last version that
    /// has data and keeps the question about renamed fields for the room; moves the confirmed versions that have
    /// submissions into the shared table.
    /// </summary>
    public async Task CheckBuiltinDbVersionAsync(int originalFormId)
    {
        if (externalDatabaseClient.IsEnabled() || !builtinFormsDatabaseClient.IsEnabled())
        {
            return;
        }

        var form = await daoFactory.GetFileDao<int>().GetFileAsync(originalFormId);
        if (form is not { IsPdf: true }
            || await DocSpaceHelper.GetParentRoom(form, daoFactory.GetFolderDao<int>()) is not { FolderType: FolderType.FillingFormsRoom })
        {
            return;
        }

        await UpdateBuiltinDbStateAsync(originalFormId, null, async (current, state) =>
        {
            if (state.OriginalFormId != current.Id)
            {
                return false;
            }

            var changed = NeedsCheck(state, current) && await CheckVersionAsync(current, state);
            foreach (var link in state.BuiltinDbPendingLinks?.Where(p => p.Confirmed == true).ToList() ?? [])
            {
                if (await HasDataAsync(originalFormId, link.Version, state.RoomId))
                {
                    await MergeBuiltinDbVersionAsync(originalFormId, state, link);
                    changed = true;
                }
            }

            return changed;
        });
    }

    /// <summary>
    /// Records the answer about the renamed fields of a form version. A confirmed version is moved into the shared table
    /// with its first submission, or in the background when it has submissions already. Only an open question takes an
    /// answer.
    /// </summary>
    public async Task AnswerBuiltinDbFieldRenamesAsync(int originalFormId, int version, bool confirm)
    {
        await UpdateBuiltinDbStateAsync(originalFormId, null, async (_, state) =>
        {
            var link = state.BuiltinDbPendingLinks?.Find(p => p.Version == version && IsOpenQuestion(p));
            if (link == null)
            {
                return false;
            }

            if (!confirm)
            {
                state.BuiltinDbPendingLinks.Remove(link);
                return true;
            }

            link.Confirmed = true;
            if (await HasDataAsync(originalFormId, version, state.RoomId))
            {
                await PublishBuiltinDbVersionCheckAsync(originalFormId);
            }

            return true;
        });
    }

    // Every change of the state is made from properties read under the version lock; the clients learn of changed questions.
    private async Task<FormFillingProperties<int>> UpdateBuiltinDbStateAsync(int originalFormId, int? version, Func<File<int>, FormFillingProperties<int>, Task<bool>> change)
    {
        await using (await AcquireBuiltinDbVersionLockAsync(originalFormId))
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var state = await GetFormFillingAsync(originalFormId);
            var form = version.HasValue ? await fileDao.GetFileAsync(originalFormId, version.Value) : await fileDao.GetFileAsync(originalFormId);
            if (form == null || state == null)
            {
                return state;
            }

            var questions = GetOpenQuestionVersions(state);
            if (!await change(form, state))
            {
                return state;
            }

            await SaveBuiltinDbStateAsync(originalFormId, state);

            if (!questions.SequenceEqual(GetOpenQuestionVersions(state)))
            {
                await socketManager.UpdateFileAsync(form);
            }

            return state;
        }
    }

    public static bool HasOpenQuestions(FormFillingProperties<int> formFilling)
    {
        return formFilling?.BuiltinDbPendingLinks?.Any(IsOpenQuestion) == true;
    }

    /// <summary>
    /// The versions whose renamed fields wait for an answer, oldest first.
    /// </summary>
    public static List<FormVersionLink> GetOpenQuestions(FormFillingProperties<int> formFilling)
    {
        return (formFilling?.BuiltinDbPendingLinks ?? [])
            .Where(IsOpenQuestion)
            .OrderBy(p => p.Version)
            .ToList();
    }

    private static bool IsOpenQuestion(FormVersionLink link) => link.Renames?.Count > 0 && link.Confirmed == null;

    private static List<int> GetOpenQuestionVersions(FormFillingProperties<int> formFilling)
    {
        return GetOpenQuestions(formFilling).Select(p => p.Version).ToList();
    }

    // A version compared already is compared again only when it was overwritten (a save that replaces the version).
    private static bool NeedsCheck(FormFillingProperties<int> formFilling, File<int> form)
    {
        return formFilling != null && form != null
            && (form.Version > formFilling.BuiltinDbCheckedVersion
                || form.Version == formFilling.BuiltinDbCheckedVersion && form.ModifiedOn.Ticks != formFilling.BuiltinDbCheckedStamp);
    }

    private static bool HasDecisionToApply(FormFillingProperties<int> formFilling, int version)
    {
        return formFilling.BuiltinDbPendingLinks?.Any(p => p.Version == version && !IsOpenQuestion(p)) == true;
    }

    private Task<IDistributedLockHandle> AcquireBuiltinDbVersionLockAsync(int originalFormId)
    {
        return distributedLockProvider.TryAcquireFairLockAsync($"forms_db_version_{tenantManager.GetCurrentTenantId()}_{originalFormId}");
    }

    // Under the version lock, so the state replaces the saved one; the lock of the submissions keeps the other properties.
    private async Task SaveBuiltinDbStateAsync(int originalFormId, FormFillingProperties<int> state)
    {
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"fillform_{state.RoomId}_{originalFormId}"))
        {
            var fileDao = daoFactory.GetFileDao<int>();
            var current = await fileDao.GetProperties(originalFormId);
            if (current?.FormFilling == null)
            {
                return;
            }

            CopyBuiltinDbState(state, current.FormFilling);
            await fileDao.SaveProperties(originalFormId, current);
        }
    }

    /// <summary>
    /// Saves form properties read earlier without losing the built-in database state saved meanwhile in the background.
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
                CopyBuiltinDbState(current, properties.FormFilling);
            }

            await fileDao.SaveProperties(formId, properties);
        }
    }

    private static void CopyBuiltinDbState<T>(FormFillingProperties<T> from, FormFillingProperties<T> to)
    {
        to.BuiltinDbCheckedVersion = from.BuiltinDbCheckedVersion;
        to.BuiltinDbCheckedStamp = from.BuiltinDbCheckedStamp;
        to.BuiltinDbVersionLinks = from.BuiltinDbVersionLinks;
        to.BuiltinDbPendingLinks = from.BuiltinDbPendingLinks;
    }

    private async Task<FormFillingProperties<int>> GetFormFillingAsync(int originalFormId)
    {
        return (await daoFactory.GetFileDao<int>().GetProperties(originalFormId))?.FormFilling;
    }

    // The version whose table the form version writes to: the version it continues, or its own.
    private static int GetBuiltinTableVersion(FormFillingProperties<int> formFilling, int version)
    {
        return formFilling?.BuiltinDbVersionLinks?.FirstOrDefault(l => l.Version == version)?.TableVersion ?? version;
    }

    private static int[] GetBuiltinTableVersions(FormFillingProperties<int> formFilling, int tableVersion)
    {
        return (formFilling?.BuiltinDbVersionLinks ?? [])
            .Where(l => l.TableVersion == tableVersion)
            .Select(l => l.Version)
            .Prepend(tableVersion)
            .ToArray();
    }

    // A column of the version under its name in the table: the renames of the later versions linked to the same table
    // are applied in order.
    private static string MapToTableColumn(FormFillingProperties<int> formFilling, int version, string column)
    {
        var tableVersion = GetBuiltinTableVersion(formFilling, version);
        var laterLinks = (formFilling?.BuiltinDbVersionLinks ?? [])
            .Where(l => l.TableVersion == tableVersion && l.Version > version)
            .OrderBy(l => l.Version);

        foreach (var link in laterLinks)
        {
            var rename = link.Renames?.FirstOrDefault(r => NormalizeColumnName(r.OldKey) == column);
            if (rename != null)
            {
                column = NormalizeColumnName(rename.NewKey);
            }
        }

        return column;
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
    private async Task<bool> CheckVersionAsync(File<int> form, FormFillingProperties<int> state)
    {
        try
        {
            // A version linked or with a table of its own keeps its decision and its open question.
            if (state.BuiltinDbVersionLinks?.Any(l => l.Version == form.Version) == true
                || await builtinFormsDatabaseClient.TableExistsAsync(GetTableName(form.Id, form.Version)))
            {
                MarkChecked(state, form);
                return true;
            }

            var link = await CompareWithPreviousVersionAsync(form, state);
            if (link == null)
            {
                return false;
            }

            var pending = state.BuiltinDbPendingLinks ??= [];

            // An earlier version left without submissions has nothing to decide: this one was compared past it.
            foreach (var stale in pending.Where(p => p.Version < form.Version).ToList())
            {
                if (!await HasDataAsync(form.Id, stale.Version, state.RoomId))
                {
                    pending.Remove(stale);
                }
            }

            var previous = pending.Find(p => p.Version == form.Version);
            pending.RemoveAll(p => p.Version == form.Version);

            if (link.Version > 0)
            {
                // An answer survives an overwrite of the version that renamed the same fields.
                if (previous?.Confirmed != null && SameRenames(previous, link))
                {
                    link.Confirmed = previous.Confirmed;
                }

                pending.Add(link);
            }

            MarkChecked(state, form);
            return true;
        }
        catch (Exception e)
        {
            logger.WarnBuiltinDbVersionCheckFailed(e, form.Id, form.Version);
            return false;
        }
    }

    private static void MarkChecked(FormFillingProperties<int> state, File<int> form)
    {
        state.BuiltinDbCheckedVersion = form.Version;
        state.BuiltinDbCheckedStamp = form.ModifiedOn.Ticks;
    }

    private static bool SameRenames(FormVersionLink a, FormVersionLink b)
    {
        return a.TableVersion == b.TableVersion
            && a.Renames.Select(r => (r.OldKey, r.NewKey)).SequenceEqual(b.Renames.Select(r => (r.OldKey, r.NewKey)));
    }

    // Data of the version: its own table, or submissions in the index. An unreadable index counts as data.
    private async Task<bool> HasDataAsync(int originalFormId, int version, int roomId)
    {
        if (await builtinFormsDatabaseClient.TableExistsAsync(GetTableName(originalFormId, version)))
        {
            return true;
        }

        var (success, count) = await CountSubmissionsAsync(originalFormId, version, roomId);

        return !success || count > 0;
    }

    private Task<(bool Success, long Count)> CountSubmissionsAsync(int originalFormId, int version, int roomId)
    {
        return factoryIndexerForm.TryCountAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, version));
    }

    // The link the version may take; one with no version when it takes none. Null when the index could not be read.
    private async Task<FormVersionLink> CompareWithPreviousVersionAsync(File<int> form, FormFillingProperties<int> formFilling)
    {
        var none = new FormVersionLink();
        var roomId = formFilling.RoomId;

        await factoryIndexerFormMetadata.RefreshAsync();
        var (metaSuccess, metaResult) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, form.Id)
             .Where(s => s.RoomId, roomId));
        if (!metaSuccess)
        {
            return null;
        }

        // Compared with the last version whose data is in a table: submitted or linked. A version saved in between and
        // never filled is skipped, so its key changes are asked about together with the later ones.
        DbFormsMetadataSearch previous = null;
        foreach (var candidate in metaResult.Where(m => m.OriginalFormVersion < form.Version && m.Metadata?.Any() == true).OrderByDescending(m => m.OriginalFormVersion))
        {
            if (formFilling.BuiltinDbVersionLinks?.Any(l => l.Version == candidate.OriginalFormVersion) == true)
            {
                previous = candidate;
                break;
            }

            var (submittedSuccess, submitted) = await CountSubmissionsAsync(form.Id, candidate.OriginalFormVersion, roomId);
            if (!submittedSuccess)
            {
                return null;
            }

            if (submitted > 0)
            {
                previous = candidate;
                break;
            }
        }

        if (previous == null)
        {
            return none;
        }

        var (currentSuccess, currentSubmitted) = await CountSubmissionsAsync(form.Id, form.Version, roomId);
        if (!currentSuccess)
        {
            return null;
        }

        // A submitted version is described in the index by its submissions; an unfilled one is read from its file, which
        // may have been overwritten since it was last read.
        List<FormMetadata> metadata;
        Dictionary<string, string> tips = [];
        if (currentSubmitted > 0)
        {
            metadata = (await GetVersionMetadataAsync(form.Id, form.Version)).ToList();
        }
        else
        {
            using var cts = new CancellationTokenSource(_fieldsReadTimeout);
            var json = await formFieldsExtractor.ExtractFieldsJsonAsync(form, cts.Token);
            metadata = ParseSubmitAndMetadata(json).MetaData;
            tips = ParseTips(json);

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
            return none;
        }

        var renames = FormFieldMatcher.Match(NormalizeMetadata(previous.Metadata), NormalizeMetadata(metadata));
        if (renames == null)
        {
            return none;
        }

        foreach (var rename in renames)
        {
            var field = metadata.Find(m => m.Key == rename.NewKey);
            rename.Type = field?.Type;
            rename.Options = field?.PossibleValues is { Count: > 0 } options ? options : null;
            rename.Tip = tips.GetValueOrDefault(rename.NewKey);
        }

        return new FormVersionLink
        {
            Version = form.Version,
            TableVersion = GetBuiltinTableVersion(formFilling, previous.OriginalFormVersion),
            Renames = renames
        };
    }

    // The tips the form author set, by field key; empty when the script could not read them.
    private static Dictionary<string, string> ParseTips(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("tips", out var tips) || tips.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return tips.EnumerateObject()
            .Where(t => t.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(t.Value.GetString()))
            .ToDictionary(t => t.Name, t => t.Value.GetString());
    }

    // The pending decision of the version, taken with its first submission. Returns whether the state changed.
    private async Task<bool> ApplyOnFirstSubmissionAsync(File<int> form, FormFillingProperties<int> state)
    {
        var link = state.BuiltinDbPendingLinks?.Find(p => p.Version == form.Version);
        if (link == null || IsOpenQuestion(link))
        {
            return false;
        }

        if (externalDatabaseClient.IsEnabled() || !builtinFormsDatabaseClient.IsEnabled())
        {
            state.BuiltinDbPendingLinks.Remove(link);
        }
        else if (await builtinFormsDatabaseClient.TableExistsAsync(GetTableName(form.Id, form.Version)))
        {
            // Confirmed after its first submissions, before the background move ran.
            await MergeBuiltinDbVersionAsync(form.Id, state, link);
        }
        else
        {
            state.BuiltinDbPendingLinks.Remove(link);
            await LinkBuiltinDbVersionAsync(form, state, link);
        }

        return true;
    }

    // Under the table lock and saved inside it, so a submission exported meanwhile maps to the columns it finds.
    private async Task LinkBuiltinDbVersionAsync(File<int> form, FormFillingProperties<int> state, FormVersionLink link)
    {
        // Without the description of the version its added columns are unknown, so it keeps a table of its own.
        var current = NormalizeMetadata(await GetVersionMetadataAsync(form.Id, form.Version)).ToList();
        if (current.Count == 0)
        {
            return;
        }

        var tableVersion = GetBuiltinTableVersion(state, link.TableVersion);
        var tableName = GetTableName(form.Id, tableVersion);

        try
        {
            await using (await AcquireBuiltinDbTableLockAsync(tableName))
            {
                // Its submissions would stay apart from the shared table.
                if (await builtinFormsDatabaseClient.TableExistsAsync(GetTableName(form.Id, form.Version)))
                {
                    return;
                }

                var change = await builtinFormsDatabaseClient.AlterTableAsync(tableName, GetColumnRenames([link]), BuildColumnDefinitions(current));
                if (change == FormTableChange.Conflict)
                {
                    logger.WarnBuiltinDbColumnConflict(tableName, form.Version);
                    return;
                }

                link.TableVersion = tableVersion;
                link.Confirmed = null;
                AddLink(state, link);

                await SaveBuiltinDbStateAsync(form.Id, state);
            }
        }
        catch (Exception e)
        {
            logger.WarnBuiltinDbVersionLinkFailed(e, tableName, form.Version);
            state.BuiltinDbVersionLinks?.RemoveAll(l => l.Version == link.Version);
        }
    }

    // Under the version lock. The table of the version and of the versions continuing it are moved into the shared table:
    // rows are copied first, the links saved next, and the table dropped last, only with all its rows in the target.
    private async Task MergeBuiltinDbVersionAsync(int originalFormId, FormFillingProperties<int> state, FormVersionLink link)
    {
        var version = link.Version;
        var tableVersion = GetBuiltinTableVersion(state, link.TableVersion);
        var targetTable = GetTableName(originalFormId, tableVersion);
        var sourceTable = GetTableName(originalFormId, version);

        // The renames of a later version linked to the target are relative to keys this version never had.
        if (state.BuiltinDbVersionLinks?.Any(l => l.TableVersion == tableVersion && l.Version > version) == true)
        {
            logger.WarnBuiltinDbColumnConflict(targetTable, version);
            state.BuiltinDbPendingLinks.Remove(link);
            await SaveBuiltinDbStateAsync(originalFormId, state);
            return;
        }

        var continuing = (state.BuiltinDbVersionLinks ?? []).Where(l => l.TableVersion == version).OrderBy(l => l.Version).ToList();

        try
        {
            await using (await AcquireBuiltinDbTableLockAsync(targetTable))
            await using (await AcquireBuiltinDbTableLockAsync(sourceTable))
            {
                // A table dropped as idle is restored from the index first; without the index the question stays open.
                if (!await EnsureBuiltinDbTableLockedAsync(targetTable, originalFormId, tableVersion, state.RoomId, state)
                    || !await EnsureBuiltinDbTableLockedAsync(sourceTable, originalFormId, version, state.RoomId, state))
                {
                    logger.WarnBuiltinDbMergeDeferred(sourceTable, targetTable);
                    return;
                }

                var change = await builtinFormsDatabaseClient.MergeTableAsync(targetTable, sourceTable, GetColumnRenames(continuing.Prepend(link)));
                if (change == FormTableChange.Missing)
                {
                    logger.WarnBuiltinDbMergeDeferred(sourceTable, targetTable);
                    return;
                }

                state.BuiltinDbPendingLinks.Remove(link);

                if (change == FormTableChange.Conflict)
                {
                    logger.WarnBuiltinDbColumnConflict(targetTable, version);
                    await SaveBuiltinDbStateAsync(originalFormId, state);
                    return;
                }

                link.TableVersion = tableVersion;
                link.Confirmed = null;
                foreach (var continued in continuing)
                {
                    continued.TableVersion = tableVersion;
                }

                AddLink(state, link);
                await SaveBuiltinDbStateAsync(originalFormId, state);

                if (!await builtinFormsDatabaseClient.DropMergedTableAsync(sourceTable, targetTable))
                {
                    logger.WarnBuiltinDbMergedTableKept(sourceTable, targetTable);
                }
            }
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.CheckViolation)
        {
            // A value of the version is not among the options of the shared column: the version keeps its table.
            logger.WarnBuiltinDbMergeFailed(e, sourceTable, targetTable);
            state.BuiltinDbPendingLinks.Remove(link);
            await SaveBuiltinDbStateAsync(originalFormId, state);
        }
        catch (Exception e)
        {
            logger.WarnBuiltinDbMergeFailed(e, sourceTable, targetTable);
        }
    }

    // The field descriptions only serve the question, so an applied link keeps the keys alone.
    private static void AddLink(FormFillingProperties<int> state, FormVersionLink link)
    {
        foreach (var rename in link.Renames ?? [])
        {
            rename.Type = null;
            rename.Options = null;
            rename.Tip = null;
        }

        var links = state.BuiltinDbVersionLinks ??= [];
        links.RemoveAll(l => l.Version == link.Version);
        links.Add(link);
    }

    // The renames of the links in order, as column names.
    private static List<(string OldName, string NewName)> GetColumnRenames(IEnumerable<FormVersionLink> links)
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
        await fileDao.SaveProperties(originalFormId, properties);

        var originalForm = await fileDao.GetFileAsync(originalFormId);
        if (originalForm != null)
        {
            await socketManager.UpdateFileAsync(originalForm);
        }
    }

    public async Task<bool> ExportMissingFromOpenSearchAsync(int originalFormId, int originalFormVersion, int roomId)
    {
        var tableName = GetTableName(originalFormId, originalFormVersion);

        var dbCount = await externalDatabaseClient.GetTableCountAsync(tableName);

        await factoryIndexerForm.RefreshAsync();
        var (osCountSuccess, osCount) = await factoryIndexerForm.TryCountAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, originalFormVersion));

        if (!osCountSuccess)
        {
            return false;
        }

        if (osCount <= dbCount)
        {
            return true;
        }

        var existingIds = await externalDatabaseClient.GetExistingFormIdsAsync(tableName);

        var (success, allSubmissions) = await factoryIndexerForm.TrySelectAsync(r =>
            r.Where(s => s.RoomId, roomId)
             .Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, originalFormVersion)
             .Limit(0, BaseIndexer<DbFormsItemDataSearch>.QueryLimit));

        if (!success)
        {
            return false;
        }

        if (allSubmissions.Count == 0)
        {
            return true;
        }

        var missing = allSubmissions
            .Where(item => !existingIds.Contains(item.Id))
            .ToList();

        if (missing.Count == 0)
        {
            return true;
        }

        var normalizedMeta = await GetExportMetadataAsync(originalFormId, originalFormVersion, missing);
        if (normalizedMeta.Count == 0)
        {
            return false;
        }

        var columnDefinitions = BuildColumnDefinitions(normalizedMeta).ToList();

        var culture = tenantManager.GetCurrentTenant().GetCulture();
        var hadFailure = false;
        var exported = false;

        foreach (var item in missing)
        {
            var rowData = BuildIndexedRowData(item, normalizedMeta, culture, tableName);
            if (rowData == null)
            {
                continue;
            }

            try
            {
                await externalDatabaseClient.CreateTableAndUpsertAsync(
                    tableName, columnDefinitions, rowData, keyColumn: "form_id");
                exported = true;
            }
            catch (Exception ex)
            {
                logger.ErrorGapSyncUpsertFailed(ex, item.Id, tableName);
                hadFailure = true;
            }
        }

        // The gap sync can be the first to create the table (e.g. from the sync button), so record it here too.
        if (exported)
        {
            await SetExternalDbTableNameAsync(originalFormId, tableName);
        }

        return !hadFailure;
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

    public async Task<IEnumerable<DbColumnDefinition>> GetColumnDefinitionsAsync(int originalFormId, int originalFormVersion)
    {
        return BuildColumnDefinitions(NormalizeMetadata(await GetVersionMetadataAsync(originalFormId, originalFormVersion)));
    }

    private async Task<IEnumerable<FormMetadata>> GetVersionMetadataAsync(int originalFormId, int originalFormVersion)
    {
        await factoryIndexerFormMetadata.RefreshAsync();
        var (metaSuccess, metaResult) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
            r.Where(s => s.OriginalFormId, originalFormId)
             .Where(s => s.OriginalFormVersion, originalFormVersion));

        return metaSuccess ? metaResult.FirstOrDefault()?.Metadata ?? [] : [];
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

        var now = DateTime.UtcNow;
        var tenantId = tenantManager.GetCurrentTenantId();

        if (formsDataFile.Id is int id && formsDataFile.ParentId is int parentId)
        {
            var searchItems = new DbFormsItemDataSearch
            {
                Id = id,
                TenantId = tenantId,
                ParentId = parentId,
                OriginalFormId = originalFormId,
                OriginalFormVersion = originalFormVersion,
                RoomId = roomId,
                CreateOn = now,
                FormsData = formNumber.Concat(fromData.FormsData)
            };

            await factoryIndexerForm.IndexAsync(searchItems);
            await factoryIndexerFormMetadata.IndexAsync(new DbFormsMetadataSearch
            {
                Id = DbFormsMetadataSearch.ComputeId(originalFormId, originalFormVersion),
                TenantId = tenantId,
                OriginalFormId = originalFormId,
                OriginalFormVersion = originalFormVersion,
                RoomId = roomId,
                Metadata = fromMetaData
            });
        }

        return (fromData, fromMetaData: fromMetaData);
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

    private static bool IsExportableField(FormsItemData f) => f.Type != "picture" && f.Type != "signature";

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
