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

namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// A dump of the whole server restored into the server it was taken of: startbackup and startrestore with
/// <c>dump</c>. The restore replaces the data of every portal of the stack, so the test runs only when
/// <see cref="RestoreGuard.ServerWideRestoreVariable"/> is 1, and then alone:
/// <c>DOCSPACE_TEST_SERVER_WIDE_RESTORE=1 dotnet test ... -- --filter-class "*DumpRoundTripTests"</c>, on each
/// database (<c>DB_PROVIDER</c>). A dump keeps the ids, so the test reads its own tenant from the database
/// afterwards: what the dump held is back, what came after it is gone.
/// </summary>
public class DumpRoundTripTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private const string KeptRoom = "Kept by the dump restore";
    private const string KeptFile = "Kept by the dump restore.txt";
    private const string LaterRoom = "Created after the dump";

    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(10);

    [Fact]
    public async Task StartRestore_ServerDump_BringsBackWhatTheDumpHeldAndDropsWhatCameLater()
    {
        Assert.SkipUnless(RestoreGuard.ServerWideRestoreAllowed,
            $"replaces the data of every portal of the stack: set {RestoreGuard.ServerWideRestoreVariable}=1 and run the class alone");

        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await ActAsAsync(BackupCaller.Owner);

        var keptRoomId = await CreateBackupRoomAsync(KeptRoom);
        await _filesApi.CreateTextFileAsync(keptRoomId, new CreateTextOrHtmlFileRequest(KeptFile, "kept content"), ct);

        var dump = await _backupApi.StartBackupAsync(new StartBackupRequestDto { StorageType = BackupStorageType.DataStore, Dump = true }, ct);
        var dumpId = await WaitForDumpAsync(dump.Response.TaskId);
        var quotaAtDump = await ReadQuotaRowsAsync();

        var laterRoomId = await CreateBackupRoomAsync(LaterRoom);

        // Act
        await StartServerWideRestoreAsync(dumpId);
        var finished = await WaitForDumpRestoreEndAsync();

        // Assert
        // null when the job was no longer seen; the rows below are the check then
        (finished?.Error).Should().BeNullOrEmpty("the restore ended with {0}", finished?.Error);

        await using var files = await Fixture.CreateDbContextAsync<FilesDbContext>(ct);
        var rooms = await files.Folders.Where(folder => folder.TenantId == TenantId && (folder.Id == keptRoomId || folder.Id == laterRoomId))
            .Select(folder => folder.Title).ToListAsync(ct);
        rooms.Should().Equal(KeptRoom);

        var fileTitles = await files.Files.Where(file => file.TenantId == TenantId && file.ParentId == keptRoomId).Select(file => file.Title).ToListAsync(ct);
        fileTitles.Should().Equal(KeptFile);

        // all portals, not this one only: a server-wide restore blocks every portal of the server while it runs
        await using var tenants = await Fixture.CreateDbContextAsync<TenantDbContext>(ct);
        var restoring = await tenants.Tenants.CountAsync(tenant => tenant.Status == ASC.Core.Tenants.TenantStatus.Restoring, ct);
        restoring.Should().Be(0, "a finished restore leaves no portal blocked");

        // the restore deletes the files of the server and brings back those of the dump, and neither may count
        // against the quota rows it restored (the files row of the kept file among them)
        quotaAtDump.Should().Contain(row => row.StartsWith("/files/", StringComparison.Ordinal));
        (await ReadQuotaRowsAsync()).Should().Equal(quotaAtDump);
    }

    private async Task<List<string>> ReadQuotaRowsAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        await using var core = await Fixture.CreateDbContextAsync<ASC.Core.Common.EF.CoreDbContext>(ct);

        // the row of the backup storage counts the archive of the dump itself, which is stored after the snapshot
        // the dump was read from, so the dump does not hold it
        return (await core.QuotaRows.Where(row => row.TenantId == TenantId && row.Path != "/backup/").ToListAsync(ct))
            .Select(row => $"{row.Path} {row.Tag} {row.UserId} {row.Counter}")
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // The dump is done when its record is in the server's dump history; a job that reports an error fails the test.
    private async Task<string> WaitForDumpAsync(string taskId)
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _timeout;

        while (true)
        {
            var history = (await _backupApi.GetBackupHistoryAsync(dump: true, cancellationToken: ct)).Response ?? [];
            if (history.Any(record => string.Equals(record.Id.ToString(), taskId, StringComparison.OrdinalIgnoreCase)))
            {
                return taskId;
            }

            var job = (await _backupApi.GetBackupProgressAsync(dump: true, cancellationToken: ct)).Response;
            if (!string.IsNullOrEmpty(job?.Error))
            {
                Assert.Fail($"the dump {taskId} failed: {job.Error}");
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the dump {taskId} is not in the history after {_timeout.TotalMinutes:0} min: job {job?.Status} {job?.Progress}");
            }

            await Task.Delay(2_000, ct);
        }
    }

    // Waits until the restore job has stopped and returns it, or null when it is no longer seen: the queue dropped
    // it, or the restored users table no longer takes the token of the test (401). While the server is restoring,
    // calls may be refused as well; the poll goes on.
    private async Task<BackupProgressDto?> WaitForDumpRestoreEndAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _timeout;
        var seen = false;
        var last = "nothing yet";

        while (true)
        {
            try
            {
                var job = (await _backupApi.GetRestoreProgressAsync(dump: true, cancellationToken: ct)).Response;

                if (job is null && seen)
                {
                    return null;
                }

                if (job is not null)
                {
                    seen = true;
                    last = $"status={job.Status} done={job.IsCompleted} progress={job.Progress} error={job.Error}";

                    if (HasStopped(job))
                    {
                        return job;
                    }
                }
            }
            catch (ApiException e) when (e.ErrorCode == 401 && seen)
            {
                return null;
            }
            catch (ApiException e)
            {
                last = $"answer {e.ErrorCode}";
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the restore did not stop in {_timeout.TotalMinutes:0} min: {last}");
            }

            await Task.Delay(1_000, ct);
        }
    }
}
