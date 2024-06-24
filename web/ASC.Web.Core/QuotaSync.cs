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

namespace ASC.Web.Studio.Core.Quota;

[Singleton]
public class QuotaSyncOperation(IServiceProvider serviceProvider, IDistributedTaskQueueFactory queueFactory)
{

    public const string CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME = "quotaOperation";

    private readonly DistributedTaskQueue _progressQueue = queueFactory.CreateQueue(CUSTOM_DISTRIBUTED_TASK_QUEUE_NAME);

    public async Task RecalculateQuota(Tenant tenant)
    {
        var item = (await _progressQueue.GetAllTasks<QuotaSyncJob>()).FirstOrDefault(t => t.TenantId == tenant.Id);
        if (item is { IsCompleted: true })
        {
            await _progressQueue.DequeueTask(item.Id);
            item = null;
        }
        
        if (item == null)
        {
            item = serviceProvider.GetRequiredService<QuotaSyncJob>();
            item.InitJob(tenant);
            await _progressQueue.EnqueueTask(item);
        }

        await item.PublishChanges();
    }

    public async Task<bool> CheckRecalculateQuota(Tenant tenant)
    {
        var item = (await _progressQueue.GetAllTasks<QuotaSyncJob>()).FirstOrDefault(t => t.TenantId == tenant.Id);
        if (item is { IsCompleted: true })
        {
            await _progressQueue.DequeueTask(item.Id);
            return false;
        }

        return item != null;
    }
}

[Transient]
public class QuotaSyncJob(IServiceScopeFactory serviceScopeFactory) : DistributedTaskProgress
{
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

    public void InitJob(Tenant tenant)
    {
        TenantId = tenant.Id;
    }
    protected override async Task DoJob()
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var tenantManager = scope.ServiceProvider.GetRequiredService<TenantManager>();
            var storageFactoryConfig = scope.ServiceProvider.GetRequiredService<StorageFactoryConfig>();
            var storageFactory = scope.ServiceProvider.GetRequiredService<StorageFactory>();

            await tenantManager.SetCurrentTenantAsync(TenantId);
            var storageModules = storageFactoryConfig.GetModuleList();

            foreach (var module in storageModules)
            {
                var storage = await storageFactory.GetStorageAsync(TenantId, module);
                await storage.ResetQuotaAsync("");

                var domains = storageFactoryConfig.GetDomainList(module);
                foreach (var domain in domains)
                {
                    await storage.ResetQuotaAsync(domain);
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
        await PublishChanges();
    }
}
