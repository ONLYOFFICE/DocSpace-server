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

namespace ASC.Api.Core.Extensions;

public static class HostExtension
{
    extension(WebApplication webHost)
    {
        public async Task RunWithTasksAsync(bool awaitTasks = true, CancellationToken cancellationToken = default)
        {
            CustomSynchronizationContext.CreateContext();

            var t = RunTasksAsync(webHost, cancellationToken);

            if (awaitTasks)
            {
                await t.ConfigureAwait(false);
            }

            // Start the tasks as normal
            await webHost.RunAsync(cancellationToken);
        }

        private async Task RunTasksAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            var logger = webHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ASC.Api.Core.Extensions.HostExtension");
            var warmupState = webHost.Services.GetService<WarmupState>();
            warmupState?.StartWarmup();
            var totalStart = TimeProvider.System.GetTimestamp();

            // Load all tasks from DI
            var startupTasks = webHost.Services.GetServices<IStartupTask>();

            // Execute all the tasks
            foreach (var startupTask in startupTasks)
            {
                var taskName = startupTask.GetType().Name;
                var taskStart = TimeProvider.System.GetTimestamp();

                try
                {
                    var t = startupTask.ExecuteAsync(cancellationToken);

                    if (startupTask is IStartupTaskNotAwaitable)
                    {
                        _ = t.ContinueWith(
                            faulted => logger.ErrorStartupTaskFailed(taskName, faulted.Exception!),
                            TaskContinuationOptions.OnlyOnFaulted);
                    }
                    else
                    {
                        await t.ConfigureAwait(false);
                        logger.InfoStartupTaskCompleted(taskName, TimeProvider.System.GetElapsedTime(taskStart).TotalMilliseconds);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.ErrorStartupTaskFailed(taskName, ex);
                }
            }

            logger.InfoAllStartupTasksCompleted(TimeProvider.System.GetElapsedTime(totalStart).TotalMilliseconds);

            webHost.Services.GetService<WarmupState>()?.MarkReady();
        }
    }
}