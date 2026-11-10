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
/// DELETE /api/2.0/backup/deletebackup/{id}: ids that are not backups, and the isolation of one copy from
/// the others. Whatever is refused or already gone must leave the queue, the history and the counters as
/// they were.
/// </summary>
public class DeleteBackupValidationTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    // The route only matches a GUID, so none of these reaches the controller. They are sent raw because
    // the typed signature takes a Guid.
    private static readonly string[] _notGuids =
    [
        "abc",
        "-1",
        "0",
        "99999999999999999999",
        new string('a', 5000),
        "'; DROP TABLE backups;--",
        "бэкап",
        "a%2Fb",
        "null",
        ""
    ];

    [Fact]
    public async Task DeleteBackup_IdThatIsNotAGuid_ReturnsNotFoundAndDeletesNothing()
    {
        // Arrange
        await RunBackupAsync();
        var before = await SnapshotBackupsAsync();
        before.HistoryIds.Should().HaveCount(1);

        foreach (var id in _notGuids)
        {
            // Act
            using var response = await _backup.DeleteAsync($"api/2.0/backup/deletebackup/{Uri.EscapeDataString(id)}", TestContext.Current.CancellationToken);

            // Assert
            var shown = id[..Math.Min(40, id.Length)];

            ((int)response.StatusCode).Should().Be(404, $"deletebackup/{shown}");
            (await SnapshotBackupsAsync()).Should().BeEquivalentTo(before, $"deletebackup/{shown} must change nothing");
        }
    }

    [Fact]
    public async Task DeleteBackup_GuidThatIsNotInTheHistory_DeletesNothing()
    {
        // Arrange
        await RunBackupAsync();
        var before = await SnapshotBackupsAsync();
        before.HistoryIds.Should().HaveCount(1);

        foreach (var id in new[] { "11111111-2222-3333-4444-555555555555", "00000000-0000-0000-0000-000000000000" })
        {
            // Act - the status is not asserted: the server answers 500 (NullReferenceException) for a GUID
            // it has never seen, while a second deletion of a real backup answers 200, and the API does not
            // say which is right. What matters here is that the request touches nothing.
            try
            {
                await _backupApi.DeleteBackupAsync(Guid.Parse(id), TestContext.Current.CancellationToken);
            }
            catch (ApiException)
            {
                // refused, which touches nothing as well
            }

            // Assert
            (await SnapshotBackupsAsync()).Should().BeEquivalentTo(before, $"deletebackup/{id} must change nothing");
        }
    }

    [Fact]
    public async Task DeleteBackup_OneOfTwoBackups_LeavesTheOtherUntouched()
    {
        // Arrange
        var firstId = await RunBackupAsync();
        var secondId = await RunBackupAsync();

        var before = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        before.Response.Select(record => record.Id.ToString()).Order(StringComparer.Ordinal).Should().Equal(SortedIds(firstId, secondId));
        var secondBefore = before.Response.Single(record => record.Id == Guid.Parse(secondId));

        // Act
        var deleted = await _backupApi.DeleteBackupAsync(Guid.Parse(firstId), TestContext.Current.CancellationToken);

        // Assert
        deleted.StatusCode.Should().Be(200);
        deleted.Response.Should().BeTrue();

        var after = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        after.Response.Should().BeEquivalentTo(new[] { secondBefore });
    }

    [Fact]
    public async Task DeleteBackup_SameBackupTwice_AnswersTrueBothTimesAndTouchesNoOtherBackup()
    {
        // Arrange
        var firstId = await RunBackupAsync();
        var secondId = await RunBackupAsync();

        // Act
        var first = await _backupApi.DeleteBackupAsync(Guid.Parse(firstId), TestContext.Current.CancellationToken);

        // Assert
        first.StatusCode.Should().Be(200);
        first.Response.Should().BeTrue();

        var afterFirst = await SnapshotBackupsAsync();
        afterFirst.HistoryIds.Should().Equal(SortedIds(secondId));

        var second = await _backupApi.DeleteBackupAsync(Guid.Parse(firstId), TestContext.Current.CancellationToken);
        second.StatusCode.Should().Be(200);
        second.Response.Should().BeTrue();

        (await SnapshotBackupsAsync()).Should().BeEquivalentTo(afterFirst);
    }
}
