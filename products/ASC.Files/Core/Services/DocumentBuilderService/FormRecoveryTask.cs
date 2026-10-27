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
    private Guid _userId;
    private FormFillingReportTaskData _reportData;

    // The queue cancels a task that publishes nothing for a minute (its default unregister window), and a recovery of
    // hundreds of forms runs for many, so it publishes on a timer: a single read through the builder can outlast the window.
    private static readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(3);

    // The heartbeat keeps the task alive whatever its reports do, so a report of the history gets the limit of a script
    // build: the builder is polled with no limit of its own.
    private static readonly TimeSpan _maxReportBuildTime = TimeSpan.FromMinutes(10);

    // The report the client polls waits in its own queue, under the same window, until the recovery starts it, and shows
    // the progress of the recovery. The report then publishes its own from 30 on, so the recovery and the older versions
    // of the report share the part below it.
    private const double RecoveryShare = 25;
    private const double HistoryShare = 5;
    private DistributedTaskQueue<FormFillingReportTask> _reportQueue;
    private ILogger<FormRecoveryTask> _logger;

    public int TenantId { get; set; }

    public int OriginalFormId { get; set; }

    public FormRecoveryTask() { }

    public FormRecoveryTask(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public void Init(string baseUri, int tenantId, Guid userId, FormFillingReportTaskData reportData)
    {
        _baseUri = baseUri;
        TenantId = tenantId;
        OriginalFormId = reportData.OriginalFormId;
        _userId = userId;
        _reportData = reportData;
        Id = FormRecoveryTaskManager.GetTaskId(tenantId, userId, reportData.OriginalFormId);
        Status = DistributedTaskStatus.Created;
    }

    protected override async Task DoJob()
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var logger = _logger = scope.ServiceProvider.GetRequiredService<ILogger<FormRecoveryTask>>();

        try
        {
            if (!string.IsNullOrEmpty(_baseUri))
            {
                scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().ServerUri = _baseUri;
            }

            await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(TenantId);
            await scope.ServiceProvider.GetRequiredService<SecurityContext>().AuthenticateMeWithoutCookieAsync(_userId);

            _reportQueue = scope.ServiceProvider.GetRequiredService<IDistributedTaskQueueFactory>().CreateQueue<FormFillingReportTask>();

            using var heartbeatStop = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
            var heartbeat = KeepAliveAsync(heartbeatStop.Token);
            try
            {
                var recoveryService = scope.ServiceProvider.GetRequiredService<FormRecoveryService>();
                await recoveryService.RecoverAsync(_reportData.RoomId, _reportData.OriginalFormId, share => Progress(share * RecoveryShare), CancellationToken);
                Progress(RecoveryShare);

                if (_reportData.IsNewFile)
                {
                    await BuildReportHistoryAsync(scope.ServiceProvider, recoveryService, logger);
                }
            }
            finally
            {
                // Stopped before the report starts, so that it is not published over.
                await heartbeatStop.CancelAsync();
                await heartbeat;
            }

            Status = DistributedTaskStatus.Completed;
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
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
                reportTask.Init(_baseUri, TenantId, _userId, _reportData, DocumentBuilderTaskManager.GetTaskId(TenantId, _userId, _reportData.OriginalFormId));

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
    // Only a new file can take them in order, since versions are added after the last one: a history that failed is built
    // again by deleting the report and generating it anew, which the warnings say.
    private async Task BuildReportHistoryAsync(IServiceProvider serviceProvider, FormRecoveryService recoveryService, ILogger<FormRecoveryTask> logger)
    {
        var versions = await recoveryService.GetVersionsWithResponsesAsync(_reportData.RoomId, _reportData.OriginalFormId);
        if (versions == null)
        {
            logger.WarnReportHistorySkipped(_reportData.OriginalFormId);
            return;
        }

        var olderVersions = versions.Where(v => v < _reportData.OriginalFormVersion).ToList();
        if (olderVersions.Count == 0)
        {
            return;
        }

        // The first report fills the empty file made for it, and each next one adds a version.
        await recoveryService.SetReportVersionChangedAsync(_reportData.RoomId, _reportData.OriginalFormId, false);

        for (var i = 0; i < olderVersions.Count; i++)
        {
            CancellationToken.ThrowIfCancellationRequested();
            Progress(RecoveryShare + HistoryShare * i / olderVersions.Count);

            var version = olderVersions[i];

            // A failure of the builder is mostly a passing one, so a report is tried twice before the history stops.
            var reportTask = await RunReportAsync(serviceProvider, version);
            if (reportTask.Status != DistributedTaskStatus.Completed)
            {
                reportTask = await RunReportAsync(serviceProvider, version);
            }

            if (reportTask.Status != DistributedTaskStatus.Completed)
            {
                logger.WarnReportHistoryStopped(reportTask.Exception, _reportData.OriginalFormId, version);
                break;
            }

            // At once, so that the requested report never takes the file for a new one once a version is in it.
            _reportData = _reportData with { IsNewFile = false };
            await recoveryService.SetReportVersionChangedAsync(_reportData.RoomId, _reportData.OriginalFormId, true);
        }
    }

    // Run here, one after another, since the versions of the file follow the order of the runs. The client follows only the
    // requested report, so these publish nothing: the heartbeat keeps this task alive meanwhile.
    private async Task<FormFillingReportTask> RunReportAsync(IServiceProvider serviceProvider, int version)
    {
        var reportTask = serviceProvider.GetRequiredService<FormFillingReportTask>();
        reportTask.Init(_baseUri, TenantId, _userId, _reportData with { OriginalFormVersion = version }, $"{Id}_v{version}");
        reportTask.Publication = _ => Task.CompletedTask;

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        deadline.CancelAfter(_maxReportBuildTime);
        try
        {
            await reportTask.RunJob(deadline.Token);
        }
        catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
        {
            // Out of time, or a request inside the build timed out: a failed run, tried again by the caller.
        }

        return reportTask;
    }

    private void Progress(double percentage)
    {
        Percentage = Math.Max(Percentage, percentage);
    }

    private async Task KeepAliveAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_heartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await HeartbeatAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task HeartbeatAsync()
    {
        // A failed publication must not stop the recovery; one that keeps failing gets the task cancelled by its queue anyway.
        try
        {
            await PublishChanges();

            var report = await _reportQueue.PeekTask(DocumentBuilderTaskManager.GetTaskId(TenantId, _userId, _reportData.OriginalFormId));
            if (report is { IsCompleted: false, Status: DistributedTaskStatus.Created })
            {
                report.Percentage = Percentage;
                await _reportQueue.PublishTask(report);
            }
        }
        catch (Exception e)
        {
            _logger.WarnRecoveryHeartbeatFailed(e, _reportData.OriginalFormId);
        }
    }
}
