// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Data.Backup.Services;

[Singleton]
public class BackupWorker(
    IDistributedTaskQueueFactory queueFactory,
    IServiceProvider serviceProvider,
    TempPath tempPath,
    IDistributedLockProvider distributedLockProvider)
{
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "backup";
    public const string LockKey = $"lock_{CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME}";

    public string TempFolder { get; } = Path.Combine(tempPath.GetTempPath(), "backup");

    private DistributedTaskQueue _progressQueue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME, 60 * 60 * 24); // 1 day
    private int _limit;
    private string _upgradesPath;
    
    public void Start(BackupSettings settings)
    {
        if (!Directory.Exists(TempFolder))
        {
            Directory.CreateDirectory(TempFolder);
        }

        _limit = settings.Limit;
        _upgradesPath = settings.UpgradesPath;
    }

    public async Task StopAsync()
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            if (_progressQueue == null)
            {
                return;
            }

            var tasks = _progressQueue.GetAllTasks(DistributedTaskQueue.INSTANCE_ID);

            foreach (var t in tasks)
            {
                _progressQueue.DequeueTask(t.Id);
            }

            _progressQueue = null;
        }
    }

    public async Task<BackupProgress> StartBackupAsync(StartBackupRequest request, bool enqueueTask = true, string taskId = null)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var item = _progressQueue.GetAllTasks<BackupProgressItem>().FirstOrDefault(t => t.TenantId == request.TenantId && t.BackupProgressItemType == BackupProgressItemType.Backup);

            if (item is { IsCompleted: true })
            {
                _progressQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null || (enqueueTask && item.Id == taskId && item.Status == DistributedTaskStatus.Created))
            {

                item = serviceProvider.GetService<BackupProgressItem>();

                item.Init(request, false, TempFolder, _limit);

                if (!string.IsNullOrEmpty(taskId))
                {
                    item.Id = taskId;
                }

                if (enqueueTask)
                {
                    _progressQueue.EnqueueTask(item);
                }
                else
                {
                    _progressQueue.PublishTask(item);
                }
            }

            item.PublishChanges();

            return ToBackupProgress(item);
        }
    }

    public async Task StartScheduledBackupAsync(BackupSchedule schedule)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var item = _progressQueue.GetAllTasks<BackupProgressItem>().FirstOrDefault(t => t.TenantId == schedule.TenantId && t.BackupProgressItemType == BackupProgressItemType.Backup);

            if (item is { IsCompleted: true })
            {
                _progressQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null)
            {
                item = serviceProvider.GetService<BackupProgressItem>();

                item.Init(schedule, true, TempFolder, _limit);

                _progressQueue.EnqueueTask(item);
            }
        }
    }

    public async Task<BackupProgress> GetBackupProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            return ToBackupProgress(_progressQueue.GetAllTasks<BackupProgressItem>().FirstOrDefault(t => t.TenantId == tenantId && t.BackupProgressItemType == BackupProgressItemType.Backup));
        }
    }

    public async Task<BackupProgress> GetTransferProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            return ToBackupProgress(_progressQueue.GetAllTasks<TransferProgressItem>().FirstOrDefault(t => t.TenantId == tenantId && t.BackupProgressItemType == BackupProgressItemType.Transfer));
        }
    }

    public async Task<BackupProgress> GetRestoreProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            return ToBackupProgress(_progressQueue.GetAllTasks<RestoreProgressItem>().FirstOrDefault(t => t.TenantId == tenantId && t.BackupProgressItemType == BackupProgressItemType.Restore));
        }
    }

    public async Task ResetBackupErrorAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var progress = _progressQueue.GetAllTasks<BackupProgressItem>().FirstOrDefault(t => t.TenantId == tenantId);
            if (progress != null)
            {
                progress.Exception = null;
            }
        }
    }

    public async Task ResetRestoreErrorAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var progress = _progressQueue.GetAllTasks<RestoreProgressItem>().FirstOrDefault(t => t.TenantId == tenantId);
            if (progress != null)
            {
                progress.Exception = null;
            }
        }
    }

    public async Task<BackupProgress> StartRestoreAsync(StartRestoreRequest request)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var item = _progressQueue.GetAllTasks<RestoreProgressItem>().FirstOrDefault(t => t.TenantId == request.TenantId);
            if (item is { IsCompleted: true })
            {
                _progressQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null)
            {
                item = serviceProvider.GetService<RestoreProgressItem>();
                item.Init(request, TempFolder, _upgradesPath);

                _progressQueue.EnqueueTask(item);
            }
            return ToBackupProgress(item);
        }
    }

    public async Task<BackupProgress> StartTransferAsync(int tenantId, string targetRegion, bool notify)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey, TimeSpan.FromMinutes(1)))
        {
            var item = _progressQueue.GetAllTasks<TransferProgressItem>().FirstOrDefault(t => t.TenantId == tenantId);
            if (item is { IsCompleted: true })
            {
                _progressQueue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null)
            {
                item = serviceProvider.GetService<TransferProgressItem>();
                item.Init(targetRegion, tenantId, TempFolder, _limit, notify);

                _progressQueue.EnqueueTask(item);
            }

            return ToBackupProgress(item);
        }
    }

    internal static string GetBackupHashSHA(string path)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = File.OpenRead(path);
        fileStream.Position = 0;
        var hash = sha256.ComputeHash(fileStream);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    internal static string GetBackupHashMD5(string path, long chunkSize)
    {
        using var md5 = MD5.Create();
        using var fileStream = File.OpenRead(path);
        var multipartSplitCount = 0;
        var splitCount = fileStream.Length / chunkSize;
        var mod = (int)(fileStream.Length - chunkSize * splitCount);
        IEnumerable<byte> concatHash = new byte[] { };

        for (var i = 0; i < splitCount; i++)
        {
            var offset = i == 0 ? 0 : chunkSize * i;
            var chunk = GetChunk(fileStream, offset, (int)chunkSize);
            var hash = md5.ComputeHash(chunk);
            concatHash = concatHash.Concat(hash);
            multipartSplitCount++;
        }
        if (mod != 0)
        {
            var chunk = GetChunk(fileStream, chunkSize * splitCount, mod);
            var hash = md5.ComputeHash(chunk);
            concatHash = concatHash.Concat(hash);
            multipartSplitCount++;
        }
        var multipartHash = BitConverter.ToString(md5.ComputeHash(concatHash.ToArray())).Replace("-", string.Empty);
        return multipartHash + "-" + multipartSplitCount;
    }

    private static byte[] GetChunk(Stream sourceStream, long offset, int count)
    {
        var buffer = new byte[count];
        sourceStream.Position = offset;
        _ = sourceStream.Read(buffer, 0, count);
        return buffer;
    }

    private BackupProgress ToBackupProgress(BaseBackupProgressItem progressItem)
    {
        if (progressItem == null)
        {
            return null;
        }
        var progress = new BackupProgress
        {
            IsCompleted = progressItem.IsCompleted,
            Progress = (int)progressItem.Percentage,
            Error = progressItem.Exception != null ? progressItem.Exception.Message : "",
            TenantId = progressItem.TenantId,
            BackupProgressEnum = progressItem.BackupProgressItemType.Convert(),
            TaskId = progressItem.Id
        };

        if (progressItem.BackupProgressItemType is BackupProgressItemType.Backup or BackupProgressItemType.Transfer && progressItem.Link != null)
        {
            progress.Link = progressItem.Link;
        }

        return progress;
    }

    public bool IsInstanceTooBusy()
    {
        var instanceTasks = _progressQueue.GetAllTasks(DistributedTaskQueue.INSTANCE_ID);

        return _progressQueue.MaxThreadsCount < instanceTasks.Count();
        }
}