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

#nullable enable

/// <summary>
/// Recovers form field data for completed form PDFs that ended up in a form's "Complete" folder without ever
/// going through the normal submit flow (e.g. restored from a backup or copied in manually), and therefore
/// have neither their own <see cref="FormFillingProperties{T}"/> nor an indexed submission record. Field
/// values are read from the PDF via a DocBuilder script, then fed into the same ingestion path a normal
/// submission uses (<see cref="FormFillingReportCreator.UpdateFormFillingReport{T}"/>).
/// </summary>
[Scope]
public class FormRecoveryService(
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    FactoryIndexerForm factoryIndexerForm,
    FormFillingReportCreator formFillingReportCreator,
    FormFieldsExtractor formFieldsExtractor,
    DocumentBuilderTask documentBuilderTask,
    PathProvider pathProvider,
    GlobalStore globalStore,
    IHttpClientFactory httpClientFactory,
    IDistributedLockProvider distributedLockProvider,
    TenantUtil tenantUtil,
    SocketManager socketManager,
    ILogger<FormRecoveryService> logger)
{
    /// <summary>
    /// If the form's completed PDFs are desynced from the search index (some completed forms have no indexed
    /// submission record), repairs the orphaned forms and rebuilds the form's xlsx report history, then
    /// returns <c>true</c>. When everything is already in sync returns <c>false</c> so the caller performs the
    /// ordinary report build instead.
    /// </summary>
    public async Task<bool> TryRecoverFormAsync(int roomId, int originalFormId, Guid userId, CancellationToken cancellationToken)
    {
        var fileDao = daoFactory.GetFileDao<int>();
        var folderDao = daoFactory.GetFolderDao<int>();

        var origProperties = await fileDao.GetProperties(originalFormId);
        if (origProperties?.FormFilling is not { ResultsFolderId: not 0 } formFilling)
        {
            return false;
        }

        var doneFolder = await folderDao.GetFolderAsync(formFilling.ResultsFolderId);
        if (doneFolder is not { FolderType: FolderType.FormFillingFolderDone })
        {
            return false;
        }

        var room = await folderDao.GetFolderAsync(roomId);
        if (room is not { FolderType: FolderType.FillingFormsRoom })
        {
            return false;
        }

        var orphans = await FindOrphanedFormsAsync(fileDao, doneFolder);
        if (orphans.Count == 0)
        {
            return false;
        }

        logger.InfoRecoveryOrphansFound(roomId, orphans.Count);

        var currentForm = await fileDao.GetFileAsync(originalFormId);
        if (currentForm == null)
        {
            return false;
        }

        var versionKeySets = await GetVersionKeySetsAsync(originalFormId, currentForm, fileDao, cancellationToken);

        var repaired = 0;
        foreach (var orphan in orphans)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await RepairFormAsync(orphan, doneFolder, room, originalFormId, currentForm, versionKeySets, fileDao, cancellationToken);
                repaired++;
            }
            catch (Exception e)
            {
                logger.ErrorRecoveryFormFailed(e, originalFormId, roomId);
            }
        }

        // Nothing repaired (or no xlsx kept): let the ordinary index build produce the report instead.
        if (repaired == 0 || !room.SettingsSaveFormAsXLSX)
        {
            return false;
        }

        try
        {
            await RebuildReportHistoryAsync(roomId, originalFormId, userId, cancellationToken);
        }
        catch (Exception e)
        {
            logger.ErrorRecoveryXlsxRebuildFailed(e, originalFormId, roomId);
        }

        return true;
    }

    /// <summary>
    /// Finds completed form PDFs in the form's "Complete" folder that have no matching submission record in the
    /// search index yet.
    /// </summary>
    private async Task<List<File<int>>> FindOrphanedFormsAsync(IFileDao<int> fileDao, Folder<int> doneFolder)
    {
        // Orphans skipped the signature sniffing, so may lack the PdfForm category stamp — match by extension.
        var completedFiles = (await fileDao.GetFilesAsync(doneFolder.Id, null, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false).ToListAsync())
            .Where(f => string.Equals(FileUtility.GetFileExtension(f.Title), ".pdf", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (completedFiles.Count == 0)
        {
            return [];
        }

        factoryIndexerForm.Refresh();

        var (indexSuccess, indexedItems) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.ParentId, doneFolder.Id));

        if (!indexSuccess)
        {
            // A failed query looks the same as "nothing indexed yet" — don't treat it as "all orphaned".
            logger.WarnRecoveryIndexQueryFailed(doneFolder.Id);
            return [];
        }

        var indexedIds = indexedItems.Select(i => i.Id).ToHashSet();

        return completedFiles.Where(f => !indexedIds.Contains(f.Id)).ToList();
    }

    private async Task RepairFormAsync(
        File<int> orphan,
        Folder<int> doneFolder,
        Folder<int> room,
        int originalFormId,
        File<int> originalForm,
        List<(int Version, HashSet<string> Keys)> versionKeySets,
        IFileDao<int> fileDao,
        CancellationToken cancellationToken)
    {
        var existing = (await fileDao.GetProperties(orphan.Id))?.FormFilling;
        var roomId = room.Id;

        // Completed forms don't reliably record their template version — route by matching the PDF's field key
        // set to the version whose layout it matches, keeping each version's report to one consistent column set.
        var orphanFormsDataJson = await formFieldsExtractor.ExtractFieldsJsonAsync(orphan, lastVersion: true, cancellationToken);
        var orphanKeys = FormFieldsExtractor.ParseFields(orphanFormsDataJson).Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Tie (a revision that didn't change fields) → newest match; no match → current version (keep the data).
        var matchedVersion = versionKeySets
            .Where(x => x.Keys.SetEquals(orphanKeys))
            .Select(x => (int?)x.Version)
            .Max();

        int effectiveVersion;
        if (matchedVersion is { } matched)
        {
            effectiveVersion = matched;
        }
        else
        {
            effectiveVersion = originalForm.Version;
            logger.WarnRecoveryVersionUnmatched(orphan.Id, originalFormId, roomId);
        }

        string formsDataUrl;
        using (var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(orphanFormsDataJson)))
        {
            formsDataUrl = await pathProvider.GetTempUrlAsync(jsonStream, ".json");
        }

        int resultFormNumber;
        var assignedNewNumber = false;
        var lockKey = $"fillform_{roomId}_{originalFormId}";

        try
        {
            // Reuse the number the form was given when it was originally filled (kept in its own properties and
            // reflected in its title), so the report's "form number" matches the completed file. Only mint a
            // fresh sequential number when it's unknown (e.g. a backup restore that lost the properties).
            if (existing is { ResultFormNumber: > 0 })
            {
                resultFormNumber = existing.ResultFormNumber;
            }
            else
            {
                await using (await distributedLockProvider.TryAcquireFairLockAsync(lockKey))
                {
                    var origProperties = await fileDao.GetProperties(originalFormId) ?? new EntryProperties<int>();
                    origProperties.FormFilling ??= new FormFillingProperties<int>();
                    origProperties.FormFilling.ResultFormNumber++;
                    resultFormNumber = origProperties.FormFilling.ResultFormNumber;
                    await fileDao.SaveProperties(originalFormId, origProperties);
                }

                assignedNewNumber = true;
            }

            try
            {
                await formFillingReportCreator.UpdateFormFillingReport(
                    originalFormId,
                    effectiveVersion,
                    roomId,
                    resultFormNumber,
                    formsDataUrl,
                    orphan,
                    room.SettingsSendFormToExternalDB,
                    room.SettingsSaveFormAsXLSX,
                    triggerXlsxUpdate: false,
                    // Report timestamp = when the form was filled, not this run. File times are tenant-local,
                    // so convert to UTC to match what a normal submission stores.
                    filledOn: tenantUtil.DateTimeToUtc(orphan.ModifiedOn));
            }
            catch
            {
                // Undo the counter bump above (only when we minted a new number) unless something else already
                // claimed a later number, otherwise a retry would skip a number permanently.
                if (assignedNewNumber)
                {
                    await using (await distributedLockProvider.TryAcquireFairLockAsync(lockKey))
                    {
                        var origProperties = await fileDao.GetProperties(originalFormId);
                        if (origProperties?.FormFilling?.ResultFormNumber == resultFormNumber)
                        {
                            origProperties.FormFilling.ResultFormNumber--;
                            await fileDao.SaveProperties(originalFormId, origProperties);
                        }
                    }
                }

                throw;
            }
        }
        finally
        {
            // No-op if UpdateFormFillingReport already read (and thereby self-deleted) the temp file; cleans it
            // up if a failure above prevented that from ever happening.
            await CleanupTempFormsDataFileAsync(formsDataUrl);
        }

        await fileDao.SaveProperties(orphan.Id, new EntryProperties<int>
        {
            FormFilling = new FormFillingProperties<int>
            {
                StartFilling = false,
                OriginalFormId = originalFormId,
                OriginalFormVersion = effectiveVersion,
                RoomId = roomId,
                ResultsFolderId = doneFolder.Id,
                ResultFormNumber = resultFormNumber
            }
        });
    }

    /// <summary>
    /// Rebuilds the form's results xlsx version history from the index — one file version per form version that
    /// has submissions, oldest first — which recovering many versions at once can't get from the normal build.
    /// </summary>
    private async Task RebuildReportHistoryAsync(int roomId, int formId, Guid userId, CancellationToken cancellationToken)
    {
        var fileDao = daoFactory.GetFileDao<int>();

        var origProperties = await fileDao.GetProperties(formId);
        if (origProperties?.FormFilling is not { ResultsFileID: not 0 } formFilling)
        {
            return;
        }

        var currentForm = await fileDao.GetFileAsync(formId);
        var resultFile = await fileDao.GetFileAsync(formFilling.ResultsFileID);
        if (currentForm == null || resultFile == null)
        {
            return;
        }

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource("FormFillingReport.docbuilder")
            ?? throw new InvalidOperationException("FormFillingReport.docbuilder template not found.");

        // The results file may already carry partial versions built online before the outage. The index (now
        // including the recovered rows) is the source of truth for the full history, so collapse the file back
        // to a single version and rebuild every version from scratch below.
        while (resultFile.Version > 1)
        {
            await fileDao.DeleteFileVersionAsync(resultFile, resultFile.Version);
            resultFile.Version--;
        }

        var isFirst = true;

        for (var version = 1; version <= currentForm.Version; version++)
        {
            var submissions = await formFillingReportCreator.GetFormFillingResults(roomId, formId, version);
            if (!submissions.Any())
            {
                continue;
            }

            var reportData = await FormFillingReportTask.GetFormFillingReportData(serviceProvider, userId, roomId, formId, version);
            var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
            var versionScript = script
                .Replace("${tempFileName}", tempFileName)
                .Replace("${inputData}", JsonSerializer.Serialize(reportData));

            var xlsxUrl = await documentBuilderTask.BuildFileAsync(new DocumentBuilderInputData(versionScript, tempFileName, ""), cancellationToken);

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
            var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000
            using var response = await httpClient.GetAsync(xlsxUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            resultFile.ContentLength = buffer.Length;

            // First version replaces the single remaining file version; each later version is appended as a new
            // file version, so the file's version history mirrors the form's versions.
            if (isFirst)
            {
                resultFile = await fileDao.ReplaceFileVersionAsync(resultFile, buffer);
                isFirst = false;
            }
            else
            {
                resultFile.Version++;
                resultFile.VersionGroup++;
                resultFile = await fileDao.SaveFileAsync(resultFile, buffer, false);
            }

            logger.InfoRecoveryReportVersionRebuilt(formId, version, submissions.Count(), resultFile.Version);
        }

        if (isFirst)
        {
            return;
        }

        // Point the form at the rebuilt file and clear the pending version-change flag so the next live
        // submission replaces the current report version instead of bumping it again.
        formFilling.ResultsFileID = resultFile.Id;
        formFilling.IsVersionChanged = false;
        await fileDao.SaveProperties(formId, origProperties);

        await fileDao.SaveProperties(resultFile.Id, new EntryProperties<int>
        {
            FormFilling = new FormFillingProperties<int>
            {
                StartFilling = false,
                OriginalFormId = formId,
                OriginalFormVersion = currentForm.Version,
                RoomId = roomId,
                ResultsFolderId = formFilling.ResultsFolderId,
                ResultsFileID = resultFile.Id
            }
        });

        await socketManager.UpdateFileAsync(resultFile);
    }

    /// <summary>
    /// Best-effort delete of the temp JSON uploaded for a repaired orphan, for when it never got read (and
    /// thereby self-deleted). Safe to call even if it was already consumed.
    /// </summary>
    private async Task CleanupTempFormsDataFileAsync(string formsDataUrl)
    {
        try
        {
            var fileName = HttpUtility.ParseQueryString(new Uri(formsDataUrl).Query)[FilesLinkUtility.FileTitle];
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var store = await globalStore.GetStoreAsync();
            var path = CrossPlatform.PathCombine("temp_stream", fileName);

            if (await store.IsFileAsync(FileConstant.StorageDomainTmp, path))
            {
                await store.DeleteAsync(FileConstant.StorageDomainTmp, path);
            }
        }
        catch (Exception e)
        {
            logger.WarnRecoveryTempCleanupFailed(e, formsDataUrl);
        }
    }

    /// <summary>
    /// Resolves the field key set of each version (1..current) of the original form, used to route each orphan
    /// into the report of the version whose field layout it matches. Prefers the layout already indexed in the
    /// search metadata; for versions never indexed (only ever filled while the index was down) it reads the
    /// fields straight from that version of the template PDF.
    /// </summary>
    private async Task<List<(int Version, HashSet<string> Keys)>> GetVersionKeySetsAsync(
        int originalFormId, File<int> currentForm, IFileDao<int> fileDao, CancellationToken cancellationToken)
    {
        var result = new List<(int Version, HashSet<string> Keys)>();

        for (var version = 1; version <= currentForm.Version; version++)
        {
            var keys = await formFillingReportCreator.TryGetIndexedFieldKeysAsync(originalFormId, version);

            if (keys == null)
            {
                var templateAtVersion = version == currentForm.Version
                    ? currentForm
                    : await fileDao.GetFileAsync(originalFormId, version);

                if (templateAtVersion != null)
                {
                    try
                    {
                        var templateJson = await formFieldsExtractor.ExtractFieldsJsonAsync(templateAtVersion, lastVersion: false, cancellationToken);
                        keys = FormFieldsExtractor.ParseFields(templateJson).Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                    catch (Exception e)
                    {
                        // A version whose layout can't be resolved simply won't participate in matching.
                        logger.WarnRecoveryTemplateVersionExtractFailed(e, originalFormId, version);
                    }
                }
            }

            if (keys is { Count: > 0 })
            {
                result.Add((version, keys));
            }
        }

        return result;
    }
}
