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
/// Who calls a backup endpoint. The portal owner and a DocSpace admin may both manage backups, so the
/// functional tests run once as each.
/// </summary>
public enum BackupCaller
{
    Owner,
    DocSpaceAdmin
}

/// <summary>
/// The callers a functional backup test runs as, for <c>[MemberData]</c>.
/// </summary>
public static class BackupCallerData
{
    public static TheoryData<BackupCaller> All => [BackupCaller.Owner, BackupCaller.DocSpaceAdmin];
}

/// <summary>
/// What the functional backup suites share on top of <see cref="BackupTestBase"/>: acting as the owner or
/// as an invited DocSpace admin, creating a room to back up into, and starting a backup of any storage
/// type and waiting for exactly that job to finish. The test stack runs as a standalone installation, so
/// the free monthly allowance is not enforced and no wallet is involved.
/// </summary>
public abstract class BackupFunctionalTestBase(AspireAppFixture fixture) : BackupTestBase(fixture)
{
    private static readonly TimeSpan _finishTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Makes the backup and the files clients act as the owner, or as a DocSpace admin invited for the
    /// purpose, and returns that user.
    /// </summary>
    protected async Task<User> ActAsAsync(BackupCaller caller)
    {
        var user = caller == BackupCaller.Owner ? Owner : await InviteMember(EmployeeType.DocSpaceAdmin);

        await _backupClient.Authenticate(user);
        await _filesClient.Authenticate(user);

        return user;
    }

    /// <summary>Creates a custom room as the user the files client acts as and returns its id.</summary>
    protected async Task<int> CreateBackupRoomAsync(string title)
    {
        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>
    /// Sends startbackup and asserts the answer describes a backup job that has just been queued. The
    /// caller decides what to wait for.
    /// </summary>
    protected async Task<BackupProgressDto> StartBackupAsync(StartBackupRequestDto request)
    {
        var started = await _backupApi.StartBackupAsync(request, TestContext.Current.CancellationToken);

        started.StatusCode.Should().Be(200);
        started.Response.Should().NotBeNull();

        var job = started.Response;
        job.BackupProgressEnum.Should().Be(BackupProgressEnum.Backup);
        job.TenantId.Should().BeGreaterThan(0);
        job.TaskId.Should().NotBeNullOrEmpty();
        job.Error.Should().BeNullOrEmpty();

        return job;
    }

    /// <summary>
    /// Starts a backup of any storage type, asserts the answer and waits until exactly that job has
    /// finished without an error. <see cref="BackupTestBase.RunBackupAsync"/> does the same for DataStore
    /// only.
    /// </summary>
    protected async Task<string> StartBackupAndWaitAsync(StartBackupRequestDto request)
    {
        var job = await StartBackupAsync(request);

        await WaitForBackupToFinishAsync(job.TaskId);

        return job.TaskId;
    }

    /// <summary>
    /// Waits until the job is seen stopped in the queue, or its record is in the history: the queue may
    /// drop a finished job before the first poll sees it. A job that reports an error, or stops in any
    /// state but Completed, fails the test at once.
    /// </summary>
    protected async Task WaitForBackupToFinishAsync(string taskId)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _finishTimeout;

        for (var poll = 0; ; poll++)
        {
            var job = (await _backupApi.GetBackupProgressAsync(cancellationToken: ct)).Response;
            var lastJob = "none";

            if (job is not null && string.Equals(job.TaskId, taskId, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(job.Error))
                {
                    Assert.Fail($"backup {taskId} failed: {job.Error}");
                }

                if (HasStopped(job))
                {
                    job.Status.Should().Be(DistributedTaskStatus.Completed);

                    return;
                }

                lastJob = $"status={job.Status} progress={job.Progress}";
            }

            var history = (await _backupApi.GetBackupHistoryAsync(cancellationToken: ct)).Response ?? [];

            if (history.Any(record => string.Equals(record.Id.ToString(), taskId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"backup {taskId} did not finish in {_finishTimeout.TotalSeconds:0} s: queue {lastJob}; history [{string.Join(", ", history.Select(record => record.Id))}]");
            }

            await Task.Delay(Math.Min(500 * (poll + 1), 2_000), ct);
        }
    }

    /// <summary>The ids of the history records, as text.</summary>
    protected async Task<List<string>> ReadHistoryIdsAsync()
    {
        var history = (await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken)).Response ?? [];

        return history.Select(record => record.Id.ToString()).ToList();
    }
}
