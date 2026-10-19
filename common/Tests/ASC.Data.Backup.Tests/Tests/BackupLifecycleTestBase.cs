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


namespace ASC.Data.Backup.Tests.Tests;

/// <summary>
/// What the tests of the backup job lifecycle - start, cancel, delete, the reads around them - share on
/// top of <see cref="BackupTestBase"/>: the history as normalized ids, the check that no job is left
/// stuck in the queue, and the end state a canceled job has to settle in. Task ids are compared as
/// GUIDs in their canonical text, because the job reports the id as a string and the history as a GUID.
/// </summary>
public abstract class BackupLifecycleTestBase(AspireAppFixture fixture) : BackupTestBase(fixture)
{
    private static readonly TimeSpan _notStuckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The canonical text of a task id.</summary>
    protected static string Canonical(string taskId)
    {
        return Guid.Parse(taskId).ToString();
    }

    /// <summary>The task ids in canonical text, distinct and sorted the way the history ids are.</summary>
    protected static string[] SortedIds(params string[] taskIds)
    {
        return taskIds.Select(Canonical).Distinct().Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>The free and the paid backups of a period added up.</summary>
    protected static int Total(BackupsCountResultDto? counts)
    {
        return (counts?.Free ?? 0) + (counts?.Paid ?? 0);
    }

    /// <summary>Starts a DataStore backup, asserts the answer is a 200 that names a job, and returns the task id without waiting.</summary>
    protected async Task<string> StartDataStoreBackupAsync()
    {
        var started = await _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, TestContext.Current.CancellationToken);

        started.StatusCode.Should().Be(200);
        started.Response.TaskId.Should().NotBeNullOrEmpty();

        return started.Response.TaskId;
    }

    /// <summary>The ids of the history records in canonical text, sorted.</summary>
    protected async Task<string[]> ListHistoryIdsAsync()
    {
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        return (history.Response ?? []).Select(record => record.Id.ToString()).Order(StringComparer.Ordinal).ToArray();
    }

    /// <summary>The backup counters as the default period reports them.</summary>
    protected async Task<BackupsCountResultDto?> ReadCountsAsync()
    {
        return (await _backupApi.GetBackupsCountsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// No job is left running. A record can show in the history a moment before the progress of the same
    /// job turns completed, so "stuck" means a job that is still running when the wait is over, not one
    /// caught in that moment.
    /// </summary>
    protected async Task ExpectQueueNotStuckAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _notStuckTimeout;

        while (true)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;

            if (job is null || job.IsCompleted)
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the queue still held a running job after {_notStuckTimeout.TotalSeconds:0} s: task {job.TaskId}, status {job.Status}, progress {job.Progress}");
            }

            await Task.Delay(500, ct);
        }
    }

    /// <summary>
    /// The state a canceled job must end in. A canceled job is either gone from the queue or shown as
    /// Canceled, and it leaves no archive. The one other end the API has been seen to produce is a job
    /// that registered with the worker after the cancel was answered and then ran to the end: that is
    /// noted as a race in the test output, and still has to be consistent - a completed job has its record.
    /// </summary>
    protected async Task ExpectCanceledJobSettledAsync(string taskId, params bool[] cancelAnswers)
    {
        var observation = await ObserveJobUntilQuiescentAsync(taskId);
        var path = string.Join(" -> ", observation.States);

        observation.Settled.Should().BeTrue($"the queue did not settle for {taskId}: {path}");

        var archived = (await ListHistoryIdsAsync()).Contains(Canonical(taskId));
        var final = observation.Final;

        if (final is not null && string.Equals(final.TaskId, taskId, StringComparison.OrdinalIgnoreCase))
        {
            if (final.Status == DistributedTaskStatus.Canceled)
            {
                final.IsCompleted.Should().BeTrue(path);
                archived.Should().BeFalse(path);
            }
            else
            {
                final.Status.Should().Be(DistributedTaskStatus.Completed, path);
                archived.Should().BeTrue(path);
                NoteRace($"cancelbackup answered {string.Join(", ", cancelAnswers)} but the job ran to the end and was archived: {path}");
            }
        }
        else
        {
            archived.Should().BeFalse(path);
        }
    }

    /// <summary>Writes a race the API is known to allow to the test output; it is an observation, not a failure.</summary>
    protected static void NoteRace(string description)
    {
        TestContext.Current.TestOutputHelper?.WriteLine($"race: {description}");
    }
}
