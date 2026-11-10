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


namespace ASC.Data.Backup.Tests.Tests.Delete;

/// <summary>
/// DELETE /api/2.0/backup/deletebackup/{id} at the same time as other work: while a new backup is being
/// made, and as concurrent deletions of different copies and of one copy twice.
/// </summary>
public class DeleteBackupConcurrencyValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    [Fact]
    public async Task DeleteBackup_WhileANewBackupIsMade_LeavesTheNewOneIntact()
    {
        // Arrange
        var keptId = await RunBackupAsync();
        var deletedId = await RunBackupAsync();

        var started = await _backupApi.StartBackupAsync(
            new StartBackupRequestDto { StorageType = BackupStorageType.DataStore }, TestContext.Current.CancellationToken);
        var newId = started.Response.TaskId;

        // Act
        var deleted = await _backupApi.DeleteBackupAsync(Guid.Parse(deletedId), TestContext.Current.CancellationToken);

        // Assert
        deleted.StatusCode.Should().Be(200);
        deleted.Response.Should().BeTrue();

        await WaitForBackupRecordAsync(newId);

        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(keptId, newId));
        await ExpectQueueNotStuckAsync();
    }

    [Fact]
    public async Task DeleteBackup_ConcurrentDeletes_EndWithTheSameHistory()
    {
        // Arrange
        var keptId = await RunBackupAsync();
        var pairA = await RunBackupAsync();
        var pairB = await RunBackupAsync();
        var twiceId = await RunBackupAsync();

        // Act - two different copies at once
        var different = await Task.WhenAll(
            _backupApi.DeleteBackupAsync(Guid.Parse(pairA), TestContext.Current.CancellationToken),
            _backupApi.DeleteBackupAsync(Guid.Parse(pairB), TestContext.Current.CancellationToken));

        // Assert
        different.Should().OnlyContain(answer => answer.StatusCode == 200 && answer.Response);
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(keptId, twiceId));

        // Act - one copy twice at once
        var same = await Task.WhenAll(
            _backupApi.DeleteBackupAsync(Guid.Parse(twiceId), TestContext.Current.CancellationToken),
            _backupApi.DeleteBackupAsync(Guid.Parse(twiceId), TestContext.Current.CancellationToken));

        // Assert
        same.Should().OnlyContain(answer => answer.StatusCode == 200 && answer.Response);
        (await ListHistoryIdsAsync()).Should().Equal(SortedIds(keptId));
    }
}
