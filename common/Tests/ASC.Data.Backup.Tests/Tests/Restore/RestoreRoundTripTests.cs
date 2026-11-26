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
/// A portal backup restored into the portal it was taken of: POST /api/2.0/backup/startrestore with a backup
/// of the test's own history. The restore loads every table of the archive into a new tenant, maps the ids
/// of its rows and moves the portal's address to it; the test reads in that tenant what the archive held and
/// what it did not. It is the test of the suite whose restore of a portal really queues; it goes
/// through <see cref="RestoreGuard.SendOwnPortalRestoreAsync{T}"/>, which lets nothing server-wide and no
/// other portal through (the server-wide restore is <c>DumpRoundTripTests</c>, behind its own variable).
/// </summary>
public class RestoreRoundTripTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    private const string KeptRoom = "Kept by the restore";
    private const string KeptFile = "Kept by the restore.txt";
    private const string LaterRoom = "Created after the backup";

    private static readonly TimeSpan _restoreTimeout = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task StartRestore_OwnPortalBackup_BringsBackWhatTheArchiveHeldAndDropsWhatCameLater()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await ActAsAsync(BackupCaller.Owner);

        var keptRoomId = await CreateBackupRoomAsync(KeptRoom);
        await _filesApi.CreateTextFileAsync(keptRoomId, new CreateTextOrHtmlFileRequest(KeptFile, "kept content"), ct);

        var backupId = await RunBackupAsync();

        await CreateBackupRoomAsync(LaterRoom);

        // Act: startrestore answers with the restore the worker holds at that moment, which may not be this one
        // yet (the request reaches the worker over the event bus), so the answer is not checked
        await StartOwnPortalRestoreAsync(backupId);
        var finished = await WaitForRestoreEndAsync();

        // Assert
        // null when the job was no longer seen once the portal moved; the rows below are the check then
        (finished?.Error).Should().BeNullOrEmpty("the restore ended with {0}", finished?.Error);

        // A restored portal takes no password until its users set a new one from the letter the restore sends
        // (RestoreCompletedV115NotifyAction, a PasswordChange link), and this stack delivers no letters, so what
        // the restore loaded is read from the database: the tenant that has the portal's address now.
        await using var tenants = await Fixture.CreateDbContextAsync<TenantDbContext>(ct);
        var restoredTenantId = await tenants.Tenants.Where(tenant => tenant.Alias == PortalName).Select(tenant => tenant.Id).SingleAsync(ct);
        restoredTenantId.Should().NotBe(TenantId, "the restore loads the archive into a new tenant");

        await using var files = await Fixture.CreateDbContextAsync<FilesDbContext>(ct);
        var titles = await files.Folders.Where(folder => folder.TenantId == restoredTenantId).Select(folder => folder.Title).ToListAsync(ct);
        titles.Should().Contain(KeptRoom).And.NotContain(LaterRoom);

        var restoredRoomId = await files.Folders.Where(folder => folder.TenantId == restoredTenantId && folder.Title == KeptRoom).Select(folder => folder.Id).SingleAsync(ct);
        var fileTitles = await files.Files.Where(file => file.TenantId == restoredTenantId && file.ParentId == restoredRoomId).Select(file => file.Title).ToListAsync(ct);
        fileTitles.Should().Equal(KeptFile);
    }

    // Waits until the restore job has stopped and returns it, or null when the job is no longer seen: the queue
    // dropped it, or the portal moved to its new tenant, where the sign-in of the old one is refused (401).
    // While the portal is restoring, its calls may be refused as well; the poll goes on.
    private async Task<BackupProgressDto?> WaitForRestoreEndAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var deadline = DateTime.UtcNow + _restoreTimeout;
        var seen = false;
        var last = "nothing yet";

        while (true)
        {
            try
            {
                var job = (await _backupApi.GetRestoreProgressAsync(cancellationToken: ct)).Response;

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
            catch (ApiException e) when (e.ErrorCode == 401)
            {
                // the token of the test is refused only once the portal has moved to its new tenant
                return null;
            }
            catch (ApiException e)
            {
                last = $"answer {e.ErrorCode}";
            }

            if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"the restore did not stop in {_restoreTimeout.TotalMinutes:0} min: {last}");
            }

            await Task.Delay(1_000, ct);
        }
    }
}
