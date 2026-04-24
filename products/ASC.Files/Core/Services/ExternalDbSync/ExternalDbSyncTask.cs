// (c) Copyright Ascensio System SIA 2009-2026
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

namespace ASC.Files.Core.Services.ExternalDbSync;

[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(ExternalDbSyncTask), "ExternalDbSyncTask")]
public abstract class ExternalDbSyncTaskBase : DistributedTaskProgress { }

[Transient]
public class ExternalDbSyncTask : ExternalDbSyncTaskBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private int _tenantId;
    private Guid _userId;
    private int _roomId;

    public List<ExternalDbSyncFormResultDto> Forms { get; set; } = [];

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

            if (!success || forms.Count == 0)
            {
                logger.WarnNoFormsFound(_roomId);
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

                    Forms.Add(new ExternalDbSyncFormResultDto { Id = form.OriginalFormId, Title = title, Success = synced, Error = synced ? null : "Sync failed" });
                }
                catch (Exception ex)
                {
                    logger.ErrorSyncFormFailed(ex, form.OriginalFormId, _roomId);
                    Forms.Add(new ExternalDbSyncFormResultDto { Id = form.OriginalFormId, Title = title, Success = false, Error = ex.Message });
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

            var processedFormIds = Forms.Select(f => f.Id).ToHashSet();

            foreach (var folderId in formFolderIds)
            {
                await foreach (var file in fileDao.GetFilesAsync(folderId, null, FilterType.PdfForm, false, Guid.Empty, string.Empty, null, false))
                {
                    if (!processedFormIds.Contains(file.Id))
                    {
                        Forms.Add(new ExternalDbSyncFormResultDto { Id = file.Id, Title = file.Title, Success = false, Error = "Not indexed" });
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
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
