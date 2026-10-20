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

/// <summary>
/// Recovers the form responses missing from the search index in a queue of its own, so that the report queue is not
/// held, and then starts the requested report; a new report file first gets a version for each older version of the form.
/// </summary>
[Transient]
public class FormRecoveryTask : DistributedTaskProgress
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private string _baseUri;
    private int _tenantId;
    private Guid _userId;
    private FormFillingReportTaskData _reportData;

    // The queue cancels a task that publishes nothing for a minute (its default unregister window), and a recovery of
    // hundreds of forms runs for many, so it publishes its progress as it goes.
    private const long HeartbeatIntervalMs = 3000;
    private long _lastHeartbeatAt;

    // The report the client polls waits in its own queue, under the same window, until the recovery starts it, and shows
    // the progress of the recovery. The report then publishes its own from 30 on, so the recovery and the older versions
    // of the report share the part below it.
    private const double RecoveryShare = 25;
    private const double HistoryShare = 5;
    private DistributedTaskQueue<FormFillingReportTask> _reportQueue;

    public FormRecoveryTask() { }

    public FormRecoveryTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(string baseUri, int tenantId, Guid userId, FormFillingReportTaskData reportData)
    {
        _baseUri = baseUri;
        _tenantId = tenantId;
        _userId = userId;
        _reportData = reportData;
        Id = FormRecoveryTaskManager.GetTaskId(tenantId, userId, reportData.OriginalFormId);
        Status = DistributedTaskStatus.Created;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FormRecoveryTask>>();

        try
        {
            if (!string.IsNullOrEmpty(_baseUri))
            {
                scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().ServerUri = _baseUri;
            }

            await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(_tenantId);
            await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(_userId);

            _lastHeartbeatAt = Environment.TickCount64;
            _reportQueue = scope.ServiceProvider.GetRequiredService<IDistributedTaskQueueFactory>().CreateQueue<FormFillingReportTask>();

            var recoveryService = scope.ServiceProvider.GetRequiredService<FormRecoveryService>();
            await recoveryService.RecoverAsync(_reportData.RoomId, _reportData.OriginalFormId, share => ProgressAsync(share * RecoveryShare), CancellationToken);
            await ProgressAsync(RecoveryShare);

            if (_reportData.IsNewFile)
            {
                await BuildReportHistoryAsync(scope.ServiceProvider, recoveryService, logger);
            }

            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException)
        {
            Status = DistributedTaskStatus.Canceled;
            throw;
        }
        catch (Exception e)
        {
            logger.ErrorWithException(e);
            Exception = e;
            Status = DistributedTaskStatus.Failted;
        }
        finally
        {
            // Whatever became of the recovery: the client already waits for the report, which reports its own failures.
            try
            {
                var reportTask = scope.ServiceProvider.GetRequiredService<FormFillingReportTask>();
                reportTask.Init(_baseUri, _tenantId, _userId, _reportData, DocumentBuilderTaskManager.GetTaskId(_tenantId, _userId, _reportData.OriginalFormId));

                // Queued with the progress shown so far, so that the client does not see it fall back to 0.
                reportTask.Percentage = Percentage;
                await scope.ServiceProvider.GetRequiredService<DocumentBuilderTaskManager<FormFillingReportTask, int, FormFillingReportTaskData>>().StartTask(reportTask);
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }

            IsCompleted = true;
            Percentage = 100;

            try
            {
                await PublishChanges();
            }
            catch (Exception e)
            {
                logger.ErrorWithException(e);
            }
        }
    }

    // A new report file gets a version for each older version of the form with responses, oldest first, as a report kept
    // since the form was first filled has; the requested report of the current version comes last.
    private async Task BuildReportHistoryAsync(IServiceProvider serviceProvider, FormRecoveryService recoveryService, ILogger logger)
    {
        var olderVersions = (await recoveryService.GetVersionsWithResponsesAsync(_reportData.RoomId, _reportData.OriginalFormId))
            .Where(v => v < _reportData.OriginalFormVersion)
            .ToList();
        if (olderVersions.Count == 0)
        {
            return;
        }

        // The first report fills the empty file made for it, and each next one adds a version.
        await recoveryService.SetReportVersionChangedAsync(_reportData.RoomId, _reportData.OriginalFormId, false);

        var isNewFile = true;
        for (var i = 0; i < olderVersions.Count; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            await ProgressAsync(RecoveryShare + HistoryShare * i / olderVersions.Count);

            var version = olderVersions[i];

            // Run here, one after another, since the versions of the file follow the order of the runs. The client follows
            // only the requested report, so the progress of these only keeps the tasks alive in their queues.
            var reportTask = serviceProvider.GetRequiredService<FormFillingReportTask>();
            reportTask.Init(_baseUri, _tenantId, _userId, _reportData with { OriginalFormVersion = version, IsNewFile = isNewFile }, $"{Id}_v{version}");
            reportTask.Publication = _ => HeartbeatAsync();
            await reportTask.RunJob(CancellationToken);

            if (reportTask.Status != DistributedTaskStatus.Completed)
            {
                if (reportTask.Exception != null)
                {
                    logger.ErrorWithException(reportTask.Exception);
                }

                break;
            }

            isNewFile = false;
            await recoveryService.SetReportVersionChangedAsync(_reportData.RoomId, _reportData.OriginalFormId, true);
        }

        _reportData = _reportData with { IsNewFile = isNewFile };
    }

    private Task ProgressAsync(double percentage)
    {
        Percentage = Math.Max(Percentage, percentage);

        return HeartbeatAsync();
    }

    private async Task HeartbeatAsync()
    {
        if (Environment.TickCount64 - _lastHeartbeatAt < HeartbeatIntervalMs)
        {
            return;
        }

        _lastHeartbeatAt = Environment.TickCount64;
        await PublishChanges();

        var report = await _reportQueue.PeekTask(DocumentBuilderTaskManager.GetTaskId(_tenantId, _userId, _reportData.OriginalFormId));
        if (report is { IsCompleted: false, Status: DistributedTaskStatus.Created })
        {
            report.Percentage = Percentage;
            await _reportQueue.PublishTask(report);
        }
    }
}
