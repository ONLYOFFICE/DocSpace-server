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

namespace ASC.Migration.Core;

[Singleton]
public class MigrationWorker(
    IDistributedTaskQueueFactory queueFactory,
    IServiceProvider serviceProvider,
    ILogger<MigrationWorker> logger)
{
    private static readonly SemaphoreSlim _semaphoreSlim = new(1);
    private readonly DistributedTaskQueue<MigrationOperation> _queue = queueFactory.CreateQueue<MigrationOperation>();

    public async Task StartParse(int tenantId, Guid userId, string migratorName)
    {
        await Start(tenantId, item => item.InitParse(tenantId, userId, migratorName));
    }

    public async Task StartMigrate(int tenantId, Guid userId, MigrationApiInfo migrationApiInfo)
    {
        await Start(tenantId, item => item.InitMigrate(tenantId, userId, migrationApiInfo));
    }

    private async Task Start(int tenantId, Action<MigrationOperation> init)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            var item = (await _queue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);

            if (item is { IsCompleted: true })
            {
                await _queue.DequeueTask(item.Id);
                item = null;
            }

            if (item == null)
            {
                item = serviceProvider.GetService<MigrationOperation>();

                init(item);

                await _queue.EnqueueTask(item);
            }

            await item.PublishChanges();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task Stop(int tenantId)
    {
        var tasks = (await _queue.GetAllTasks()).Where(t => t.MigrationApiInfo.Operation == "parse" && t.TenantId == tenantId);

        foreach (var t in tasks)
        {
            await _queue.DequeueTask(t.Id);
        }

        await MigrationOperation.ClearMigrationFolder(serviceProvider, tenantId);
    }

    public async Task Clear(int tenantId)
    {
        var tasks = (await _queue.GetAllTasks()).Where(t => t.MigrationApiInfo.Operation == "migration" && t.TenantId == tenantId && t.IsCompleted);

        foreach (var t in tasks)
        {
            await _queue.DequeueTask(t.Id);
        }

        await MigrationOperation.ClearMigrationFolder(serviceProvider, tenantId);
    }

    public async Task<MigrationOperation> GetStatusAsync(int tenantId)
    {
        logger.Debug($"try get status {tenantId}");
        return (await _queue.GetAllTasks()).FirstOrDefault(t => t.TenantId == tenantId);
    }
}