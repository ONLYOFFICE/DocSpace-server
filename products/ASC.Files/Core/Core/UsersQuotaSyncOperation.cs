﻿// (c) Copyright Ascensio System SIA 2010-2022
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

using ASC.Web.Core.WebZones;

namespace ASC.Web.Files;

[Singleton(Additional = typeof(UsersQuotaOperationExtension))]
public class UsersQuotaSyncOperation
{
    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "userQuotaOperation";

    private readonly DistributedTaskQueue _progressQueue;

    private readonly IServiceProvider _serviceProvider;


    public void RecalculateQuota(Tenant tenant)
    {
        var item = _progressQueue.GetAllTasks<UsersQuotaSyncJob>().FirstOrDefault(t => t.TenantId == tenant.Id);
        if (item is { IsCompleted: true })
        {
            _progressQueue.DequeueTask(item.Id);
            item = null;
        }

        if (item == null)
        {
            item = _serviceProvider.GetRequiredService<UsersQuotaSyncJob>();
            item.InitJob(tenant);
            _progressQueue.EnqueueTask(item.RunJobAsync, item);
        }
    }
    public TaskProgressDto CheckRecalculateQuota(Tenant tenant)
    {
        var item = _progressQueue.GetAllTasks<UsersQuotaSyncJob>().FirstOrDefault(t => t.TenantId == tenant.Id);
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
            _progressQueue.DequeueTask(item.Id);
        }

        return progress;
    }


    public UsersQuotaSyncOperation(IServiceProvider serviceProvider, IDistributedTaskQueueFactory queueFactory)
    {
        _serviceProvider = serviceProvider;
        _progressQueue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);
    }



    public static class UsersQuotaOperationExtension
    {
        public static void Register(DIHelper services)
        {
            services.TryAdd<UsersQuotaSyncJob>();
        }
    }
}

public class UsersQuotaSyncJob : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private int? _tenantId;
    public int TenantId
    {
        get
        {
            return _tenantId ?? this[nameof(_tenantId)];
        }
        private set
        {
            _tenantId = value;
            this[nameof(_tenantId)] = value;
        }
    }

    public UsersQuotaSyncJob(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void InitJob(Tenant tenant)
    {
        TenantId = tenant.Id;
    }

    public async Task RunJobAsync(DistributedTask _, CancellationToken cancellationToken)
    {
        try
        {
           await using var scope = _serviceScopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager>();
            var authentication = scope.ServiceProvider.GetRequiredService<AuthManager>();
            var securityContext = scope.ServiceProvider.GetRequiredService<SecurityContext>();
            var webItemManagerSecurity = scope.ServiceProvider.GetRequiredService<WebItemManagerSecurity>();

            await tenantManager.SetCurrentTenantAsync(TenantId);

            var users = await userManager.GetUsersAsync();
            var webItems = webItemManagerSecurity.GetItems(WebZoneType.All, ItemAvailableState.All);

            foreach (var user in users)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Status = DistributedTaskStatus.Canceled;
                    break;
                }

                Percentage += 1.0 * 100 / users.Length;
                PublishChanges();

                var account = await authentication.GetAccountByIDAsync(TenantId, user.Id);
                await securityContext.AuthenticateMeAsync(account);

                foreach (var item in webItems)
                {
                    if (item.ID == WebItemManager.DocumentsProductID)
                    {
                        var manager = item.Context.SpaceUsageStatManager as IUserSpaceUsage;
                        if (manager == null)
                        {
                            continue;
                        }
                        await manager.RecalculateUserQuota(TenantId, user.Id);
                    }
                }

            }
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
        PublishChanges();
    }
}
