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
/// GET /api/2.0/backup/getbackuphistory: the list of finished backups of the portal.
/// </summary>
public class BackupHistoryTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    [Fact]
    public async Task GetBackupHistory_FreshPortal_ReturnsEmptyList()
    {
        // Act
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        history.StatusCode.Should().Be(200);
        history.Response.Should().BeEmpty();
        history.Count.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task GetBackupHistory_AfterBackup_ReturnsRecordWithAllFields(BackupCaller caller)
    {
        // Arrange
        await ActAsAsync(caller);
        var taskId = await RunBackupAsync();

        // Act
        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        history.StatusCode.Should().Be(200);

        var record = history.Response
            .Should().ContainSingle(r => string.Equals(r.Id.ToString(), taskId, StringComparison.OrdinalIgnoreCase)).Subject;
        record.Id.Should().NotBe(Guid.Empty);
        record.StorageType.Should().Be(BackupStorageType.DataStore);
        record.CreatedOn.Should().NotBe(DateTime.MinValue);
        record.ExpiresOn.Should().NotBe(DateTime.MinValue);
    }

    [Fact]
    public async Task GetBackupHistory_SecondBackup_IncreasesCountByOne()
    {
        // Arrange
        await RunBackupAsync();

        var afterFirst = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        afterFirst.Count.Should().BeGreaterThan(0);

        await RunBackupAsync();

        // Act
        var afterSecond = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        afterSecond.StatusCode.Should().Be(200);
        afterSecond.Count.Should().Be(afterFirst.Count + 1);
    }
}
