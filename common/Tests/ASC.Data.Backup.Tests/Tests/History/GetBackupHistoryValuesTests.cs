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
/// GET /api/2.0/backup/getbackuphistory: the records of finished backups follow the contract.
/// </summary>
public class GetBackupHistoryValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    // The server writes UTC times without a zone designator, which the client reads as unspecified.
    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
    }

    [Fact]
    public async Task GetBackupHistory_TwoBackups_ListsTwoRecordsThatFollowTheContract()
    {
        // Arrange
        var taskIds = new[] { await RunBackupAsync(), await RunBackupAsync() };

        // Act
        var answer = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        answer.StatusCode.Should().Be(200);
        answer.Count.Should().Be(2);
        answer.Response.Should().HaveCount(2);

        // The order is not specified, so the records are matched by id.
        answer.Response.Select(record => record.Id.ToString()).Order(StringComparer.Ordinal).Should().Equal(SortedIds(taskIds));

        foreach (var record in answer.Response)
        {
            record.Id.Should().NotBe(Guid.Empty);
            record.FileName.Should().NotBeNullOrEmpty();
            record.StorageType.Should().Be(BackupStorageType.DataStore);
            AsUtc(record.ExpiresOn).Should().BeAfter(AsUtc(record.CreatedOn));

            // The record was made moments ago, so a zone mistake or a default date would be far from now.
            (DateTime.UtcNow - AsUtc(record.CreatedOn)).Duration().Should().BeLessThan(TimeSpan.FromHours(24));
        }
    }
}
