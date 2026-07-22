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
/// Recovers form field data for completed form PDFs that ended up in a "Complete" folder without ever
/// going through the normal submit flow (e.g. restored from a backup or copied in manually), and therefore
/// have neither their own <see cref="FormFillingProperties{T}"/> nor an indexed submission record.
/// Field values are read from the PDF itself via a Document Server DocBuilder script
/// (<c>ExtractFormFieldsData.docbuilder</c>), then fed into the same ingestion path used by a normal
/// form submission (<see cref="FormFillingReportCreator.UpdateFormFillingReport{T}"/>), so the xlsx report
/// pipeline itself is not duplicated.
/// </summary>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(FormRecoveryTask), "FormRecoveryTask")]
public abstract class FormRecoveryTaskBase : DistributedTaskProgress { }

[Transient]
public class FormRecoveryTask : FormRecoveryTaskBase
{
    // Default encoder unicode-escapes "&", which the docbuilder script engine doesn't decode back inside
    // string literals, breaking every query parameter after the first in an embedded URL.
    private static readonly JsonSerializerOptions _scriptStringOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly TimeSpan _sourceUrlSignatureLifetime = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private string? _baseUri;
    private int _tenantId;
    private Guid _userId;
    private int _roomId;

    private List<FormRecoveryFormResultDto> _forms = [];

    // Field-key layout of each version of an original form, resolved once per form during a run and reused
    // across all its orphans to route each into the report of the version whose field set it matches.
    private readonly Dictionary<int, List<(int Version, HashSet<string> Keys)>> _versionKeySetsByForm = [];

    public List<FormRecoveryFormResultDto>? FinalForms
    {
        get => IsCompleted ? _forms : null;
        set => _forms = value ?? [];
    }

    public FormRecoveryTask() { }

    public FormRecoveryTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(string? baseUri, int tenantId, Guid userId, int roomId, string taskId)
    {
        _baseUri = baseUri;
        _tenantId = tenantId;
        _userId = userId;
        _roomId = roomId;

        Id = taskId;
        Status = DistributedTaskStatus.Created;
    }

