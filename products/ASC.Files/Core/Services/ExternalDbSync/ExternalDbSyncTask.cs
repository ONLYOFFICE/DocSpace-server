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

namespace ASC.Files.Core.Services.ExternalDbSync;

#nullable enable

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(ExternalDbSyncTask), "ExternalDbSyncTask")]
public abstract class ExternalDbSyncTaskBase : DistributedTaskProgress { }

[Transient]
public class ExternalDbSyncTask : ExternalDbSyncTaskBase
{
    private readonly IServiceScopeFactory? _serviceScopeFactory;
    private int _tenantId;
    private Guid _userId;
    private int _roomId;

    private List<ExternalDbSyncFormResultDto> _forms = [];

    public List<ExternalDbSyncFormResultDto>? FinalForms
    {
        get => IsCompleted ? _forms : null;
        set => _forms = value ?? [];
    }

    public ExternalDbSyncTask() { }

    public ExternalDbSyncTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(int tenantId, Guid userId, int roomId, string taskId)
    {
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
            throw new InvalidOperationException($"{nameof(ExternalDbSyncTask)} cannot execute: was deserialized from cache without a DI scope.");
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ExternalDbSyncTask>>();

        try
        {
            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            await tenantManager.SetCurrentTenantAsync(_tenantId);

            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            await securityContext.AuthenticateMeWithoutCookieAsync(_userId);

            var factoryIndexerFormMetadata = scope.ServiceProvider.GetRequiredService<FactoryIndexerFormMetadata>();
            var formFillingReportCreator = scope.ServiceProvider.GetRequiredService<FormFillingReportCreator>();
            var daoFactory = scope.ServiceProvider.GetRequiredService<IDaoFactory>();
            var fileDao = daoFactory.GetFileDao<int>();

            logger.InfoSyncStarted(_roomId);

            var folderDao = daoFactory.GetFolderDao<int>();

            factoryIndexerFormMetadata.Refresh();
            var (success, forms) = await factoryIndexerFormMetadata.TrySelectAsync(r =>
                r.Where(s => s.RoomId, _roomId)
                 .Limit(0, BaseIndexer<DbFormsMetadataSearch>.QueryLimit));

            if (!success)
            {
                throw new InvalidOperationException("Failed to query form metadata from search index.");
            }

            if (forms.Count == 0)
            {
                logger.WarnNoFormsFound(_roomId);
            }
            else if (forms.Count == BaseIndexer<DbFormsMetadataSearch>.QueryLimit)
            {
                logger.WarnQueryLimitReached(_roomId, BaseIndexer<DbFormsMetadataSearch>.QueryLimit);
            }

            var originalForms = forms.Count > 0
                ? await fileDao.GetFilesAsync(forms.Select(f => f.OriginalFormId).Distinct()).ToDictionaryAsync(f => f.Id)
                : new Dictionary<int, File<int>>();

            var total = forms.Count;
            var processed = 0;

            foreach (var form in forms)
            {
                originalForms.TryGetValue(form.OriginalFormId, out var originalForm);
                var title = originalForm?.Title ?? string.Empty;

                try
                {
                    if (originalForm != null)
                    {
                        await formFillingReportCreator.MigrateFormVersionAsync(_roomId, form.OriginalFormId, originalForm.Version);
                    }

                    var version = originalForm?.Version ?? form.OriginalFormVersion;
                    var synced = await formFillingReportCreator.ExportMissingFromOpenSearchAsync(form.OriginalFormId, version, _roomId);

                    if (synced)
                    {
                        var tableName = FormFillingReportCreator.GetTableName(form.OriginalFormId, version);
                        var properties = await fileDao.GetProperties(form.OriginalFormId);
                        if (properties?.FormFilling != null)
                        {
                            properties.FormFilling.ExternalDbTableName = tableName;
                            await fileDao.SaveProperties(form.OriginalFormId, properties);
                        }
                    }

                    _forms.Add(new ExternalDbSyncFormResultDto { Id = form.OriginalFormId, Title = title, Success = synced, Error = synced ? null : FilesCommonResource.ErrorMessage_ExternalDbSyncFailed });
                }
                catch (Exception ex)
                {
                    logger.ErrorSyncFormFailed(ex, form.OriginalFormId, _roomId);
                    _forms.Add(new ExternalDbSyncFormResultDto { Id = form.OriginalFormId, Title = title, Success = false, Error = ex.Message });
                }

                processed++;
                Percentage = processed * 100.0 / total;
                logger.InfoSyncProgress(_roomId, processed, total);

                if (processed % 10 == 0 || processed == total)
                {
                    await PublishChanges();
                }
            }

            var excludedFolderTypes = new HashSet<FolderType>
            {
                FolderType.InProcessFormFolder,
                FolderType.FormFillingFolderInProgress,
                FolderType.ReadyFormFolder,
                FolderType.FormFillingFolderDone
            };

            var subFolders = await folderDao.GetFoldersAsync(_roomId).ToListAsync();

            var formFolderIds = subFolders
                .Where(f => !excludedFolderTypes.Contains(f.FolderType))
                .Select(f => f.Id)
                .Prepend(_roomId)
                .ToList();

            var processedFormIds = _forms.Select(f => f.Id).ToHashSet();

            foreach (var folderId in formFolderIds)
            {
                await foreach (var file in fileDao.GetFilesAsync(folderId, null, FilterType.PdfForm, false, Guid.Empty, string.Empty, null, false))
                {
                    if (!processedFormIds.Contains(file.Id))
                    {
                        _forms.Add(new ExternalDbSyncFormResultDto { Id = file.Id, Title = file.Title, Success = false, Error = FilesCommonResource.ErrorMessage_ExternalDbNotIndexed });
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
}
