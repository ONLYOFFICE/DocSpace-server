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

namespace ASC.Files.Core;

[Singleton]
public class MetadataCascadeWorker(
    IDistributedTaskQueueFactory queueFactory,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
{
    private readonly DistributedTaskQueue<MetadataCascadeOperation> _queue = queueFactory.CreateQueue<MetadataCascadeOperation>();

    /// <summary>
    /// Enqueues a cascade operation for the folder. A running operation is reused only when it propagates the very
    /// same templates in the same mode with the same conflict rule; any other request gets its own operation, otherwise
    /// the templates (or the Overwrite) of the second request would silently never reach the subtree. The operations
    /// of a tenant run one after another (see <see cref="MetadataCascadeOperation.DoJob"/>). Completed operations are dropped.
    /// </summary>
    public async Task<string> StartAsync(int tenantId, Guid userId, int folderId, IEnumerable<int> templateIds, MetadataConflictResolveType conflict, MetadataCascadeMode mode)
    {
        var requestedTemplateIds = templateIds.Distinct().Order().ToArray();

        await using (await distributedLockProvider.TryAcquireFairLockAsync($"lock_metadata_cascade_{tenantId}"))
        {
            var folderTasks = (await _queue.GetAllTasks()).Where(t => t.TenantId == tenantId && t.FolderId == folderId && t.Mode == mode).ToList();

            foreach (var completed in folderTasks.Where(t => t.IsCompleted))
            {
                await _queue.DequeueTask(completed.Id);
            }

            var running = folderTasks.FirstOrDefault(t => !t.IsCompleted && t.Conflict == conflict && t.TemplateIds.SequenceEqual(requestedTemplateIds));

            if (running != null)
            {
                return running.Id;
            }

            var item = serviceProvider.GetService<MetadataCascadeOperation>();

            item.Init(tenantId, userId, folderId, requestedTemplateIds, conflict, mode);

            await _queue.EnqueueTask(item);

            return item.Id;
        }
    }

    /// <summary>
    /// Returns the assignment operation of the folder to report: a running one first, otherwise the most recent.
    /// </summary>
    public async Task<MetadataCascadeOperation> GetStatusAsync(int tenantId, int folderId)
    {
        var folderTasks = (await _queue.GetAllTasks())
            .Where(t => t.TenantId == tenantId && t.FolderId == folderId && t.Mode == MetadataCascadeMode.Assign)
            .ToList();

        return folderTasks.FirstOrDefault(t => !t.IsCompleted) ?? folderTasks.MaxBy(t => t.LastModifiedOn);
    }
}
