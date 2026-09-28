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
    WorkerSignal signal)
    : BackgroundService
{
    private readonly int _threadCount = settings.ThreadCount ?? 10;
    private readonly TimeSpan _waitingPeriod = TimeSpan.FromSeconds(5);
    private const int BatchWaves = 5;
    private readonly TimeSpan _lease = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await eventBus.SubscribeAsync<WebhookRequestIntegrationEvent, WebhookRequestIntegrationEventHandler>();

        stoppingToken.Register(eventBus.Unsubscribe<WebhookRequestIntegrationEvent, WebhookRequestIntegrationEventHandler>);

        var batchSize = _threadCount * BatchWaves;
        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _threadCount, CancellationToken = stoppingToken };

        while (!stoppingToken.IsCancellationRequested)
        {
            var claimed = 0;

            try
            {
                List<(int TenantId, int Id)> entries;

                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    var dbWorker = scope.ServiceProvider.GetRequiredService<DbWorker>();
                    entries = await dbWorker.ClaimDueJournalEntriesAsync(batchSize, _lease);
                }

                claimed = entries.Count;

                if (claimed > 0)
                {
                    await Parallel.ForEachAsync(entries, parallelOptions, async (entry, token) => await webhookSender.Send(entry.TenantId, entry.Id, token));

                    logger.DebugProcedureFinish();
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

            if (claimed < batchSize)
            {
                logger.TraceProcedure(_waitingPeriod);

                await signal.WaitAsync(_waitingPeriod, stoppingToken);
            }
        }
    }
}