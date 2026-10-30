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

namespace ASC.Files.Core.Services.DocumentBuilderService;

[Singleton]
public class FormRecoveryTaskManager(IDistributedTaskQueueFactory queueFactory, IDistributedLockProvider distributedLockProvider)
{
    private const string TaskIdPrefix = "FormRecoveryTask";

    private readonly Lock _queuedOnLock = new();
    private DateTime _lastQueuedOn;
    private readonly DistributedTaskQueue<FormRecoveryTask> _queue = queueFactory.CreateQueue<FormRecoveryTask>();

    // Per user, as the report it starts: a second request of the same user waits for the running one.
    public static string GetTaskId(int tenantId, Guid userId, int originalFormId) => $"{TaskIdPrefix}_{tenantId}_{userId}_{originalFormId}";

    /// <summary>
    /// Queues the recovery of the form for the user; false when one is already running, which starts the report itself.
    /// </summary>
    public async Task<bool> TryEnqueueAsync(FormRecoveryTask newTask)
    {
        // Requests of one user may reach two workers at once: the check and the publication go under a lock of the cluster
        // per recovery, so that only one of them queues it and the starts of other recoveries do not wait.
        IDistributedLockHandle handle;
        try
        {
            handle = await distributedLockProvider.TryAcquireFairLockAsync($"{newTask.Id}_start");
        }
        catch (DistributedLockException)
        {
            // The start that holds the lock queued the recovery, which starts the report itself. A lock that could not be had
            // for any other reason fails the request, which the caller logs.
            if (await _queue.PeekTask(newTask.Id) is { IsCompleted: false })
            {
                return false;
            }

            throw;
        }

        await using (handle)
        {
            var task = await _queue.PeekTask(newTask.Id);
            if (task is { IsCompleted: false })
            {
                return false;
            }

            if (task != null)
            {
                await _queue.DequeueTask(task.Id);
            }

            newTask.QueuedOn = NextQueuedOn();

            // Published at once, so that the task is seen as running, and queued once the earlier recoveries of the form are
            // done. The heartbeat starts first, since a free reader may start the task as soon as it is queued and stop it.
            newTask.StartQueuedHeartbeat();
            try
            {
                await _queue.PublishTask(newTask);
            }
            catch
            {
                await newTask.StopQueuedHeartbeatAsync();
                throw;
            }

            _ = newTask.QueueWhenFreeAsync(this, _queue);

            return true;
        }
    }

    // The order the recoveries of a form run in; kept growing when the clock is set back.
    private DateTime NextQueuedOn()
    {
        lock (_queuedOnLock)
        {
            var now = DateTime.UtcNow;
            return _lastQueuedOn = now > _lastQueuedOn ? now : _lastQueuedOn.AddTicks(1);
        }
    }

    /// <summary>
    /// Whether a recovery of the form runs or waits, for any user. It starts a report when done, which takes in every
    /// response indexed meanwhile.
    /// </summary>
    public async Task<bool> IsRecoveringAsync(int tenantId, int originalFormId)
    {
        return (await _queue.GetAllTasks()).Any(t => !t.IsCompleted && t.TenantId == tenantId && t.OriginalFormId == originalFormId);
    }

    /// <summary>
    /// The recoveries of the form that run or wait, for any user.
    /// </summary>
    public async Task<List<FormRecoveryTask>> GetRecoveriesAsync(int tenantId, int originalFormId)
    {
        return (await _queue.GetAllTasks()).Where(t => !t.IsCompleted && t.TenantId == tenantId && t.OriginalFormId == originalFormId).ToList();
    }

    /// <summary>
    /// Whether the first recovery was queued before the second. The order is strict, the id breaking a tie, so that two
    /// recoveries never wait for each other.
    /// </summary>
    public static bool IsQueuedBefore(FormRecoveryTask first, FormRecoveryTask second)
    {
        return first.QueuedOn < second.QueuedOn || (first.QueuedOn == second.QueuedOn && string.CompareOrdinal(first.Id, second.Id) < 0);
    }
}
