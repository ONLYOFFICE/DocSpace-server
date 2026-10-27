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

namespace ASC.Files.Core.Services.FormRecovery;

/// <summary>
/// Indexes the completed forms missing from the search index, the only store of form responses, reading their values
/// back from the PDFs.
/// </summary>
[Scope]
public class FormRecoveryService(
    IDaoFactory daoFactory,
    FactoryIndexerForm factoryIndexerForm,
    FactoryIndexerFormMetadata factoryIndexerFormMetadata,
    FormFillingReportCreator formFillingReportCreator,
    FormFieldsExtractor formFieldsExtractor,
    IDistributedLockProvider distributedLockProvider,
    TenantUtil tenantUtil,
    ILogger<FormRecoveryService> logger)
{
    private const int LookupBatchSize = 1000;

    // A file that fails this often is not read through the builder again until it gets a new version.
    private const int MaxRecoveryAttempts = 3;

    // Passing failures on this many forms in a row mean the builder or the network is down, not that the forms are bad.
    private const int MaxPassingFailuresInRow = 3;

    // The key a form submission takes in EntryManager: the recovery is safe only while the two are the same.
    private static string GetSubmissionLockKey(int roomId, int originalFormId) => $"fillform_{roomId}_{originalFormId}";

    private sealed class FormVersion(File<int> file)
    {
        public File<int> File { get; } = file;

        public HashSet<string> Keys { get; set; }

        // The fields could not be read, so a submission matching no version may still be of this one.
        public bool KeysFailed { get; set; }

        // The description written on submission is kept; one read back from a file is written only when there is none.
        public bool HasMetadata { get; set; }
    }

    private enum RecoveryOutcome
    {
        Recovered,
        Skipped,
        Postponed,
        Failed,
        Unrecoverable
    }

    /// <summary>
    /// Best effort: a failure is logged, and the report is built from what the index holds. The progress, the share of the
    /// forms read from 0 to 1, is reported for every form.
    /// </summary>
    public async Task RecoverAsync(int roomId, int originalFormId, Action<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            await RecoverFormAsync(roomId, originalFormId, progress, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorRecoveryFormFailed(e, originalFormId, roomId);
        }
    }

    /// <summary>
    /// The versions of the form with responses in the search index, oldest first; null when the index cannot be read.
    /// </summary>
    public async Task<List<int>> GetVersionsWithResponsesAsync(int roomId, int originalFormId)
    {
        var history = await daoFactory.GetFileDao<int>().GetFileHistoryAsync(originalFormId).Select(v => v.Version).ToListAsync();

        await factoryIndexerForm.RefreshAsync();

        var versions = new List<int>();
        foreach (var version in history.Distinct().Order())
        {
            var (success, count) = await factoryIndexerForm.TryCountAsync(r => r
                .Where(s => s.RoomId, roomId)
                .Where(s => s.OriginalFormId, originalFormId)
                .Where(s => s.OriginalFormVersion, version));
            if (!success)
            {
                return null;
            }

            if (count > 0)
            {
                versions.Add(version);
            }
        }

        return versions;
    }

    /// <summary>
    /// Sets whether the next report of the form starts a new version of its file, as a change of the form does.
    /// </summary>
    public async Task SetReportVersionChangedAsync(int roomId, int originalFormId, bool changed)
    {
        var fileDao = daoFactory.GetFileDao<int>();

        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetSubmissionLockKey(roomId, originalFormId)))
        {
            var properties = await fileDao.GetProperties(originalFormId);
            if (properties?.FormFilling == null || properties.FormFilling.IsVersionChanged == changed)
            {
                return;
            }

            properties.FormFilling.IsVersionChanged = changed;
            await fileDao.SaveProperties(originalFormId, properties);
        }
    }

    /// <summary>
    /// Whether the folder of completed forms holds forms missing from the search index; false when the index cannot be read.
    /// </summary>
    public async Task<bool> HasMissingResponsesAsync(int roomId, int originalFormId)
    {
        var (_, _, _, orphans) = await FindFormOrphansAsync(daoFactory.GetFileDao<int>(), roomId, originalFormId);

        return orphans.Count > 0;
    }

    private async Task<(Folder<int> Room, File<int> Form, Folder<int> DoneFolder, List<File<int>> Orphans)> FindFormOrphansAsync(
        IFileDao<int> fileDao,
        int roomId,
        int originalFormId)
    {
        var folderDao = daoFactory.GetFolderDao<int>();

        var room = await folderDao.GetFolderAsync(roomId);
        var form = await fileDao.GetFileAsync(originalFormId);
        var formFilling = (await fileDao.GetProperties(originalFormId))?.FormFilling;
        if (room is not { FolderType: FolderType.FillingFormsRoom } || form == null || formFilling is not { ResultsFolderId: not 0 })
        {
            return (room, form, null, []);
        }

        var doneFolder = await folderDao.GetFolderAsync(formFilling.ResultsFolderId);
        if (doneFolder is not { FolderType: FolderType.FormFillingFolderDone })
        {
            return (room, form, null, []);
        }

        return (room, form, doneFolder, await FindOrphansAsync(fileDao, doneFolder, originalFormId));
    }

    private async Task RecoverFormAsync(int roomId, int originalFormId, Action<double> progress, CancellationToken cancellationToken)
    {
        var fileDao = daoFactory.GetFileDao<int>();

        // Searched again after the check that queued the run: the run may have waited for a slot, and the responses
        // indexed meanwhile must not be read through the builder.
        var (room, form, doneFolder, orphans) = await FindFormOrphansAsync(fileDao, roomId, originalFormId);
        if (orphans.Count == 0)
        {
            return;
        }

        logger.InfoRecoveryOrphansFound(originalFormId, roomId, orphans.Count);

        var versions = (await fileDao.GetFileHistoryAsync(originalFormId).ToListAsync())
            .OrderBy(v => v.Version)
            .Select(v => new FormVersion(v))
            .ToList();
        var recoveredVersions = new HashSet<int>();
        var passingFailures = 0;

        for (var i = 0; i < orphans.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress((double)i / orphans.Count);

            var orphan = orphans[i];

            Exception passingFailure = null;
            var (outcome, version) = (RecoveryOutcome.Failed, 0);
            try
            {
                (outcome, version) = await RecoverSubmissionAsync(orphan, doneFolder, roomId, form, versions, fileDao, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e) when (IsPassingFailure(e))
            {
                passingFailure = e;
                outcome = RecoveryOutcome.Postponed;
            }
            catch (Exception e)
            {
                logger.ErrorRecoverySubmissionFailed(e, orphan.Id, originalFormId);
            }

            if (outcome == RecoveryOutcome.Postponed)
            {
                // Not the file's fault: it is left for the next report, uncounted, so that an outage does not use up the
                // attempts of every form, and the forms after it are still read.
                logger.WarnRecoverySubmissionPostponed(passingFailure, orphan.Id, originalFormId);
                if (++passingFailures >= MaxPassingFailuresInRow)
                {
                    logger.WarnRecoveryStopped(passingFailure, originalFormId, i + 1, orphans.Count);
                    break;
                }

                continue;
            }

            passingFailures = 0;

            if (outcome == RecoveryOutcome.Recovered)
            {
                recoveredVersions.Add(version);
            }
            else if (outcome is RecoveryOutcome.Failed or RecoveryOutcome.Unrecoverable)
            {
                await CountFailedAttemptAsync(orphan, roomId, form, fileDao, outcome == RecoveryOutcome.Unrecoverable);
            }
        }

        if (recoveredVersions.Count == 0)
        {
            return;
        }

        await factoryIndexerFormMetadata.RefreshAsync();
        await factoryIndexerForm.RefreshAsync();

        // The external database is filled from the index, as its sync button does.
        foreach (var version in recoveredVersions)
        {
            await formFillingReportCreator.SyncExternalDbAsync(room, originalFormId, version);
        }
    }

    // Empty when the index cannot be read, so that a failed query does not make every form look missing.
    // A file that failed too often is left out until it gets a new version, and so is a response of another form.
    private async Task<List<File<int>>> FindOrphansAsync(IFileDao<int> fileDao, Folder<int> doneFolder, int originalFormId)
    {
        // Only a submission puts a file here, but an older one may lack the form category, so the extension decides.
        var completed = await fileDao.GetFilesAsync(doneFolder.Id, null, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false)
            .Where(f => FileUtility.GetFileTypeByFileName(f.Title) == FileType.Pdf)
            .ToListAsync();

        if (completed.Count == 0)
        {
            return [];
        }

        if (!await factoryIndexerForm.SupportAsync())
        {
            logger.WarnRecoveryIndexQueryFailed(doneFolder.Id);
            return [];
        }

        factoryIndexerForm.CreateIfNotExist();
        factoryIndexerFormMetadata.CreateIfNotExist();
        await factoryIndexerForm.RefreshAsync();

        var indexed = new HashSet<int>();
        foreach (var ids in completed.Select(f => f.Id).Chunk(LookupBatchSize))
        {
            var (success, found) = await factoryIndexerForm.TrySelectIdsAsync(r => r.In(s => s.Id, ids).Limit(0, ids.Length));
            if (!success)
            {
                logger.WarnRecoveryIndexQueryFailed(doneFolder.Id);
                return [];
            }

            indexed.UnionWith(found);
        }

        var orphans = new List<File<int>>();
        foreach (var files in completed.Where(f => !indexed.Contains(f.Id)).Chunk(LookupBatchSize))
        {
            var properties = await fileDao.GetPropertiesAsync(files.Select(f => f.Id));
            orphans.AddRange(files.Where(f =>
            {
                var formFilling = properties.GetValueOrDefault(f.Id)?.FormFilling;
                if (formFilling is { OriginalFormId: not 0 } && formFilling.OriginalFormId != originalFormId)
                {
                    return false;
                }

                return formFilling?.RecoveryFailedVersion != f.Version || (formFilling.RecoveryFailedAttempts ?? 0) < MaxRecoveryAttempts;
            }));
        }

        return orphans;
    }

    // A failure of the builder, the network or a lock, which passes. Anything else, a defect included, counts as an
    // attempt on the file, so that it cannot hold back the forms after it for good.
    private static bool IsPassingFailure(Exception e) =>
        e is HttpRequestException or TimeoutException or TaskCanceledException or DistributedLockException
        || e is IOException and not (FileNotFoundException or DirectoryNotFoundException)
        || e is DocumentServiceException
        {
            Code: not (DocumentServiceException.ErrorCode.Convert or DocumentServiceException.ErrorCode.SizeLimit or DocumentServiceException.ErrorCode.ConvertPassword)
        };

    // From the index, or from that version of the form file; read once per run.
    private async Task<HashSet<string>> GetVersionKeysAsync(FormVersion version, CancellationToken cancellationToken)
    {
        if (version.Keys != null)
        {
            return version.Keys;
        }

        var keys = await formFillingReportCreator.TryGetIndexedFieldKeysAsync(version.File.Id, version.File.Version);
        version.HasMetadata = keys != null;
        if (keys == null)
        {
            try
            {
                keys = formFillingReportCreator.GetFieldKeys(await formFieldsExtractor.ExtractFieldsJsonAsync(version.File, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e) when (!IsPassingFailure(e))
            {
                logger.WarnRecoveryVersionFieldsFailed(e, version.File.Id, version.File.Version);
                keys = [];
                version.KeysFailed = true;
            }
        }

        version.Keys = keys;

        return keys;
    }

    // The version is set for a recovered submission only.
    private async Task<(RecoveryOutcome Outcome, int Version)> RecoverSubmissionAsync(
        File<int> orphan,
        Folder<int> doneFolder,
        int roomId,
        File<int> form,
        List<FormVersion> versions,
        IFileDao<int> fileDao,
        CancellationToken cancellationToken)
    {
        // The first version is what was submitted: a later one is an edit of the completed form, not the filler's answers.
        var submitted = orphan.Version > 1 ? await fileDao.GetFileAsync(orphan.Id, 1) ?? orphan : orphan;
        var formsDataJson = await formFieldsExtractor.ExtractFieldsJsonAsync(submitted, cancellationToken);
        var keys = formFillingReportCreator.GetFieldKeys(formsDataJson);
        if (keys.Count == 0)
        {
            logger.WarnRecoveryNoFormData(orphan.Id, form.Id);
            return (RecoveryOutcome.Unrecoverable, 0);
        }

        // A completed form does not record its version. A draft of an older version cannot be submitted, so the response is
        // of the version current when it was sent: the newest one saved by then, tried first, since versions often share
        // their fields. If its fields differ, the newest version with the same fields is taken, and a file matching none
        // is skipped, since the report lays the values out by the columns of the version.
        var currentOnSubmission = versions.LastOrDefault(v => v.File.ModifiedOn <= orphan.CreateOn);

        FormVersion version = null;
        foreach (var candidate in versions.AsEnumerable().Reverse().OrderByDescending(v => v == currentOnSubmission))
        {
            if ((await GetVersionKeysAsync(candidate, cancellationToken)).SetEquals(keys))
            {
                version = candidate;
                break;
            }
        }

        if (version == null)
        {
            logger.WarnRecoveryVersionUnmatched(orphan.Id, form.Id, roomId);
            return (versions.Any(v => v.KeysFailed) ? RecoveryOutcome.Failed : RecoveryOutcome.Unrecoverable, 0);
        }

        // A submission saves and indexes its PDF under this lock, refreshing the index, so a file still not indexed once
        // it is taken was lost. A file without a number gets the next one, saved on it so that a retry reuses it.
        int resultFormNumber;
        await using (await distributedLockProvider.TryAcquireFairLockAsync(GetSubmissionLockKey(roomId, form.Id)))
        {
            var (success, indexed) = await factoryIndexerForm.TrySelectIdsAsync(r => r.Where(s => s.Id, orphan.Id).Limit(0, 1));
            if (!success)
            {
                return (RecoveryOutcome.Postponed, 0);
            }

            if (indexed.Count > 0)
            {
                return (RecoveryOutcome.Skipped, 0);
            }

            var properties = await fileDao.GetProperties(orphan.Id) ?? new EntryProperties<int>();
            properties.FormFilling ??= new FormFillingProperties<int>();

            resultFormNumber = properties.FormFilling.ResultFormNumber;
            if (resultFormNumber == 0)
            {
                // The filling was stopped while the run went on.
                var formProperties = await fileDao.GetProperties(form.Id);
                if (formProperties?.FormFilling == null)
                {
                    return (RecoveryOutcome.Skipped, 0);
                }

                resultFormNumber = ++formProperties.FormFilling.ResultFormNumber;
                await fileDao.SaveProperties(form.Id, formProperties);

                properties.FormFilling.OriginalFormId = form.Id;
                properties.FormFilling.RoomId = roomId;
                properties.FormFilling.ResultsFolderId = doneFolder.Id;
                properties.FormFilling.ResultFormNumber = resultFormNumber;
                await fileDao.SaveProperties(orphan.Id, properties);
            }
        }

        if (!await formFillingReportCreator.IndexRecoveredSubmissionAsync(
                orphan, form.Id, version.File.Version, roomId, resultFormNumber, formsDataJson, tenantUtil.DateTimeToUtc(orphan.CreateOn), !version.HasMetadata))
        {
            // The index refused or could not be reached: the number stays on the file for the next report.
            return (RecoveryOutcome.Postponed, 0);
        }

        version.HasMetadata = true;

        return (RecoveryOutcome.Recovered, version.File.Version);
    }

    // Counted for the version of the file, so that a new version is read again; an unrecoverable file takes all the
    // attempts at once. Under the submission lock, as the other writes of the file's properties.
    private async Task CountFailedAttemptAsync(File<int> orphan, int roomId, File<int> form, IFileDao<int> fileDao, bool unrecoverable)
    {
        try
        {
            await using (await distributedLockProvider.TryAcquireFairLockAsync(GetSubmissionLockKey(roomId, form.Id)))
            {
                var properties = await fileDao.GetProperties(orphan.Id) ?? new EntryProperties<int>();
                var formFilling = properties.FormFilling ??= new FormFillingProperties<int>();

                var attempts = formFilling.RecoveryFailedVersion == orphan.Version ? formFilling.RecoveryFailedAttempts ?? 0 : 0;
                formFilling.RecoveryFailedVersion = orphan.Version;
                formFilling.RecoveryFailedAttempts = unrecoverable ? MaxRecoveryAttempts : attempts + 1;
                await fileDao.SaveProperties(orphan.Id, properties);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.ErrorRecoverySubmissionFailed(e, orphan.Id, form.Id);
        }
    }
}