    protected override async Task DoJob()
    {
        if (_serviceScopeFactory is null)
        {
            throw new InvalidOperationException($"{nameof(FormRecoveryTask)} cannot execute: was deserialized from cache without a DI scope.");
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FormRecoveryTask>>();

        try
        {
            if (!string.IsNullOrEmpty(_baseUri))
            {
                var commonLinkUtility = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>();
                commonLinkUtility.ServerUri = _baseUri;
            }

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<int>();
            var folderDao = daoFactory.GetFolderDao<int>();
            var factoryIndexerForm = scope.ServiceProvider.GetRequiredService<FactoryIndexerForm>();
            var formFillingReportCreator = scope.ServiceProvider.GetRequiredService<FormFillingReportCreator>();
            var documentBuilderTask = scope.ServiceProvider.GetRequiredService<DocumentBuilderTask>();
            var documentServiceConnector = scope.ServiceProvider.GetRequiredService<DocumentServiceConnector>();
            var documentServiceHelper = scope.ServiceProvider.GetRequiredService<DocumentServiceHelper>();
            var pathProvider = scope.ServiceProvider.GetRequiredService<PathProvider>();
            var globalStore = scope.ServiceProvider.GetRequiredService<GlobalStore>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var distributedLockProvider = scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
            var tenantUtil = scope.ServiceProvider.GetRequiredService<TenantUtil>();

            logger.InfoRecoveryStarted(_roomId);

            var room = await folderDao.GetFolderAsync(_roomId);
            if (room is not { FolderType: FolderType.FillingFormsRoom })
            {
                throw new InvalidOperationException("The specified room is not a filling forms room.");
            }

            var subFolders = await folderDao.GetFoldersAsync(_roomId).ToListAsync();

            var templatesByResultsFolder = await BuildTemplateMapAsync(fileDao, subFolders);
            var pendingRepairs = await FindOrphanedFormsAsync(fileDao, folderDao, factoryIndexerForm, subFolders, logger);

            var total = pendingRepairs.Count;
            if (total == 0)
            {
                logger.InfoRecoveryNothingToRepair(_roomId);
            }
            else
            {
                logger.InfoRecoveryOrphansFound(_roomId, total);
            }

            var processed = 0;

            // Rebuilding the xlsx rebuilds a form's whole report history, so it runs once per form after
            // the loop rather than once per repaired file.
            var touchedForms = new HashSet<int>();

            foreach (var (doneFolder, orphan) in pendingRepairs)
            {
                CancellationToken.ThrowIfCancellationRequested();

                _forms.Add(await RepairFormAsync(
                    orphan, doneFolder, room, templatesByResultsFolder, touchedForms,
                    fileDao, formFillingReportCreator, documentBuilderTask, documentServiceConnector,
                    pathProvider, documentServiceHelper, globalStore, httpClientFactory, distributedLockProvider, tenantUtil, logger));

                processed++;
                Percentage = processed * 100.0 / total;

                if (processed % 5 == 0 || processed == total)
                {
                    await PublishChanges();
                }
            }

            if (room.SettingsSaveFormAsXLSX)
            {
                foreach (var touchedFormId in touchedForms)
                {
                    try
                    {
                        await RebuildReportHistoryAsync(scope.ServiceProvider, _roomId, touchedFormId, logger);
                    }
                    catch (Exception e)
                    {
                        logger.ErrorRecoveryXlsxRebuildFailed(e, touchedFormId, _roomId);
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted; // TODO: rename to Failed when the enum typo is fixed
        }
        finally
        {
            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }

    /// <summary>
    /// Maps a form template's own "Complete" subfolder ID back to that template's ID, so a completed form
    /// PDF that lacks its own <see cref="FormFillingProperties{T}"/> can still be resolved to the right
    /// original form purely by where it physically sits.
    /// </summary>
    private static async Task<Dictionary<int, int>> BuildTemplateMapAsync(
        IFileDao<int> fileDao, List<Folder<int>> subFolders)
    {
        var excludedFolderTypes = new HashSet<FolderType>
        {
            FolderType.InProcessFormFolder,
            FolderType.FormFillingFolderInProgress,
            FolderType.ReadyFormFolder,
            FolderType.FormFillingFolderDone
        };

        var templateCandidateFolderIds = subFolders
            .Where(f => !excludedFolderTypes.Contains(f.FolderType))
            .Select(f => f.Id);

        var templateFiles = new List<File<int>>();
        foreach (var folderId in templateCandidateFolderIds)
        {
            await foreach (var templateFile in fileDao.GetFilesAsync(folderId, null, FilterType.PdfForm, false, Guid.Empty, string.Empty, null, false))
            {
                templateFiles.Add(templateFile);
            }
        }

        var propertiesByFileId = await fileDao.GetPropertiesAsync(templateFiles.Select(f => f.Id));

        var templatesByResultsFolder = new Dictionary<int, int>();

        foreach (var templateFile in templateFiles)
        {
            var formFilling = propertiesByFileId.GetValueOrDefault(templateFile.Id)?.FormFilling;

            if (formFilling != null && formFilling.OriginalFormId == templateFile.Id && formFilling.ResultsFolderId != 0)
            {
                templatesByResultsFolder[formFilling.ResultsFolderId] = formFilling.OriginalFormId;
            }
        }

        return templatesByResultsFolder;
    }

    /// <summary>
    /// Finds completed form PDFs in every "Complete" subfolder of the room that have no matching
    /// submission record in the search index yet.
    /// </summary>
    private async Task<List<(Folder<int> DoneFolder, File<int> File)>> FindOrphanedFormsAsync(
        IFileDao<int> fileDao, IFolderDao<int> folderDao, FactoryIndexerForm factoryIndexerForm, List<Folder<int>> subFolders, ILogger<FormRecoveryTask> logger)
    {
        var pendingRepairs = new List<(Folder<int> DoneFolder, File<int> File)>();

        // FormFillingFolderDone folders sit one level under the room's ReadyFormFolder, not directly under
        // the room, so `subFolders` (the room's direct children) never contains them directly.
        var readyFormFolder = subFolders.FirstOrDefault(f => f.FolderType == FolderType.ReadyFormFolder);
        if (readyFormFolder == null)
        {
            return pendingRepairs;
        }

        var doneFolders = await folderDao.GetFoldersAsync(readyFormFolder.Id).ToListAsync();
        if (doneFolders.Count == 0)
        {
            return pendingRepairs;
        }

        // Refresh() is a global, comparatively expensive index-flush, so it only needs to run once here,
        // not once per folder.
        factoryIndexerForm.Refresh();

        foreach (var doneFolder in doneFolders)
        {
            // Orphans never went through FileDao.SaveFileAsync's ONLYOFFICE-signature sniffing, so they may
            // lack the FilterType.PdfForm/Pdf category stamp — filter by extension instead of that category.
            var completedFiles = (await fileDao.GetFilesAsync(doneFolder.Id, null, FilterType.FilesOnly, false, Guid.Empty, string.Empty, null, false).ToListAsync())
                .Where(f => string.Equals(FileUtility.GetFileExtension(f.Title), ".pdf", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (completedFiles.Count == 0)
            {
                continue;
            }

            var (indexSuccess, indexedItems) = await factoryIndexerForm.TrySelectAsync(r => r.Where(s => s.ParentId, doneFolder.Id));

            if (!indexSuccess)
            {
                // TrySelectAsync returns the same (success, empty) shape on failure as on "nothing indexed
                // yet" — treating that as "all orphaned" would reprocess already-indexed, correct forms.
                logger.WarnRecoveryIndexQueryFailed(doneFolder.Id);
                continue;
            }

            var indexedIds = indexedItems.Select(i => i.Id).ToHashSet();

            pendingRepairs.AddRange(completedFiles
                .Where(f => !indexedIds.Contains(f.Id))
                .Select(f => (doneFolder, f)));
        }

        return pendingRepairs;
    }

    private async Task<FormRecoveryFormResultDto> RepairFormAsync(
        File<int> orphan,
        Folder<int> doneFolder,
        Folder<int> room,
        Dictionary<int, int> templatesByResultsFolder,
        HashSet<int> touchedForms,
        IFileDao<int> fileDao,
        FormFillingReportCreator formFillingReportCreator,
        DocumentBuilderTask documentBuilderTask,
        DocumentServiceConnector documentServiceConnector,
        PathProvider pathProvider,
        DocumentServiceHelper documentServiceHelper,
        GlobalStore globalStore,
        IHttpClientFactory httpClientFactory,
        IDistributedLockProvider distributedLockProvider,
        TenantUtil tenantUtil,
        ILogger<FormRecoveryTask> logger)
    {
        try
        {
            var existing = (await fileDao.GetProperties(orphan.Id))?.FormFilling;

            // The room actually being recovered, not a possibly-stale RoomId left over on the orphan's own
            // properties (e.g. from a backup restore or a cross-room move).
            var roomId = _roomId;

            int originalFormId;

            if (existing is { OriginalFormId: not 0 })
            {
                originalFormId = existing.OriginalFormId;
            }
            else if (templatesByResultsFolder.TryGetValue(doneFolder.Id, out var resolvedFormId))
            {
                originalFormId = resolvedFormId;
            }
            else
            {
                return new FormRecoveryFormResultDto
                {
                    Id = orphan.Id,
                    Title = orphan.Title,
                    Success = false,
                    Error = "The source form template could not be resolved for this file."
                };
            }

            var originalForm = await fileDao.GetFileAsync(originalFormId);
            if (originalForm == null)
            {
                return new FormRecoveryFormResultDto
                {
                    Id = orphan.Id,
                    Title = orphan.Title,
                    Success = false,
                    Error = "The source form template no longer exists."
                };
            }

            // Completed forms don't reliably record which template version they were filled against, so
            // derive it from the actual field layout: read the form's field keys out of the PDF and route
            // it into the report of the version whose key set matches. This keeps each version's report to
            // a single, consistent set of columns and supports old forms that predate version tracking.
            var orphanFormsDataJson = await ExtractFormFieldsJsonAsync(orphan, lastVersion: true, documentServiceHelper, documentBuilderTask, documentServiceConnector, pathProvider, httpClientFactory);
            var orphanKeys = ParseFieldKeys(orphanFormsDataJson);

            var versionKeySets = await GetVersionKeySetsAsync(
                originalFormId, originalForm, fileDao, formFillingReportCreator,
                documentServiceHelper, documentBuilderTask, documentServiceConnector, pathProvider, httpClientFactory, logger);

            // On the rare tie (a template revision that didn't change the field set) prefer the newest
            // matching version. When nothing matches (a layout absent from every template version) fall
            // back to the current version so the data isn't lost, and flag it.
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
                logger.WarnRecoveryVersionUnmatched(orphan.Id, originalFormId, _roomId);
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
                // Reuse the number the form was given when it was originally filled (kept in its own
                // properties and reflected in its title), so the report's "form number" matches the
                // completed file. Only mint a fresh sequential number when it's unknown (e.g. a backup
                // restore that lost the properties).
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
                        // The report timestamp should reflect when the form was actually filled, not this
                        // recovery run. File timestamps are materialized in tenant-local time, so convert
                        // back to UTC to match the DateTime.UtcNow a normal submission stores.
                        filledOn: tenantUtil.DateTimeToUtc(orphan.ModifiedOn));

                    touchedForms.Add(originalFormId);
                }
                catch
                {
                    // Undo the counter bump above (only when we minted a new number) unless something else
                    // already claimed a later number, otherwise a retry would skip a number permanently.
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
                // No-op if UpdateFormFillingReport already read (and thereby self-deleted) the temp file;
                // cleans it up if a failure above prevented that from ever happening.
                await CleanupTempFormsDataFileAsync(formsDataUrl, globalStore, logger);
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

            return new FormRecoveryFormResultDto { Id = orphan.Id, Title = orphan.Title, Success = true };
        }
        catch (Exception ex)
        {
            logger.ErrorRecoveryFormFailed(ex, orphan.Id, _roomId);
            return new FormRecoveryFormResultDto { Id = orphan.Id, Title = orphan.Title, Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Rebuilds the whole version history of a form's results xlsx from the search index: one file version
    /// per form version that has submissions, oldest first, so each report shows exactly the submissions of
    /// its own version. This mirrors how the online flow accretes the file over time — which a form recovery
    /// run (indexing many versions at once) can't reproduce by triggering the normal per-version report.
    /// </summary>
    private async Task RebuildReportHistoryAsync(IServiceProvider serviceProvider, int roomId, int formId, ILogger<FormRecoveryTask> logger)
    {
        var daoFactory = serviceProvider.GetRequiredService<IDaoFactory>();
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

        var formFillingReportCreator = serviceProvider.GetRequiredService<FormFillingReportCreator>();
        var documentBuilderTask = serviceProvider.GetRequiredService<DocumentBuilderTask>();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource("FormFillingReport.docbuilder")
            ?? throw new InvalidOperationException("FormFillingReport.docbuilder template not found.");

        // The results file may already carry partial versions built online before the outage. The index
        // (now including the recovered rows) is the source of truth for the full history, so collapse the
        // file back to a single version and rebuild every version from scratch below.
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

            var reportData = await FormFillingReportTask.GetFormFillingReportData(serviceProvider, _userId, roomId, formId, version);
            var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");
            var versionScript = script
                .Replace("${tempFileName}", tempFileName)
                .Replace("${inputData}", JsonSerializer.Serialize(reportData));

            var xlsxUrl = await documentBuilderTask.BuildFileAsync(new DocumentBuilderInputData(versionScript, tempFileName, ""), CancellationToken);

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
            var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000
            using var response = await httpClient.GetAsync(xlsxUrl, CancellationToken);
            response.EnsureSuccessStatusCode();

            using var buffer = new MemoryStream();
            await response.Content.CopyToAsync(buffer, CancellationToken);
            buffer.Position = 0;
            resultFile.ContentLength = buffer.Length;

            // First version replaces the single remaining file version; each later version is appended as a
            // new file version, so the file's version history mirrors the form's versions.
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

        await serviceProvider.GetRequiredService<SocketManager>().UpdateFileAsync(resultFile);
    }

    /// <summary>
    /// Best-effort delete of the temp JSON uploaded for a repaired orphan, for when it never got read
    /// (and thereby self-deleted). Safe to call even if it was already consumed.
    /// </summary>
    private static async Task CleanupTempFormsDataFileAsync(string formsDataUrl, GlobalStore globalStore, ILogger<FormRecoveryTask> logger)
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
    /// Resolves the field key set of each version (1..current) of the original form, used to route each
    /// orphan into the report of the version whose field layout it matches. Prefers the layout already
    /// indexed in the search metadata; for versions never indexed (only ever filled while the index was
    /// down) it reads the fields straight from that version of the template PDF. Cached per form for the run.
    /// </summary>
    private async Task<List<(int Version, HashSet<string> Keys)>> GetVersionKeySetsAsync(
        int originalFormId,
        File<int> currentForm,
        IFileDao<int> fileDao,
        FormFillingReportCreator formFillingReportCreator,
        DocumentServiceHelper documentServiceHelper,
        DocumentBuilderTask documentBuilderTask,
        DocumentServiceConnector documentServiceConnector,
        PathProvider pathProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<FormRecoveryTask> logger)
    {
        if (_versionKeySetsByForm.TryGetValue(originalFormId, out var cached))
        {
            return cached;
        }

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
                        var templateJson = await ExtractFormFieldsJsonAsync(templateAtVersion, lastVersion: false,
                            documentServiceHelper, documentBuilderTask, documentServiceConnector, pathProvider, httpClientFactory);
                        keys = ParseFieldKeys(templateJson);
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

        _versionKeySetsByForm[originalFormId] = result;
        return result;
    }

    /// <summary>
    /// The set of field keys present in the given "{ formsdata: [...] }" payload, matching how the report
    /// lays out its columns (picture/signature fields excluded, compared case-insensitively).
    /// </summary>
    private static HashSet<string> ParseFieldKeys(string formsDataJson)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(formsDataJson);
        if (document.RootElement.TryGetProperty("formsdata", out var formsArray) && formsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var form in formsArray.EnumerateArray())
            {
                var key = form.TryGetProperty("key", out var keyProp) ? keyProp.GetString() : null;
                var type = form.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

                if (!string.IsNullOrEmpty(key) && type != "picture" && type != "signature")
                {
                    keys.Add(key);
                }
            }
        }

        return keys;
    }

    /// <summary>
    /// Runs the ExtractFormFieldsData.docbuilder script against a form file's stream URL and pulls the
    /// "{ formsdata: [...] }" JSON the script embeds in its (otherwise irrelevant) text output. The shape
    /// matches Document Server's normal formsDataUrl submit callback, so it can feed both the field-key
    /// matching here and <see cref="FormFillingReportCreator.UpdateFormFillingReport{T}"/> unchanged.
    /// </summary>
    private async Task<string> ExtractFormFieldsJsonAsync(
        File<int> file,
        bool lastVersion,
        DocumentServiceHelper documentServiceHelper,
        DocumentBuilderTask documentBuilderTask,
        DocumentServiceConnector documentServiceConnector,
        PathProvider pathProvider,
        IHttpClientFactory httpClientFactory)
    {
        var sourceUrl = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file, lastVersion));

        // builder.OpenFile(url) can't attach custom headers, so the signature travels as a query parameter
        // instead — bound to this file and short-lived (JsonWebToken.Decode enforces "exp"), since a query
        // string routinely ends up in access logs unlike a header.
        var signatureToken = documentServiceHelper.GetSignature(new
        {
            fileId = file.Id,
            exp = DateTimeOffset.UtcNow.Add(_sourceUrlSignatureLifetime).ToUnixTimeSeconds()
        });

        if (!string.IsNullOrEmpty(signatureToken))
        {
            sourceUrl = FilesLinkUtility.AddQueryString(sourceUrl, new Dictionary<string, string>
            {
                { FilesLinkUtility.SignatureQueryKey, signatureToken }
            });
        }

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource("ExtractFormFieldsData.docbuilder")
            ?? throw new InvalidOperationException("ExtractFormFieldsData.docbuilder template not found.");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".txt");

        // The docbuilder engine only ever keeps one document "active" at a time — closing the opened PDF
        // to create a second document to hold the result drops every variable read from the first (see the
        // script itself). So the script embeds the result directly into the opened PDF's own text output,
        // wrapped in unique markers, and this pulls the JSON back out from between them.
        var markerStart = $"@@FORMDATA_START_{Guid.NewGuid():N}@@";
        var markerEnd = $"@@FORMDATA_END_{Guid.NewGuid():N}@@";

        script = script
            .Replace("${sourceFileUrl}", JsonSerializer.Serialize(sourceUrl, _scriptStringOptions))
            .Replace("${tempFileName}", tempFileName)
            .Replace("${resultMarkerStart}", markerStart)
            .Replace("${resultMarkerEnd}", markerEnd);

        var inputData = new DocumentBuilderInputData(script, tempFileName, "");
        var resultTextUrl = await documentBuilderTask.BuildFileAsync(inputData, CancellationToken);

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.GetAsync(resultTextUrl, CancellationToken);
        response.EnsureSuccessStatusCode();
        var resultText = await response.Content.ReadAsStringAsync(CancellationToken);

        var startIndex = resultText.IndexOf(markerStart, StringComparison.Ordinal);
        var endIndex = resultText.IndexOf(markerEnd, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex)
        {
            throw new InvalidOperationException("The form data markers were not found in the DocBuilder script output.");
        }

        startIndex += markerStart.Length;

        return resultText[startIndex..endIndex];
    }
}
