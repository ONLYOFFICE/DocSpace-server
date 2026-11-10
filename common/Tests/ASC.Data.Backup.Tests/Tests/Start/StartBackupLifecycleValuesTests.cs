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


namespace ASC.Data.Backup.Tests.Tests.Start;

/// <summary>
/// POST /api/2.0/backup/startbackup over the life of a job: following one to the end, starting while
/// another runs, two starts at once, starting after a failed job or a cancel, and the reads made while a
/// backup is being written. The queue holds one job per portal, so a second start normally lands on the
/// running job and answers with it; if the first one has already finished it is a new job. Both are valid,
/// and what must hold either way is one record per job and nothing stuck. The wallet of the portal is
/// never involved: the test stack runs as a standalone installation.
/// </summary>
public class StartBackupLifecycleValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    [Fact]
    public async Task StartBackup_FollowedToCompletion_LeavesItsRecordAndAnotherCanStart()
    {
        // Arrange
        var firstId = await StartDataStoreBackupAsync();

        // Act
        var job = await WaitForBackupJobEndAsync(firstId);

        // Assert
        job.Error.Should().BeNullOrEmpty();
        job.Status.Should().Be(DistributedTaskStatus.Completed);
        job.Progress.Should().Be(100);

        // the finished job stays in the queue unchanged, and its id is the record id
        for (var poll = 0; poll < 2; poll++)
        {
            var progress = await _backupApi.GetBackupProgressAsync(cancellationToken: TestContext.Current.CancellationToken);
            progress.Response.Should().BeEquivalentTo(job);
        }

        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(firstId));

        // a second backup is a new job with its own record
        var secondId = await StartDataStoreBackupAsync();
        Canonical(secondId).Should().NotBe(Canonical(firstId));

        await WaitForBackupJobEndAsync(secondId);
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(firstId, secondId));
        Total(await ReadCountsAsync()).Should().Be(2);
    }

    [Fact]
    public async Task StartBackup_SecondStartWhileTheFirstRuns_DoesNotCreateASecondArchive()
    {
        // Act
        var firstId = await StartDataStoreBackupAsync();
        var secondId = await StartDataStoreBackupAsync();

        // Assert
        var ids = SortedIds(firstId, secondId);
        NoteRace(ids.Length == 1 ? "overlap: the second start landed on the running job" : "overlap: the first backup had finished before the second start");

        foreach (var id in ids)
        {
            await WaitForBackupRecordAsync(id);
        }

        (await ListHistoryIdsAsync()).Should().Equal(ids);
        Total(await ReadCountsAsync()).Should().Be(ids.Length);
        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task StartBackup_TwoConcurrentStarts_LeaveOneRecordPerJob()
    {
        // Act
        var started = await Task.WhenAll(StartDataStoreBackupAsync(), StartDataStoreBackupAsync());

        // Assert
        var ids = SortedIds(started);

        foreach (var id in ids)
        {
            await WaitForBackupRecordAsync(id);
        }

        (await ListHistoryIdsAsync()).Should().Equal(ids);
        Total(await ReadCountsAsync()).Should().Be(ids.Length);
        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task StartBackup_AfterAFailedJob_WorksAndLeavesTheEarlierCopyUntouched()
    {
        // Arrange
        var earlierId = await RunBackupAsync();
        var before = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        var earlier = before.Response.Single(record => record.Id == Guid.Parse(earlierId));

        // a job that fails: the module of the storage is missing
        var failedStart = await _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.ThirdPartyConsumer }, TestContext.Current.CancellationToken);
        failedStart.StatusCode.Should().Be(200);

        var failed = await WaitForBackupJobEndAsync(failedStart.Response.TaskId);
        failed.Error.Should().NotBeNullOrEmpty();

        // the failure did not touch the earlier copy and left no record
        var afterFailure = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        afterFailure.Response.Should().BeEquivalentTo(new[] { earlier });

        // Act
        var laterId = await RunBackupAsync();

        // Assert - the failed job made no archive and is not counted
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(earlierId, laterId));
        Total(await ReadCountsAsync()).Should().Be(2);
        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task StartBackup_ReadsDuringTheBackup_StayConsistentAndTheNewBackupIsCountedOnceItIsDone()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var earlierId = await RunBackupAsync();
        var taskId = await StartDataStoreBackupAsync();

        // Act
        var historyTask = _backupApi.GetBackupHistoryAsync(cancellationToken: ct);
        var progressTask = _backupApi.GetBackupProgressAsync(cancellationToken: ct);
        var countTask = _backupApi.GetBackupsCountAsync(cancellationToken: ct);
        var countsTask = _backupApi.GetBackupsCountsAsync(cancellationToken: ct);
        var scheduleTask = _backupApi.GetBackupScheduleAsync(cancellationToken: ct);
        var serviceTask = _backupApi.GetBackupsServiceStateAsync(ct);

        await Task.WhenAll(historyTask, progressTask, countTask, countsTask, scheduleTask, serviceTask);

        // Assert - what the reads show is one of the two valid moments: the record appears when the backup
        // is done, so the history holds the earlier copy and perhaps the new one, never anything else
        var historyIds = (await historyTask).Response.Select(record => record.Id.ToString()).ToList();
        historyIds.Should().Contain(Canonical(earlierId));
        historyIds.Should().OnlyContain(id => id == Canonical(earlierId) || id == Canonical(taskId));

        var job = (await progressTask).Response;
        (job is null || Canonical(job.TaskId) == Canonical(earlierId) || Canonical(job.TaskId) == Canonical(taskId)).Should().BeTrue();

        Total((await countsTask).Response).Should().BeOneOf(1, 2);

        // after the backup the counts and the history agree
        await WaitForBackupRecordAsync(taskId);

        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(earlierId, taskId));
        Total(await ReadCountsAsync()).Should().Be(2);
    }

    [Fact]
    public async Task StartBackup_RightAfterACancel_CompletesAndHasItsRecord()
    {
        // Arrange
        var canceledId = await StartDataStoreBackupAsync();

        var canceled = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);
        canceled.Response.Should().BeTrue();

        // Act
        var nextId = await StartDataStoreBackupAsync();

        // Normally this is a new job. When the canceled job had not reached the worker yet, the cancel
        // dropped nothing real and the start lands on that very job, which then runs to the end. Either
        // way the backup that was asked for must complete and have its record; the second way is noted.
        var sameJob = Canonical(nextId) == Canonical(canceledId);

        if (sameJob)
        {
            NoteRace("cancelbackup answered true, then the start returned the canceled job and it ran to the end");
        }

        await WaitForBackupRecordAsync(nextId);

        // Assert
        var ids = await ListHistoryIdsAsync();
        ids.Should().Contain(Canonical(nextId));

        if (!sameJob && ids.Contains(Canonical(canceledId)))
        {
            NoteRace("the canceled job ran to the end and was archived too");
            ids.Should().Equal(SortedIds(canceledId, nextId));
        }
        else
        {
            ids.Should().Equal(SortedIds(nextId));
        }

        await ExpectQueueNotStuckAsync();
    }
}
