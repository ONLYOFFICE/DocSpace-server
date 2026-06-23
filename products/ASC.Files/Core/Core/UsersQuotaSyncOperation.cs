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

namespace ASC.Web.Files;

[Singleton]
public class UsersQuotaSyncOperation(IServiceProvider serviceProvider, IDistributedTaskQueueFactory queueFactory)
{
    private readonly DistributedTaskQueue<UsersQuotaSyncJob> _progressQueue = queueFactory.CreateQueue<UsersQuotaSyncJob>();

    public async Task RecalculateQuota(Tenant tenant)
    {
        var item = (await _progressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenant.Id);
        if (item is { IsCompleted: true })
        {
            await _progressQueue.DequeueTask(item.Id);
            item = null;
        }

        if (item == null)
        {
            item = serviceProvider.GetRequiredService<UsersQuotaSyncJob>();
            item.InitJob(tenant);
            await _progressQueue.EnqueueTask(item);
        }
    }
    public async Task<TaskProgressDto> CheckRecalculateQuota(Tenant tenant)
    {
        var item = (await _progressQueue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenant.Id);
        var progress = new TaskProgressDto();

        if (item == null)
        {
            progress.IsCompleted = true;
            return progress;
        }

        progress.IsCompleted = item.IsCompleted;
        progress.Progress = (int)item.Percentage;

        if (item.IsCompleted)
        {
            await _progressQueue.DequeueTask(item.Id);
        }

        return progress;

    }
}

[Transient]
public class UsersQuotaSyncJob : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public int TenantId { get; set; }

    public UsersQuotaSyncJob()
    {

    }

    public UsersQuotaSyncJob(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void InitJob(Tenant tenant)
    {
        TenantId = tenant.Id;
    }

    public override async Task RunJob(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var tenant = await tenantManager.SetCurrentTenantAsync(TenantId);

            var settingsManager = scope.ServiceProvider.GetRequiredService<SettingsManager>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            var filesSpaceUsageStatManager = scope.ServiceProvider.GetRequiredService<FilesSpaceUsageStatManager>();

            await filesSpaceUsageStatManager.RecalculateQuota(tenant.Id);

            var tenantQuotaSettings = await settingsManager.LoadAsync<TenantQuotaSettings>();
            tenantQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(tenantQuotaSettings);

            var users = await userManager.GetUsersAsync();

            foreach (var user in users)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Status = DistributedTaskStatus.Canceled;
                    break;
                }

                Percentage += 1.0 * 100 / users.Length;
                await PublishChanges();

                await filesSpaceUsageStatManager.RecalculateUserQuota(TenantId, user.Id);
            }

            var userQuotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            userQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(userQuotaSettings);

            await filesSpaceUsageStatManager.RecalculateFoldersUsedSpace(TenantId);

            var roomQuotaSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
            roomQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(roomQuotaSettings);

            var aiAgentQuotaSettings = await settingsManager.LoadAsync<TenantAiAgentQuotaSettings>();
            aiAgentQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(aiAgentQuotaSettings);

        }
        catch (Exception ex)
        {
            Status = DistributedTaskStatus.Failted;
            Exception = ex;
        }
        finally
        {
            IsCompleted = true;
        }

        await PublishChanges();
    }
}