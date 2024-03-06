﻿// (c) Copyright Ascensio System SIA 2010-2023
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

using ASC.Core.Common.Hosting;

using Microsoft.Extensions.Options;

using static ASC.Notify.Engine.NotifyEngine;

namespace ASC.Core.Common.Notify.Engine;

[Singleton]
public class NotifySchedulerService(NotifyEngine notifyEngine,
                                    ILogger<NotifyEngine> logger,
                                    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan _defaultSleep = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.DebugNotifySchedulerServiceStarting();

        stoppingToken.Register(logger.DebugNotifySchedulerServiceStopping);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var serviceScope = scopeFactory.CreateAsyncScope();

            var registerInstanceService = serviceScope.ServiceProvider.GetService<IRegisterInstanceManager<NotifySchedulerService>>();
            var instanceId = serviceScope.ServiceProvider.GetService<IOptions<InstanceWorkerOptions<NotifySchedulerService>>>().Value.InstanceId;

            if (!await registerInstanceService.IsActive(instanceId))
            {
                logger.DebugNotifySchedulerServiceIsNotActive(instanceId);

                await Task.Delay(1000, stoppingToken);

                continue;
            }

            var delay = await ExecuteNotifySchedulerAsync(stoppingToken);

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task<TimeSpan> ExecuteNotifySchedulerAsync(CancellationToken stoppingToken)
    {
        var min = DateTime.MaxValue;
        var now = DateTime.UtcNow;

        List<SendMethodWrapper> copy;

        lock (notifyEngine.SendMethods)
        {
            copy = notifyEngine.SendMethods.ToList();
        }

        foreach (var w in copy)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (!w.ScheduleDate.HasValue)
            {
                lock (notifyEngine.SendMethods)
                {
                    notifyEngine.SendMethods.Remove(w);
                }
                continue;
            }

            if (w.ScheduleDate.Value <= now)
            {
                try
                {
                    await w.InvokeSendMethod(now);
                }
                catch (Exception error)
                {
                    logger.ErrorInvokeSendMethod(error);
                }
                w.UpdateScheduleDate(now);
            }

            if (w.ScheduleDate.Value > now && w.ScheduleDate.Value < min)
            {
                min = w.ScheduleDate.Value;
            }
        }

        var wait = min != DateTime.MaxValue ? min - DateTime.UtcNow : _defaultSleep;

        if (wait < _defaultSleep)
        {
            wait = _defaultSleep;
        }
        else if (wait.Ticks > int.MaxValue)
        {
            wait = TimeSpan.FromTicks(int.MaxValue);
        }

        return wait;
    }
}