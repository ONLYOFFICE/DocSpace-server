﻿// (c) Copyright Ascensio System SIA 2009-2025
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
            var authentication = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
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

                var account = await authentication.GetAccountByIDAsync(TenantId, user.Id);
                await securityContext.AuthenticateMeAsync(account);

                await filesSpaceUsageStatManager.RecalculateUserQuota(TenantId, user.Id);
            }

            var userQuotaSettings = await settingsManager.LoadAsync<TenantUserQuotaSettings>();
            userQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(userQuotaSettings);

            await filesSpaceUsageStatManager.RecalculateFoldersUsedSpace(TenantId);

            var roomQuotaSettings = await settingsManager.LoadAsync<TenantRoomQuotaSettings>();
            roomQuotaSettings.LastRecalculateDate = DateTime.UtcNow;
            await settingsManager.SaveAsync(roomQuotaSettings);

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
