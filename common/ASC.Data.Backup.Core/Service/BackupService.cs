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

[Scope]
public class BackupService(
        ILogger<BackupService> logger,
        BackupStorageFactory backupStorageFactory,
        BackupWorker backupWorker,
        BackupRepository backupRepository)
    {
    public async Task<string> StartBackupAsync(StartBackupRequest request, bool enqueueTask = true, string taskId = null)
    {
        var progress = await backupWorker.StartBackupAsync(request, enqueueTask, taskId);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
        return progress.TaskId;
    }

    public async Task DeleteBackupAsync(Guid backupId)
    {
        var backupRecord = await backupRepository.GetBackupRecordAsync(backupId);
        await backupRepository.DeleteBackupRecordAsync(backupRecord.Id);

        var storage = await backupStorageFactory.GetBackupStorageAsync(backupRecord);
        if (storage == null)
        {
            return;
        }

        await storage.DeleteAsync(backupRecord.StoragePath);
    }

    public async Task DeleteAllBackupsAsync(int tenantId)
    {
        foreach (var backupRecord in await backupRepository.GetBackupRecordsByTenantIdAsync(tenantId))
        {
            try
            {
                await backupRepository.DeleteBackupRecordAsync(backupRecord.Id);
                var storage = await backupStorageFactory.GetBackupStorageAsync(backupRecord);
                if (storage == null)
                {
                    continue;
                }

                await storage.DeleteAsync(backupRecord.StoragePath);
            }
            catch (Exception error)
            {
                logger.WarningErrorWhileBackupRecord(error);
            }
        }
    }

    public async Task<List<BackupHistoryRecord>> GetBackupHistoryAsync(int tenantId)
    {
        var backupHistory = new List<BackupHistoryRecord>();
        foreach (var record in await backupRepository.GetBackupRecordsByTenantIdAsync(tenantId))
        {
            var storage = await backupStorageFactory.GetBackupStorageAsync(record);
            if (storage == null)
            {
                continue;
            }

            if (await storage.IsExistsAsync(record.StoragePath))
            {
                backupHistory.Add(new BackupHistoryRecord
                {
                    Id = record.Id,
                    FileName = record.Name,
                    StorageType = record.StorageType,
                    CreatedOn = record.CreatedOn,
                    ExpiresOn = record.ExpiresOn
                });
            }
            else
            {
                await backupRepository.DeleteBackupRecordAsync(record.Id);
            }
        }
        return backupHistory;
    }

    public async Task StartTransferAsync(StartTransferRequest request)
    {
        var progress = await backupWorker.StartTransferAsync(request.TenantId, request.TargetRegion, request.NotifyUsers);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
    }

    public async Task<string> StartRestoreAsync(StartRestoreRequest request, bool enqueueTask = true, string taskId = null)
    {
        if (request.StorageType == BackupStorageType.Local && (string.IsNullOrEmpty(request.FilePathOrId) || !File.Exists(request.FilePathOrId)) && enqueueTask)
        {
            throw new FileNotFoundException();
        }

        if (!request.BackupId.Equals(Guid.Empty))
        {
            var backupRecord = await backupRepository.GetBackupRecordAsync(request.BackupId);
            if (backupRecord == null)
            {
                throw new FileNotFoundException();
            }

            request.FilePathOrId = backupRecord.StoragePath;
            request.StorageType = backupRecord.StorageType;
            request.StorageParams = JsonSerializer.Deserialize<Dictionary<string, string>>(backupRecord.StorageParams);
        }

        var progress = await backupWorker.StartRestoreAsync(request, enqueueTask, taskId);
        if (!string.IsNullOrEmpty(progress.Error))
        {
            throw new FaultException();
        }
        return progress.TaskId;
    }

    public async Task<BackupProgress> GetBackupProgress(int tenantId)
    {
        return await backupWorker.GetBackupProgressAsync(tenantId);
    }

    public async Task<BackupProgress> GetTransferProgress(int tenantId)
    {
        return await backupWorker.GetTransferProgressAsync(tenantId);
    }

    public async Task<BackupProgress> GetRestoreProgress(int tenantId)
    {
        return await backupWorker.GetRestoreProgressAsync(tenantId);
    }

    public string GetTmpFolder()
    {
        return backupWorker.TempFolder;
    }

    public async Task CreateScheduleAsync(CreateScheduleRequest request)
    {
        await backupRepository.SaveBackupScheduleAsync(
            new BackupSchedule
            {
                TenantId = request.TenantId,
                Cron = request.Cron,
                BackupsStored = request.NumberOfBackupsStored,
                StorageType = request.StorageType,
                StorageBasePath = request.StorageBasePath,
                StorageParams = JsonSerializer.Serialize(request.StorageParams),
                Dump = request.Dump
            });
    }

    public async Task DeleteScheduleAsync(int tenantId)
    {
        await backupRepository.DeleteBackupScheduleAsync(tenantId);
    }

    public async Task<ScheduleResponse> GetScheduleAsync(int tenantId)
    {
        var schedule = await backupRepository.GetBackupScheduleAsync(tenantId);
        if (schedule != null)
        {
            var tmp = new ScheduleResponse
            {
                StorageType = schedule.StorageType,
                StorageBasePath = schedule.StorageBasePath,
                NumberOfBackupsStored = schedule.BackupsStored,
                Cron = schedule.Cron,
                LastBackupTime = schedule.LastBackupTime,
                StorageParams = JsonSerializer.Deserialize<Dictionary<string, string>>(schedule.StorageParams),
                Dump = schedule.Dump
            };

            return tmp;
        }

        return null;
    }
}
