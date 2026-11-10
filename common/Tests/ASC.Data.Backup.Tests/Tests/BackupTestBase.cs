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
// source code, which remains licensed under the GNU Affero General Public
// License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only


namespace ASC.Data.Backup.Tests.Tests;

/// <summary>
/// The backup jobs, polled the way the backup worker makes them observable, and the single guarded way
/// to call the restore endpoint. A backup is written by a background worker, so every wait is a deadline
/// loop that ends in an assertion naming the last observed state.
/// </summary>
public abstract class BackupTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    // Polling intervals of the TypeScript suite: the list is walked once and its last step repeats.
    private static readonly int[] _jobPollMs = [2_000, 5_000, 10_000];
    private static readonly int[] _recordPollMs = [500, 1_000, 2_000, 5_000];
    private static readonly int[] _stoppedPollMs = [250, 500, 1_000];

    private static readonly TimeSpan _jobTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _stoppedTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// A job has stopped when it is completed and its status says how it ended. The progress can show
    /// <c>isCompleted</c> for a moment while the status is still Created or Running, so the flag alone is
    /// not taken as the end.
    /// </summary>
    protected static bool HasStopped(BackupProgressDto? job)
    {
        return job is { IsCompleted: true } && job.Status != DistributedTaskStatus.Created && job.Status != DistributedTaskStatus.Running;
    }

    /// <summary>
    /// Starts a DataStore backup and waits until exactly that job has finished, returning its task id.
    /// The job is bound by <c>taskId</c>: the startbackup response carries it, the progress endpoint
    /// reports it while the job is in the queue, and the history record of the finished archive uses it as
    /// its <c>id</c>. Either of the last two is accepted as proof of completion, because the queue may drop
    /// a finished job before the first poll sees it. A job that reports an error fails the test at once.
    /// </summary>
    protected async Task<string> RunBackupAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var started = await _backupApi.StartBackupAsync(new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, ct);
        var taskId = started.Response?.TaskId;
        taskId.Should().NotBeNullOrEmpty($"startbackup answered {started.ToJson()}");

        var deadline = DateTime.UtcNow + _jobTimeout;

        for (var poll = 0; ; poll++)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;
            var lastJob = DescribeJob(job, taskId!);

            if (IsSameTask(job, taskId!))
            {
                if (!string.IsNullOrEmpty(job!.Error))
                {
                    Assert.Fail($"backup {taskId} failed: {job.Error}");
                }

                if (HasStopped(job))
                {
                    return taskId!;
                }
            }

            var lastHistory = await HistoryIdsAsync();

            if (ContainsId(lastHistory, taskId!))
            {
                return taskId!;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"backup {taskId} did not finish in {_jobTimeout.TotalSeconds:0} s: queue {lastJob}; history [{string.Join(", ", lastHistory)}]");
            }

            await Task.Delay(IntervalAt(_jobPollMs, poll), ct);
        }
    }

    /// <summary>
    /// Everything a rejected startbackup, deletebackup or startrestore could have changed: the job queue,
    /// the history and the counters. Compare two snapshots with <c>BeEquivalentTo</c>.
    /// </summary>
    protected async Task<BackupSnapshot> SnapshotBackupsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;
        var historyIds = await HistoryIdsAsync();
        var counts = (await _backupApi.GetBackupsCountsAsync(cancellationToken: ct)).Response;

        return new BackupSnapshot(job, historyIds, counts);
    }

    /// <summary>
    /// Waits until the given job has stopped, however it ended, and returns its final state. Fails the
    /// test, naming the last state of the queue, when it has not stopped in time.
    /// </summary>
    protected async Task<BackupProgressDto> WaitForBackupJobEndAsync(string taskId)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _jobTimeout;

        for (var poll = 0; ; poll++)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;

            if (IsSameTask(job, taskId) && HasStopped(job))
            {
                return job!;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"backup {taskId} did not stop in {_jobTimeout.TotalSeconds:0} s: queue {DescribeJob(job, taskId)}");
            }

            await Task.Delay(IntervalAt(_jobPollMs, poll), ct);
        }
    }

    /// <summary>
    /// Waits until the history holds a record with this id, which is how a finished backup shows up.
    /// Fails the test, listing the ids that were there, when it does not appear in time.
    /// </summary>
    protected async Task WaitForBackupRecordAsync(string taskId)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _jobTimeout;

        for (var poll = 0; ; poll++)
        {
            var historyIds = await HistoryIdsAsync();

            if (ContainsId(historyIds, taskId))
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the history never held a record for backup {taskId}: [{string.Join(", ", historyIds)}]");
            }

            await Task.Delay(IntervalAt(_recordPollMs, poll), ct);
        }
    }

    /// <summary>
    /// Watches the job queue after a cancel until it has been quiet for a while. "No job" right after
    /// cancelbackup is not final: the backup worker picks the job up from an event, so a job can show up
    /// in the queue after the cancel was answered, and then be marked canceled or even run to the end.
    /// Time cannot settle that, so the queue is polled until it has shown the same stopped state (no job,
    /// or a completed one) for <paramref name="stablePolls"/> polls in a row, and a running job restarts
    /// the count. When it never settles the observation comes back with <c>Settled</c> false and the
    /// states seen, for the caller to assert on.
    /// </summary>
    protected async Task<JobObservation> ObserveJobUntilQuiescentAsync(string taskId, int stablePolls = 8, int intervalMs = 250, int timeoutMs = 60_000)
    {
        var ct = TestContext.Current.CancellationToken;
        var states = new List<string>();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        var stable = 0;
        var previous = "";

        while (true)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;
            var signature = DescribeJob(job, taskId);

            if (states.Count == 0 || signature != states[^1])
            {
                states.Add(signature);
            }

            var stopped = job is null || job.IsCompleted;
            stable = stopped && signature == previous ? stable + 1 : 0;
            previous = signature;

            if (stopped && stable >= stablePolls)
            {
                return new JobObservation(job, states, true);
            }

            if (DateTime.UtcNow > deadline)
            {
                return new JobObservation(job, states, false);
            }

            await Task.Delay(intervalMs, ct);
        }
    }

    /// <summary>
    /// Starts a backup and returns its id once the job has been seen Running and not yet past its first
    /// step (progress 0), which is the only moment a cancel is known to take. A cancel sent before the
    /// backup worker has picked the job up - while the job is not in the queue yet, or is still Created -
    /// is answered true but the job goes on to run to the end and is archived. A cancel of a job seen
    /// Running was effective in all 107 measured attempts that were not overtaken by the end of the backup,
    /// one of a job seen Created in 2 of 16. The job can also be gone past that point before it is seen (a
    /// backup is about a second long); then that attempt proves nothing about cancel and a new backup is
    /// started. Each attempt that ran to the end is a real backup, so the portal needs a funded wallet.
    /// </summary>
    protected async Task<string> StartBackupAndCatchItRunningAsync(int attempts = 6, int pollMs = 30, int timeoutMs = 20_000)
    {
        var ct = TestContext.Current.CancellationToken;
        var outcomes = new List<string>();

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var started = await _backupApi.StartBackupAsync(new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, ct);
            var taskId = started.Response?.TaskId;
            taskId.Should().NotBeNullOrEmpty($"startbackup answered {started.ToJson()}");

            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (true)
            {
                var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;

                if (IsSameTask(job, taskId!))
                {
                    if (job!.Status == DistributedTaskStatus.Running && !job.IsCompleted && job.Progress == 0)
                    {
                        return taskId!;
                    }

                    // Past the first step, or already over: this attempt cannot show a cancel.
                    if (job.IsCompleted || job.Progress > 0)
                    {
                        outcomes.Add($"attempt {attempt}: seen status={job.Status} progress={job.Progress}");

                        break;
                    }
                }

                if (DateTime.UtcNow > deadline)
                {
                    Assert.Fail($"The backup {taskId} was never seen Running in {timeoutMs} ms; last seen {DescribeJob(job, taskId!)}");
                }

                await Task.Delay(pollMs, ct);
            }

            // Let this attempt finish before the next one: a start while a job runs lands on it.
            await WaitForBackupRecordAsync(taskId!);
            await WaitForQueueToHoldNoRunningJobAsync(taskId!);
        }

        Assert.Fail($"The backup was not caught Running in {attempts} attempts: {string.Join("; ", outcomes)}");

        return "";
    }

    /// <summary>
    /// Proves no restore was started. An empty restore queue alone is not enough, because the job is
    /// queued asynchronously, so this also requires the portal to still be fully operational: while a
    /// portal is being restored every other operation of the backup service answers 403, which the SDK
    /// raises as an exception here.
    /// </summary>
    protected async Task ExpectRestoreNotStartedAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var progress = await _backupApi.GetRestoreProgressAsync(cancellationToken: ct);
        progress.Response.Should().BeNull("no restore is running");
        progress.Count.Should().Be(0);

        await _backupApi.GetBackupHistoryAsync(cancellationToken: ct);
    }

    /// <summary>
    /// The only caller of the SDK restore call in the suite. The body goes through <see cref="RestoreGuard"/>
    /// first, which throws before anything is sent unless the body provably cannot reach the restore
    /// queue: a restore that really queues replaces the data of the portal, or with <c>dump</c> of the whole
    /// server database of the run. A non-2xx answer raises <see cref="ApiException"/>.
    /// </summary>
    protected Task<BackupProgressWrapper> StartRestoreThatCannotQueueAsync(StartBackupRestoreRequestDto? dto)
    {
        return RestoreGuard.SendAsync(dto, () => _backupApi.StartBackupRestoreAsync(dto, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// The only raw request to the restore route in the suite, for the bodies a typed DTO cannot carry
    /// (no body, a missing or wrong-typed <c>backupId</c>). It goes through <see cref="RestoreGuard"/> first.
    /// The caller disposes the response.
    /// </summary>
    protected Task<HttpResponseMessage> StartRawRestoreThatCannotQueueAsync(string? rawJson)
    {
        return RestoreGuard.SendAsync(rawJson, () => _backup.PostRawAsync("api/2.0/backup/startrestore", rawJson ?? "", TestContext.Current.CancellationToken));
    }

    // The queue holds no job of this task that is still going. A queue without any job counts as quiet.
    private async Task WaitForQueueToHoldNoRunningJobAsync(string taskId)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _stoppedTimeout;

        for (var poll = 0; ; poll++)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;

            if (job is null || HasStopped(job))
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the queue still held a job after backup {taskId} was archived: {DescribeJob(job, taskId)}");
            }

            await Task.Delay(IntervalAt(_stoppedPollMs, poll), ct);
        }
    }

    private async Task<string[]> HistoryIdsAsync()
    {
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        return (history.Response ?? []).Select(record => record.Id.ToString()).Order(StringComparer.Ordinal).ToArray();
    }

    private static bool ContainsId(string[] ids, string taskId)
    {
        return ids.Contains(taskId, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSameTask(BackupProgressDto? job, string taskId)
    {
        return job is not null && string.Equals(job.TaskId, taskId, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeJob(BackupProgressDto? job, string taskId)
    {
        return job is null
            ? "none"
            : $"{(IsSameTask(job, taskId) ? "ours" : "other")} status={job.Status} done={job.IsCompleted}";
    }

    private static TimeSpan IntervalAt(int[] stepsMs, int poll)
    {
        return TimeSpan.FromMilliseconds(stepsMs[Math.Min(poll, stepsMs.Length - 1)]);
    }
}

/// <summary>
/// What the backup service reports about a portal at one moment. The history ids are the record ids as
/// text, sorted, so they can be compared with a task id.
/// </summary>
/// <param name="Job">The backup job in the queue, or null when there is none.</param>
/// <param name="HistoryIds">The ids of the history records.</param>
/// <param name="Counts">The backup counters.</param>
public sealed record BackupSnapshot(BackupProgressDto? Job, string[] HistoryIds, BackupsCountResultDto? Counts);

/// <summary>
/// The outcome of watching the job queue after a cancel.
/// </summary>
/// <param name="Final">The last state of the queue: no job, or a stopped one.</param>
/// <param name="States">Each distinct state seen on the way, oldest first.</param>
/// <param name="Settled">False when the queue did not become quiet before the timeout.</param>
public sealed record JobObservation(BackupProgressDto? Final, IReadOnlyList<string> States, bool Settled);
