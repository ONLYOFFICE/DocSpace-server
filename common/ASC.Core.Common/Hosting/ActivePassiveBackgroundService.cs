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

namespace ASC.Core.Common.Hosting;
public abstract class ActivePassiveBackgroundService<T>(ILogger logger, IServiceScopeFactory scopeFactory)
    : BackgroundService where T : ActivePassiveBackgroundService<T>
{
    protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);
    protected abstract TimeSpan ExecuteTaskPeriod { get; set; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceName = GetType().Name;

        logger.DebugActivePassiveBackgroundServiceStarting(serviceName);

        stoppingToken.Register(() => logger.DebugActivePassiveBackgroundServiceStopping(serviceName));

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var serviceScope = scopeFactory.CreateAsyncScope();

            var registerInstanceService = serviceScope.ServiceProvider.GetService<IRegisterInstanceManager<T>>();
            var workerOptions = serviceScope.ServiceProvider.GetService<IOptions<InstanceWorkerOptions<T>>>().Value;

            const int millisecondsDelay = 1000;
            try
            {
                if (!await registerInstanceService.IsActive())
                {
                    logger.TraceActivePassiveBackgroundServiceIsNotActive(serviceName, workerOptions.InstanceId);

                    await Task.Delay(millisecondsDelay, stoppingToken);
                    continue;
                }
            }
            catch (Exception e)
            {
                logger.WarningWithException(e);

                await Task.Delay(millisecondsDelay, stoppingToken);
                continue;
            }

            logger.TraceActivePassiveBackgroundServiceIsRunning(serviceName);

            await ExecuteTaskAsync(stoppingToken);

            logger.TraceActivePassiveBackgroundServiceIsSleeping(serviceName, ExecuteTaskPeriod);

            await Task.Delay(ExecuteTaskPeriod, stoppingToken);
        }
    }
}