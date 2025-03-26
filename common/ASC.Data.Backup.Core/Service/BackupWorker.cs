// (c) Copyright Ascensio System SIA 2009-2025
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
    public const string LockKey = "lock_backup";

    public string TempFolder { get; } = Path.Combine(tempPath.GetTempPath(), "backup");

    private DistributedTaskQueue<BackupProgressItem> _backupProgressQueue = queueFactory.CreateQueue<BackupProgressItem>(60 * 60 * 24); // 1 day
    private DistributedTaskQueue<RestoreProgressItem> _restoreProgressQueue = queueFactory.CreateQueue<RestoreProgressItem>(60 * 60 * 24); // 1 day
    private DistributedTaskQueue<TransferProgressItem> _transferProgressQueue = queueFactory.CreateQueue<TransferProgressItem>(60 * 60 * 24); // 1 day
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
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            if (_backupProgressQueue != null)
            {
                var tasks = await _backupProgressQueue.GetAllTasks(DistributedTaskQueue<BackupProgressItem>.INSTANCE_ID);

                foreach (var t in tasks)
                {
                    await _backupProgressQueue.DequeueTask(t.Id);
                }

                _backupProgressQueue = null;
            }
            
            if (_restoreProgressQueue != null)
            {
                var tasks = await _restoreProgressQueue.GetAllTasks(DistributedTaskQueue<BackupProgressItem>.INSTANCE_ID);

                foreach (var t in tasks)
                {
                    await _restoreProgressQueue.DequeueTask(t.Id);
                }

                _restoreProgressQueue = null;
            }
            
            if (_transferProgressQueue != null)
            {
                var tasks = await _transferProgressQueue.GetAllTasks(DistributedTaskQueue<BackupProgressItem>.INSTANCE_ID);

                foreach (var t in tasks)
                {
                    await _transferProgressQueue.DequeueTask(t.Id);
                }

                _transferProgressQueue = null;
            }
        }
    }

    public async Task<BackupProgress> StartBackupAsync(StartBackupRequest request, bool enqueueTask = true, string taskId = null)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _backupProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == request.TenantId);

            if (item is { IsCompleted: true })
            {
                await _backupProgressQueue.DequeueTask(item.Id);
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
                    await _backupProgressQueue.EnqueueTask(item);
                }
                else
                {
                    await _backupProgressQueue.PublishTask(item);
                }
            }

            return ToBackupProgress(item);
        }
    }

    public async Task StartScheduledBackupAsync(BackupSchedule schedule)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _backupProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == schedule.TenantId);

            if (item is { IsCompleted: true })
            {
                await _backupProgressQueue.DequeueTask(item.Id);
                item = null;
            }
            if (item == null)
            {
                item = serviceProvider.GetService<BackupProgressItem>();

                item.Init(schedule, true, TempFolder, _limit);

                await _backupProgressQueue.EnqueueTask(item);
            }
        }
    }

    public async Task<BackupProgress> GetBackupProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            return ToBackupProgress((await _backupProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId));
        }
    }

    public async Task<BackupProgress> GetTransferProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            return ToBackupProgress((await _transferProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId));
        }
    }

    public async Task<BackupProgress> GetRestoreProgressAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            return ToBackupProgress((await _restoreProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId || t.NewTenantId == tenantId));
        }
    }

    public async Task ResetBackupErrorAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var progress = (await _backupProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
            if (progress != null)
            {
                progress.Exception = null;
            }
        }
    }

    public async Task ResetRestoreErrorAsync(int tenantId)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var progress = (await _restoreProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
            if (progress != null)
            {
                progress.Exception = null;
            }
        }
    }

    public async Task<BackupProgress> StartRestoreAsync(StartRestoreRequest request, bool enqueueTask = true, string taskId = null)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _restoreProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == request.TenantId);
            if (item is { IsCompleted: true })
            {
                await _restoreProgressQueue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null || (enqueueTask && item.Id == taskId && item.Status == DistributedTaskStatus.Created))
            {

                item = serviceProvider.GetService<RestoreProgressItem>();

                item.Init(request, TempFolder, _upgradesPath);

                if (!string.IsNullOrEmpty(taskId))
                {
                    item.Id = taskId;
                }

                if (enqueueTask)
                {
                    await _restoreProgressQueue.EnqueueTask(item);
                }
                else
                {
                    await _restoreProgressQueue.PublishTask(item);
                }
            }
            return ToBackupProgress(item);
        }
    }

    public async Task<BackupProgress> StartTransferAsync(int tenantId, string targetRegion, bool notify)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync(LockKey))
        {
            var item = (await _transferProgressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
            if (item is { IsCompleted: true })
            {
                await _transferProgressQueue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null)
            {
                item = serviceProvider.GetService<TransferProgressItem>();
                item.Init(targetRegion, tenantId, TempFolder, _limit, notify);

                await _transferProgressQueue.EnqueueTask(item);
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

    internal static async Task<string> GetBackupHashMD5Async(string path, long chunkSize)
    {
        await using var fileStream = File.OpenRead(path);
        var multipartSplitCount = 0;
        var splitCount = fileStream.Length / chunkSize;
        var mod = (int)(fileStream.Length - chunkSize * splitCount);
        IEnumerable<byte> concatHash = Array.Empty<byte>();

        for (var i = 0; i < splitCount; i++)
        {
            var offset = i == 0 ? 0 : chunkSize * i;
            var chunk = await GetChunkAsync(fileStream, offset, (int)chunkSize);
            var hash = MD5.HashData(chunk);
            concatHash = concatHash.Concat(hash);
            multipartSplitCount++;
        }
        if (mod != 0)
        {
            var chunk = await GetChunkAsync(fileStream, chunkSize * splitCount, mod);
            var hash = MD5.HashData(chunk);
            concatHash = concatHash.Concat(hash);
            multipartSplitCount++;
        }
        var multipartHash = BitConverter.ToString(MD5.HashData(concatHash.ToArray())).Replace("-", string.Empty);
        return multipartHash + "-" + multipartSplitCount;
    }

    private static async Task<byte[]> GetChunkAsync(FileStream sourceStream, long offset, int count)
    {
        var buffer = new byte[count];
        sourceStream.Position = offset;
        _ = await sourceStream.ReadAsync(buffer.AsMemory(0, count));
        return buffer;
    }

    private BackupProgress ToBackupProgress(BaseBackupProgressItem progressItem)
    {
        if (progressItem == null)
        {
            return null;
        }
        return progressItem.ToBackupProgress();
    }

    public async Task<bool> IsBackupInstanceTooBusy()
    {
        var instanceTasks = await _backupProgressQueue.GetAllTasks(DistributedTaskQueue<BackupProgressItem>.INSTANCE_ID);

        return _backupProgressQueue.MaxThreadsCount < instanceTasks.Count;
    }

    public async Task<bool> IsRestoreInstanceTooBusy()
    {
        var instanceTasks = await _restoreProgressQueue.GetAllTasks(DistributedTaskQueue<BackupProgressItem>.INSTANCE_ID);

        return _restoreProgressQueue.MaxThreadsCount < instanceTasks.Count;
    }
}