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

using System.Text.Json;

namespace ASC.Data.Backup.Services;

[Transient]
public class BackupProgressItem(ILogger<BackupProgressItem> logger,
        IServiceScopeFactory serviceProvider,
        CoreBaseSettings coreBaseSettings,
        NotifyHelper notifyHelper)
    : BaseBackupProgressItem(serviceProvider)
{
    private Dictionary<string, string> _storageParams;
    private string _tempFolder;

    private bool _isScheduled;
    private Guid _userId;
    private BackupStorageType _storageType;
    private string _storageBasePath;
    private int _limit;
    private string _serverBaseUri;
    private bool _dump;

    public void Init(BackupSchedule schedule, bool isScheduled, string tempFolder, int limit)
    {
        Init();
        _userId = Guid.Empty;
        TenantId = schedule.TenantId;
        _storageType = schedule.StorageType;
        _storageBasePath = schedule.StorageBasePath;
        _storageParams = JsonSerializer.Deserialize<Dictionary<string, string>>(schedule.StorageParams);
        _isScheduled = isScheduled;
        _tempFolder = tempFolder;
        _limit = limit;
        _dump = schedule.Dump;
    }

    public void Init(StartBackupRequest request, bool isScheduled, string tempFolder, int limit)
    {
        Init();
        _userId = request.UserId;
        TenantId = request.TenantId;
        _storageType = request.StorageType;
        _storageBasePath = request.StorageBasePath;
        _storageParams = request.StorageParams.ToDictionary(r => r.Key, r => r.Value);
        _isScheduled = isScheduled;
        _tempFolder = tempFolder;
        _limit = limit;
        _dump = request.Dump;
        _serverBaseUri = request.ServerBaseUri;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeProvider.CreateAsyncScope();

        var tenantManager = scope.ServiceProvider.GetService<TenantManager>();
        var backupStorageFactory = scope.ServiceProvider.GetService<BackupStorageFactory>();
        var backupRepository = scope.ServiceProvider.GetService<BackupRepository>();
        var backupPortalTask = scope.ServiceProvider.GetService<BackupPortalTask>();
        var tempStream = scope.ServiceProvider.GetService<TempStream>();

        var dateTime = coreBaseSettings.Standalone ? DateTime.Now : DateTime.UtcNow;
        var tempFile = "";
        var storagePath = "";

        try
        {
            var backupStorage = await backupStorageFactory.GetBackupStorageAsync(_storageType, TenantId, _storageParams);

            var getter = backupStorage as IGetterWriteOperator;
            var name = _dump ? "workspace" : (await tenantManager.GetTenantAsync(TenantId)).Alias;
            var backupName = string.Format("{0}_{1:yyyy-MM-dd_HH-mm-ss}.{2}", name, dateTime, await getter.GetBackupExtensionAsync(_storageBasePath));

            tempFile = CrossPlatform.PathCombine(_tempFolder, backupName);
            storagePath = tempFile;

            var writer = await DataOperatorFactory.GetWriteOperatorAsync(tempStream, _storageBasePath, backupName, _tempFolder, _userId, CancellationToken, getter);

            backupPortalTask.Init(TenantId, tempFile, _limit, writer, _dump);

            backupPortalTask.ProgressChanged = async args =>
            {
                if (CancellationToken.IsCancellationRequested) 
                {
                    return;
                }
                Percentage = 0.9 * args.Progress;
                await PublishChanges();
            };

            await backupPortalTask.RunJob();
            if (CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            string hash;
            if (writer.NeedUpload)
            {
                storagePath = await backupStorage.UploadAsync(_storageBasePath, tempFile, _userId, CancellationToken);
                hash = BackupWorker.GetBackupHashSHA(tempFile);
            }
            else
            {
                storagePath = writer.StoragePath;
                hash = writer.Hash;
            }
            if (CancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            Link = await backupStorage.GetPublicLinkAsync(storagePath);

            await backupRepository.SaveBackupRecordAsync(
                new BackupRecord
                {
                    Id = Guid.Parse(Id),
                    TenantId = TenantId,
                    IsScheduled = _isScheduled,
                    Name = Path.GetFileName(tempFile),
                    StorageType = _storageType,
                    StorageBasePath = _storageBasePath,
                    StoragePath = storagePath,
                    CreatedOn = DateTime.UtcNow,
                    ExpiresOn = _storageType == BackupStorageType.DataStore ? DateTime.UtcNow.AddDays(1) : DateTime.MinValue,
                    StorageParams = JsonSerializer.Serialize(_storageParams),
                    Hash = hash,
                    Removed = false
                });

            Percentage = 100;

            if (_userId != Guid.Empty && !_isScheduled)
            {
                notifyHelper.SetServerBaseUri(_serverBaseUri);

                await notifyHelper.SendAboutBackupCompletedAsync(TenantId, _userId);
            }


            IsCompleted = true;
            await PublishChanges();
        }
        catch (Exception error)
        {
            if (!CancellationToken.IsCancellationRequested) 
            {
                logger.ErrorRunJob(Id, TenantId, tempFile, _storageBasePath, error);
            }
            Exception = error;
            IsCompleted = true;
        }
        finally
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                try
                {
                    await PublishChanges();
                }
                catch (Exception error)
                {
                    logger.ErrorPublish(error);
                }
            }
            try
            {
                if (!(storagePath == tempFile && _storageType == BackupStorageType.Local))
                {
                    File.Delete(tempFile);
                }
            }
            catch (Exception error)
            {
                logger.ErrorCantDeleteFile(error);
            }
        }
    }

    public override object Clone()
    {
        return MemberwiseClone();
    }
}
