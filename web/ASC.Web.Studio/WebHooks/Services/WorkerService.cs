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

namespace ASC.Webhooks.Service.Services;

[Singleton]
public class WorkerService(
    WebhookSender webhookSender,
    ILogger<WorkerService> logger,
    Settings settings,
    IEventBus eventBus,
    IServiceScopeFactory scopeFactory,
    WorkerSignal signal,
    IDistributedLockProvider distributedLockProvider)
    : BackgroundService
{
    private const string UpkeepLockKey = "lock_webhooks_journal_upkeep";

    private readonly int _threadCount = settings.ThreadCount ?? 10;
    private readonly TimeSpan _waitingPeriod = TimeSpan.FromSeconds(5);

    // Rows are claimed only for the slots that are free, and each one is sent as soon as it is claimed, so a claimed row
    // never waits for others: one slow target holds its own slot and nothing else. The claim lease only has to cover the
    // checks before the request, where the row's lease is renewed for the attempt itself (WebhookSender.AttemptLease);
    // should it still run out, the renewal finds a row another instance has taken and does not send it.
    private readonly TimeSpan _lease = TimeSpan.FromTicks(Math.Max(TimeSpan.FromMinutes(10).Ticks, webhookSender.AttemptLease.Ticks));

    // how often the upkeep moves the due rows of inactive portals out of the claim's way, and deletes the delivery
    // records older than webhooks:journalRetentionDays when that is set; one instance at a time does it (UpkeepLockKey).
    // Runs beside the delivery loop, never in it, so a slow upkeep statement cannot hold up the claim
    private readonly TimeSpan _upkeepPeriod = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventBus.SubscribeAsync<WebhookRequestIntegrationEvent, WebhookRequestIntegrationEventHandler>();

        stoppingToken.Register(eventBus.Unsubscribe<WebhookRequestIntegrationEvent, WebhookRequestIntegrationEventHandler>);

        var upkeep = Task.Run(() => RunUpkeepAsync(stoppingToken), CancellationToken.None);

        try
        {
            await DeliverAsync(stoppingToken);
        }
        finally
        {
            await upkeep;
        }
    }

    private async Task DeliverAsync(CancellationToken stoppingToken)
    {
        using var slots = new SemaphoreSlim(_threadCount, _threadCount);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // wait for one free slot, then take every other slot that is free as well; the slots taken are counted
                // inside the try, so a cancellation while taking them still hands every one of them back
                await slots.WaitAsync(stoppingToken);

                var free = 1;
                var claimed = 0;

                try
                {
                    while (slots.Wait(0, stoppingToken))
                    {
                        free++;
                    }

                    List<(int TenantId, int Id, Guid LeaseId)> entries;

                    await using (var scope = scopeFactory.CreateAsyncScope())
                    {
                        var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();

                        entries = await dbWorker.ClaimDueJournalEntriesAsync(free, _lease);
                    }

                    claimed = entries.Count;

                    foreach (var entry in entries)
                    {
                        _ = SendAsync(entry, slots, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    logger.ErrorWithException(e);
                }
                finally
                {
                    if (free > claimed)
                    {
                        slots.Release(free - claimed);
                    }
                }

                if (claimed < free)
                {
                    logger.TraceProcedure(_waitingPeriod);

                    await signal.WaitAsync(_waitingPeriod, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            // every slot comes back once its attempt has ended, so the claimed rows are handed back before the service stops
            for (var i = 0; i < _threadCount; i++)
            {
                await slots.WaitAsync(CancellationToken.None);
            }
        }
    }

    private async Task SendAsync((int TenantId, int Id, Guid LeaseId) entry, SemaphoreSlim slots, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Yield();
            await webhookSender.Send(entry.TenantId, entry.Id, entry.LeaseId, stoppingToken);

            logger.DebugProcedureFinish();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
        }
        finally
        {
            slots.Release();
        }
    }

    private async Task RunUpkeepAsync(CancellationToken stoppingToken)
    {
        // a random start per instance, so instances started together do not all reach for the upkeep at once
        if (!await DelayAsync(_upkeepPeriod * Random.Shared.NextDouble(), stoppingToken))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            // one instance does the upkeep: the one holding the lock keeps it, and runs every round, until it stops;
            // the others try again every period and take over once it has gone. Without the lock this round is
            // skipped, the upkeep only tidies up and can wait
            IDistributedLockHandle upkeepLock = null;

            try
            {
                upkeepLock = await distributedLockProvider.TryAcquireLockAsync(UpkeepLockKey, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (DistributedLockException)
            {
                logger.DebugUpkeepElsewhere();
            }
            catch (Exception e)
            {
                logger.ErrorUpkeep(e);
            }

            if (upkeepLock == null)
            {
                if (!await DelayAsync(_upkeepPeriod, stoppingToken))
                {
                    return;
                }

                continue;
            }

            try
            {
                do
                {
                    await RunUpkeepRoundAsync(stoppingToken);
                }
                while (await DelayAsync(_upkeepPeriod, stoppingToken));
            }
            finally
            {
                try
                {
                    await upkeepLock.DisposeAsync();
                }
                catch (Exception e)
                {
                    logger.ErrorUpkeep(e);
                }
            }
        }
    }

    private async Task RunUpkeepRoundAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();

            var moved = await dbWorker.PostponeInactiveTenantsJournalAsync(DateTime.UtcNow - webhookSender.RetryWindow);
            var deleted = 0;

            if (settings.JournalRetentionDays is { } retentionDays)
            {
                deleted = await dbWorker.DeleteExpiredJournalAsync(DateTime.UtcNow.AddDays(-retentionDays), stoppingToken);
            }

            if (moved > 0 || deleted > 0)
            {
                logger.InfoUpkeepDone(moved, deleted);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            logger.ErrorUpkeep(e);
        }
    }

    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
