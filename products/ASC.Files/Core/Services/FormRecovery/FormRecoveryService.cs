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
    private const int IndexLookupBatchSize = 1000;

    private sealed class FormVersion(File<int> file)
    {
        public File<int> File { get; } = file;

        public HashSet<string> Keys { get; set; }
    }

    /// <summary>
    /// Best effort: a failure is logged, and the report is built from what the index holds.
    /// </summary>
    public async Task RecoverAsync(int roomId, int originalFormId, CancellationToken cancellationToken)
    {
        try
        {
            await RecoverFormAsync(roomId, originalFormId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorRecoveryFormFailed(e, originalFormId, roomId);
        }
    }

    private async Task RecoverFormAsync(int roomId, int originalFormId, CancellationToken cancellationToken)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var room = await folderDao.GetFolderAsync(roomId);
        var form = await fileDao.GetFileAsync(originalFormId);
        var formFilling = (await fileDao.GetProperties(originalFormId))?.FormFilling;
        if (room is not { FolderType: FolderType.FillingFormsRoom } || form == null || formFilling is not { ResultsFolderId: not 0 })
        {
            return;
        }

        var doneFolder = await folderDao.GetFolderAsync(formFilling.ResultsFolderId);
        if (doneFolder is not { FolderType: FolderType.FormFillingFolderDone })
        {
            return;
        }

        var orphans = await FindOrphansAsync(fileDao, doneFolder);
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

        foreach (var orphan in orphans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (await RecoverSubmissionAsync(orphan, doneFolder, roomId, form, versions, fileDao, cancellationToken) is { } version)
                {
                    recoveredVersions.Add(version);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.ErrorRecoverySubmissionFailed(e, orphan.Id, originalFormId);
            }
        }

        // The external database is filled from the index, as its sync button does.
        foreach (var version in recoveredVersions)
        {
            await formFillingReportCreator.SyncExternalDbAsync(room, originalFormId, version);
        }
    }

    // Empty when the index cannot be read, so that a failed query does not make every form look missing.
    private async Task<List<File<int>>> FindOrphansAsync(IFileDao<int> fileDao, Folder<int> doneFolder)
    {
        // A file put here by hand may lack the form category, so the extension decides.
        var completed = await fileDao.GetFilesAsync(doneFolder.Id, null, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false)
            .Where(f => string.Equals(FileUtility.GetFileExtension(f.Title), ".pdf", StringComparison.OrdinalIgnoreCase))
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
        foreach (var ids in completed.Select(f => f.Id).Chunk(IndexLookupBatchSize))
        {
            var (success, items) = await factoryIndexerForm.TrySelectAsync(r => r.In(s => s.Id, ids).Limit(0, ids.Length));
            if (!success)
            {
                logger.WarnRecoveryIndexQueryFailed(doneFolder.Id);
                return [];
            }

            indexed.UnionWith(items.Select(i => i.Id));
        }

        return completed.Where(f => !indexed.Contains(f.Id)).ToList();
    }

    // From the index, or from that version of the form file; read once per run.
    private async Task<HashSet<string>> GetVersionKeysAsync(FormVersion version, CancellationToken cancellationToken)
    {
        if (version.Keys != null)
        {
            return version.Keys;
        }

        var keys = await formFillingReportCreator.TryGetIndexedFieldKeysAsync(version.File.Id, version.File.Version);
        if (keys == null)
        {
            try
            {
                keys = formFillingReportCreator.GetFieldKeys(await formFieldsExtractor.ExtractFieldsJsonAsync(version.File, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.WarnRecoveryVersionFieldsFailed(e, version.File.Id, version.File.Version);
                keys = [];
            }
        }

        version.Keys = keys;

        return keys;
    }

    // The version the submission was indexed under; null when it was skipped.
    private async Task<int?> RecoverSubmissionAsync(
        File<int> orphan,
        Folder<int> doneFolder,
        int roomId,
        File<int> form,
        List<FormVersion> versions,
        IFileDao<int> fileDao,
        CancellationToken cancellationToken)
    {
        var formsDataJson = await formFieldsExtractor.ExtractFieldsJsonAsync(orphan, cancellationToken);
        var keys = formFillingReportCreator.GetFieldKeys(formsDataJson);
        if (keys.Count == 0)
        {
            logger.WarnRecoveryNoFormData(orphan.Id, form.Id);
            return null;
        }

        // A completed form does not record its version: the newest one with the same fields is taken, and a file matching
        // none is skipped, since the report lays the values out by the columns of the version.
        int? version = null;
        for (var i = versions.Count - 1; i >= 0 && version == null; i--)
        {
            if ((await GetVersionKeysAsync(versions[i], cancellationToken)).SetEquals(keys))
            {
                version = versions[i].File.Version;
            }
        }

        if (version == null)
        {
            logger.WarnRecoveryVersionUnmatched(orphan.Id, form.Id, roomId);
            return null;
        }

        // A submission saves and indexes its PDF under this lock, so a file still not indexed once it is taken was lost.
        // A file without a number gets the next one, saved on it so that a retry reuses it.
        int resultFormNumber;
        await using (await distributedLockProvider.TryAcquireFairLockAsync($"fillform_{roomId}_{form.Id}"))
        {
            await factoryIndexerForm.RefreshAsync();
            var (success, indexed) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.Id, orphan.Id).Limit(0, 1));
            if (!success || indexed.Count > 0)
            {
                return null;
            }

            var properties = await fileDao.GetProperties(orphan.Id) ?? new EntryProperties<int>();
            properties.FormFilling ??= new FormFillingProperties<int>();

            resultFormNumber = properties.FormFilling.ResultFormNumber;
            if (resultFormNumber == 0)
            {
                var formProperties = await fileDao.GetProperties(form.Id);
                resultFormNumber = ++formProperties.FormFilling.ResultFormNumber;
                await fileDao.SaveProperties(form.Id, formProperties);

                properties.FormFilling.OriginalFormId = form.Id;
                properties.FormFilling.RoomId = roomId;
                properties.FormFilling.ResultsFolderId = doneFolder.Id;
                properties.FormFilling.ResultFormNumber = resultFormNumber;
                await fileDao.SaveProperties(orphan.Id, properties);
            }
        }

        await formFillingReportCreator.IndexRecoveredSubmissionAsync(
            orphan, form.Id, version.Value, roomId, resultFormNumber, formsDataJson, tenantUtil.DateTimeToUtc(orphan.CreateOn));

        return version;
    }
}
