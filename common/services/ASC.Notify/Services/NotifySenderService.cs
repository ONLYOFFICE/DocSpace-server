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
            logger.DebugStartScheduler(pair.Name, pair.MethodInfo);
            pair.MethodInfo.Invoke(null, null);
        }
    }

    private async Task ThreadManagerWorkAsync(CancellationToken stoppingToken)
    {
        var tasks = new List<Task>(_notifyServiceCfg.Process.MaxThreads);

        try
        {
            if (tasks.Count < _notifyServiceCfg.Process.MaxThreads)
            {
                var messages = await dbWorker.GetMessagesAsync(_notifyServiceCfg.Process.BufferSize);
                if (messages.Count > 0)
                {
                    var t = new Task(async () => await SendMessagesAsync(messages, stoppingToken), stoppingToken, TaskCreationOptions.LongRunning);
                    tasks.Add(t);
                    t.Start(TaskScheduler.Default);
                }
                else
                {
                    await Task.Delay(5000, stoppingToken);
                }
            }
            else
            {
                await Task.WhenAny(tasks.ToArray()).ContinueWith(_ => tasks.RemoveAll(a => a.IsCompleted), stoppingToken);
            }
        }
        catch (ThreadAbortException)
        {
        }
        catch (Exception e)
        {
            logger.ErrorThreadManagerWork(e);
        }
    }

    private async Task SendMessagesAsync(IDictionary<int, NotifyMessage> messages, CancellationToken stoppingToken)
    {
        try
        {
            foreach (var m in messages)
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
        }
        catch (ThreadAbortException)
        {
        }
        catch (Exception e)
        {
            logger.ErrorSendMessages(e);
        }
    }
}