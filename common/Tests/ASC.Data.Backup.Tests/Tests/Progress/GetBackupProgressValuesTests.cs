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


namespace ASC.Data.Backup.Tests.Tests.Progress;

/// <summary>
/// GET /api/2.0/backup/getbackupprogress over the life of one job: every state seen while it runs agrees
/// with the others and with the answer of the start, the finished job links to the archive listed in the
/// history, and a job that fails reports it in <c>error</c> and leaves no archive.
/// </summary>
public class GetBackupProgressValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    private static readonly TimeSpan _jobTimeout = TimeSpan.FromSeconds(60);

    [Fact]
    public async Task GetBackupProgress_RunningBackup_StatesAreConsistentAndTheFinishedJobLinksToTheArchive()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;

        // Act
        var started = await _backupApi.StartBackupAsync(new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, ct);
        var job = started.Response;
        var taskId = job.TaskId;

        // Assert - the start response describes the job of this portal. The job is Created until the worker
        // picks it up, which is what the start normally reports, but the response shows the queue as it is
        // when the answer is built, so a worker that was quick can already have it Running.
        Guid.TryParse(taskId, out _).Should().BeTrue();
        job.BackupProgressEnum.Should().Be(BackupProgressEnum.Backup);
        job.Error.Should().BeNullOrEmpty();
        job.Progress.Should().BeInRange(0, 100);
        new[] { DistributedTaskStatus.Created, DistributedTaskStatus.Running, DistributedTaskStatus.Completed }
            .Should().Contain(job.Status!.Value);
        job.IsCompleted.Should().Be(job.Status == DistributedTaskStatus.Completed);

        // poll until the job completes, keeping every state seen. Done is isCompleted together with the
        // Completed status: for a moment the progress can show the first without the second.
        var seen = new List<BackupProgressDto>();
        var deadline = DateTime.UtcNow + _jobTimeout;

        while (true)
        {
            var answer = await _backupApi.GetBackupProgressAsync(cancellationToken: ct);

            if (answer.Response is not null)
            {
                answer.StatusCode.Should().Be(200);
                answer.Count.Should().Be(1);
                seen.Add(answer.Response);

                if (answer.Response.IsCompleted && answer.Response.Status == DistributedTaskStatus.Completed)
                {
                    break;
                }
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"backup {taskId} did not complete in {_jobTimeout.TotalSeconds:0} s: {seen.Count} states seen, the last ones {string.Join(" | ", seen.TakeLast(5).Select(state => $"{state.Status}/{state.Progress}%"))}");
            }

            await Task.Delay(250, ct);
        }

        // every state agrees with the others
        var previous = 0;

        foreach (var (state, index) in seen.Select((item, position) => (item, position)))
        {
            var at = $"state {index} ({state.Status}, {state.Progress}%)";

            Canonical(state.TaskId).Should().Be(Canonical(taskId), at);
            state.TenantId.Should().Be(job.TenantId, at);
            state.BackupProgressEnum.Should().Be(BackupProgressEnum.Backup, at);
            state.Progress.Should().BeInRange(0, 100, at);
            state.Progress.Should().BeGreaterThanOrEqualTo(previous, at);
            previous = state.Progress;

            if (state.IsCompleted)
            {
                state.Progress.Should().Be(100, at);

                if (state.Status != DistributedTaskStatus.Completed)
                {
                    // A documented observation, not an asserted defect: for a moment the progress can show
                    // isCompleted before the status follows, which is what eventual consistency of the
                    // reads allows, and the contract does not ask for an atomic snapshot of the job. The
                    // state must still be Running, and the polling above requires the job to end as
                    // Completed; the moment is only noted.
                    state.Status.Should().Be(DistributedTaskStatus.Running, at);
                    TestContext.Current.TestOutputHelper?.WriteLine("observation: getbackupprogress showed isCompleted true while status was still Running (transient, eventual consistency)");
                }
            }
            else
            {
                // A job is Created until the worker picks it up, then Running.
                new[] { DistributedTaskStatus.Created, DistributedTaskStatus.Running }.Should().Contain(state.Status!.Value, at);
                state.Error.Should().BeNullOrEmpty(at);
                state.Link.Should().BeNullOrEmpty(at);
            }
        }

        // the finished job links to the archive listed in the history
        var finished = seen[^1];
        finished.Error.Should().BeNullOrEmpty();
        finished.Link.Should().NotBeNull();

        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: ct);
        var record = history.Response.Single(item => item.Id == Guid.Parse(taskId));
        record.FileName.Should().NotBeNullOrEmpty();
        finished.Link.Should().Contain(record.FileName);
    }

    [Fact]
    public async Task GetBackupProgress_JobThatFails_ReportsItInErrorAndLeavesNoArchive()
    {
        // Arrange
        var started = await _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.ThirdPartyConsumer }, TestContext.Current.CancellationToken);
        var taskId = started.Response.TaskId;

        // Act
        var job = await WaitForBackupJobEndAsync(taskId);

        // Assert
        job.IsCompleted.Should().BeTrue();
        job.Error.Should().NotBeNullOrEmpty();
        job.Link.Should().BeNullOrEmpty();
        Canonical(job.TaskId).Should().Be(Canonical(taskId));
        job.BackupProgressEnum.Should().Be(BackupProgressEnum.Backup);

        (await ListHistoryIdsAsync()).Should().BeEmpty();
    }
}
