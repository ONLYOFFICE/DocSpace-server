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

namespace ASC.Data.Backup.Tests.Tests.History;

/// <summary>
/// The archive names of backups made back to back. A backup of a fresh portal finishes in well under a
/// second, so consecutive backups start within the same second - the resolution of the timestamp in
/// the archive name.
/// </summary>
/// <remarks>
/// Such backups used to get the same name and so the same path in the storage: the later archive
/// overwrote the earlier one, deleting one backup deleted the file of the other, and the history then
/// dropped the record whose file was gone.
/// </remarks>
public class BackupFileNameTests(
    AspireAppFixture fixture)
    : BackupTestBase(fixture)
{
    [Fact]
    public async Task StartBackup_BackToBack_GivesEveryArchiveItsOwnName()
    {
        // Arrange
        var ids = await StartBackToBackAsync(3);

        // Act
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var records = history.Response.Where(record => ids.Contains(record.Id)).ToList();
        records.Should().HaveCount(3, "every backup has to stay in the history");
        records.Select(record => record.FileName).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task DeleteBackup_OfBackToBackBackups_KeepsTheOthers()
    {
        // Arrange
        var ids = await StartBackToBackAsync(3);

        // Act
        await _backupApi.DeleteBackupAsync(ids[1], TestContext.Current.CancellationToken);

        // Assert
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        history.Response.Select(record => record.Id).Should().BeEquivalentTo([ids[0], ids[2]]);
    }

    /// <summary>
    /// Makes the given number of DataStore backups, each started the moment the previous one has
    /// finished, and waits until all of them are in the history. A portal runs one backup at a time:
    /// while a backup is still going, startbackup answers with that same job, so the loop keeps asking
    /// until a new task id comes back.
    /// </summary>
    private async Task<List<Guid>> StartBackToBackAsync(int count)
    {
        var ct = TestContext.Current.CancellationToken;
        var request = new StartBackupRequestDto { StorageType = BackupStorageType.DataStore };
        var ids = new List<Guid>();
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (ids.Count < count)
        {
            var id = Guid.Parse((await _backupApi.StartBackupAsync(request, ct)).Response.TaskId);

            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
            else if (DateTime.UtcNow >= deadline)
            {
                Assert.Fail($"only {ids.Count} of {count} backups started in 60 s: [{string.Join(", ", ids)}]");
            }
            else
            {
                await Task.Delay(20, ct);
            }
        }

        foreach (var id in ids)
        {
            await WaitForBackupRecordAsync(id.ToString());
        }

        return ids;
    }
}
