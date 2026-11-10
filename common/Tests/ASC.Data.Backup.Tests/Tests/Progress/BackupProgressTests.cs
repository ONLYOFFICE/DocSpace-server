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
/// GET /api/2.0/backup/getbackupprogress before a backup and after it has finished. The TypeScript check
/// of a job caught while Running is not ported: a backup of a fresh portal on this stack finishes before
/// any poll can see it running.
/// </summary>
public class BackupProgressTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    [Fact]
    public async Task GetBackupProgress_NoBackupStarted_ReturnsNoJob()
    {
        // Act
        var progress = await _backupApi.GetBackupProgressAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        progress.StatusCode.Should().Be(200);
        progress.Count.Should().Be(0);
        progress.Response.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task GetBackupProgress_BackupCompleted_ReturnsFinishedJob(BackupCaller caller)
    {
        // Arrange
        await ActAsAsync(caller);
        var started = await StartBackupAsync(new StartBackupRequestDto { StorageType = BackupStorageType.DataStore });

        // Act
        var job = await WaitForBackupJobEndAsync(started.TaskId);

        // Assert
        job.BackupProgressEnum.Should().Be(BackupProgressEnum.Backup);
        job.IsCompleted.Should().BeTrue();
        job.Progress.Should().Be(100);
        job.Status.Should().Be(DistributedTaskStatus.Completed);
        job.TenantId.Should().BeGreaterThan(0);
        job.TaskId.Should().Be(started.TaskId);
        job.Error.Should().BeNullOrEmpty();
    }
}
