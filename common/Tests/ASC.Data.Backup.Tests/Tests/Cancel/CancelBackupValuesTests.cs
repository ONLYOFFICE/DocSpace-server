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


namespace ASC.Data.Backup.Tests.Tests.Cancel;

/// <summary>
/// POST /api/2.0/backup/cancelbackup: with no job, after a job finished, right after a start, twice in a
/// row, twice at once and at the same time as a start. The backup worker picks a job up from an event, so
/// a cancel can arrive before the job exists; the end state of a canceled job is therefore observed until
/// the queue is quiet (see <see cref="BackupLifecycleTestBase.ExpectCanceledJobSettledAsync"/>) and the
/// races the API allows are noted in the test output. The wallet of the portal is never involved: the test
/// stack runs as a standalone installation.
/// </summary>
public class CancelBackupValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    [Fact]
    public async Task CancelBackup_NoJob_AnswersFalseAndDoesNotBlockTheQueue()
    {
        // Act
        var canceled = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        canceled.StatusCode.Should().Be(200);
        canceled.Response.Should().BeFalse();

        var progress = await _backupApi.GetBackupProgressAsync(cancellationToken: TestContext.Current.CancellationToken);
        progress.Response.Should().BeNull();

        var taskId = await RunBackupAsync();
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(taskId));
    }

    [Fact]
    public async Task CancelBackup_AfterTheBackupFinished_DropsTheFinishedJobAndKeepsTheArchive()
    {
        // Arrange
        var taskId = await RunBackupAsync();
        var before = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act - the finished job is still in the queue, so cancel drops it
        var canceled = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        canceled.StatusCode.Should().Be(200);
        canceled.Response.Should().BeTrue();

        var progress = await _backupApi.GetBackupProgressAsync(cancellationToken: TestContext.Current.CancellationToken);
        progress.Response.Should().BeNull();

        // the archive and the counts are untouched
        var after = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        after.Response.Should().BeEquivalentTo(before.Response);
        Total(await ReadCountsAsync()).Should().Be(1);

        // nothing is left to cancel
        var again = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);
        again.Response.Should().BeFalse();

        // a new backup starts
        var nextId = await RunBackupAsync();
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(taskId, nextId));
    }

    [Fact]
    public async Task CancelBackup_RightAfterTheStart_LeavesNoArchiveKeepsTheEarlierCopyAndFreesTheQueue()
    {
        // Arrange
        var earlierId = await RunBackupAsync();
        var before = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        var canceledId = await StartDataStoreBackupAsync();
        Canonical(canceledId).Should().NotBe(Canonical(earlierId));

        // Act
        var canceled = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        canceled.StatusCode.Should().Be(200);
        canceled.Response.Should().BeTrue();

        // the canceled job settles without an archive
        await ExpectCanceledJobSettledAsync(canceledId, true);

        // the earlier copy is untouched
        var after = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var record in before.Response)
        {
            after.Response.Should().ContainEquivalentOf(record);
        }

        // the queue is free: a new backup completes
        var nextId = await RunBackupAsync();
        var ids = await ListHistoryIdsAsync();
        ids.Should().Contain(Canonical(earlierId));
        ids.Should().Contain(Canonical(nextId));
        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task CancelBackup_TwiceInARow_LeavesTheQueueConsistent()
    {
        // Arrange
        var taskId = await StartDataStoreBackupAsync();

        // Act
        var first = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);
        var second = await _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        // Assert - the second answer is true when the canceled job was still in the queue to be dropped, false
        // when the first cancel had already removed it: both are the contract
        first.StatusCode.Should().Be(200);
        first.Response.Should().BeTrue();
        second.StatusCode.Should().Be(200);

        await ExpectCanceledJobSettledAsync(taskId, first.Response, second.Response);

        var nextId = await RunBackupAsync();
        (await ListHistoryIdsAsync()).Should().Contain(Canonical(nextId));
    }

    [Fact]
    public async Task CancelBackup_TwoConcurrentCancels_BothAnswerOkAndOneOfThemTookTheJob()
    {
        // Arrange
        var taskId = await StartDataStoreBackupAsync();

        // Act
        var answers = await Task.WhenAll(
            _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken),
            _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken));

        // Assert - which one wins is not defined, but a job existed, so one of them dropped it
        answers.Should().OnlyContain(answer => answer.StatusCode == 200);
        answers.Select(answer => answer.Response).Should().Contain(true);

        await ExpectCanceledJobSettledAsync(taskId, answers.Select(answer => answer.Response).ToArray());

        var nextId = await RunBackupAsync();
        (await ListHistoryIdsAsync()).Should().Contain(Canonical(nextId));
    }

    [Fact]
    public async Task CancelBackup_ConcurrentWithTheStart_LeavesAJobThatIsEitherCanceledOrArchivedNeverStuck()
    {
        // Act
        var startTask = _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, TestContext.Current.CancellationToken);
        var cancelTask = _backupApi.CancelBackupAsync(TestContext.Current.CancellationToken);

        await Task.WhenAll(startTask, cancelTask);

        var start = await startTask;
        var cancel = await cancelTask;
        var taskId = start.Response.TaskId;

        // Assert - the cancel can arrive before the job exists. Then it answers false and the backup runs to
        // the end; otherwise it answers true and the job is canceled.
        start.StatusCode.Should().Be(200);
        cancel.StatusCode.Should().Be(200);

        var observation = await ObserveJobUntilQuiescentAsync(taskId);
        var path = string.Join(" -> ", observation.States);
        observation.Settled.Should().BeTrue($"the queue did not settle for {taskId}: {path}");

        var archived = (await ListHistoryIdsAsync()).Contains(Canonical(taskId));
        var final = observation.Final;
        var ours = final is not null && string.Equals(final.TaskId, taskId, StringComparison.OrdinalIgnoreCase);

        if (ours && final!.Status == DistributedTaskStatus.Completed)
        {
            archived.Should().BeTrue(path);
        }
        else
        {
            archived.Should().BeFalse(path);

            if (ours)
            {
                final!.Status.Should().Be(DistributedTaskStatus.Canceled, path);
            }
        }

        if (!cancel.Response)
        {
            archived.Should().BeTrue("a cancel that found no job cannot stop the backup");
        }

        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task CancelBackup_ManualBackupsCancelsAndDeletions_LeaveTheScheduleAsItWas()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = 6,
                CronParams = new BackupCronRequest(BackupPeriod.EveryWeek, 11, 4)
            },
            ct);
        var scheduled = await _backupApi.GetBackupScheduleAsync(cancellationToken: ct);

        // Act
        var kept = await RunBackupAsync();
        await StartDataStoreBackupAsync();
        await _backupApi.CancelBackupAsync(ct);
        await _backupApi.CancelBackupAsync(ct);
        await _backupApi.DeleteBackupAsync(Guid.Parse(kept), ct);
        await RunBackupAsync();

        // Assert
        var after = await _backupApi.GetBackupScheduleAsync(cancellationToken: ct);
        after.Response.Should().BeEquivalentTo(scheduled.Response);
        await ExpectQueueNotStuckAsync();
    }
}
