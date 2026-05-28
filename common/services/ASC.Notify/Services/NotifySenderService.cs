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

using ASC.Core.Common.Notify.Model;

namespace ASC.Notify.Services;

[Singleton]
public class NotifySenderService(
    ConfigureNotifyServiceCfg notifyServiceCfg,
    NotifyConfiguration notifyConfiguration,
    DbWorker dbWorker,
    IServiceScopeFactory scopeFactory,
    ILogger<NotifySenderService> logger)
    : ActivePassiveBackgroundService<NotifySenderService>(logger, scopeFactory)

{
    private readonly NotifyServiceCfg _notifyServiceCfg = notifyServiceCfg.Value;

    protected override TimeSpan ExecuteTaskPeriod { get; set; } = TimeSpan.Zero;
    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        if (_notifyServiceCfg.Schedulers != null && _notifyServiceCfg.Schedulers.Count != 0)
        {
            InitializeNotifySchedulers();
        }

        await ThreadManagerWorkAsync(stoppingToken);
    }

    private void InitializeNotifySchedulers()
    {
        notifyConfiguration.Configure();

        foreach (var pair in _notifyServiceCfg.Schedulers.Where(r => r.MethodInfo != null))
        {
            logger.DebugStartScheduler(pair.Name, FormatMethodSignature(pair.MethodInfo));
            pair.MethodInfo.Invoke(null, null);
        }
    }

    private static string FormatMethodSignature(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));

        return $"{method.DeclaringType?.FullName}.{method.Name}({paramList})";
    }

    private async Task ThreadManagerWorkAsync(CancellationToken stoppingToken)
    {
        try
        {
            var messages = await dbWorker.GetMessagesAsync(_notifyServiceCfg.Process.BufferSize);
            if (messages.Count == 0)
            {
                await Task.Delay(5000, stoppingToken);
                return;
            }

            var tasks = new List<Task>(_notifyServiceCfg.Process.MaxThreads);

            foreach (var notifyMessage in messages)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                if (tasks.Count >= _notifyServiceCfg.Process.MaxThreads)
                {
                    await Task.WhenAny(tasks.ToArray());
                    tasks.RemoveAll(a => a.IsCompleted);
                }

                tasks.Add(SendOneAsync(notifyMessage, stoppingToken));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception e)
        {
            logger.ErrorThreadManagerWork(e);
        }
    }

    private async Task SendOneAsync(KeyValuePair<int, NotifyMessage> m, CancellationToken stoppingToken)
    {
        try
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            var result = MailSendingState.Sended;
            try
            {
                var sender = _notifyServiceCfg.Senders.FirstOrDefault(r => r.Name == m.Value.SenderType);
                if (sender != null)
                {
                    await sender.NotifySender.SendAsync(m.Value);
                }
                else
                {
                    result = MailSendingState.FatalError;
                }

                logger.DebugNotify(m.Key);
            }
            catch (Exception e)
            {
                result = MailSendingState.FatalError;
                logger.ErrorWithException(e);
            }

            await dbWorker.SetStateAsync(m.Key, result);
        }
        catch (Exception e)
        {
            logger.ErrorSendMessages(e);
        }
    }
}
