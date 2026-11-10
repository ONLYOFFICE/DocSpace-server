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


namespace ASC.Data.Backup.Tests.Tests.Counts;

/// <summary>
/// GET /api/2.0/backup/getbackupscount and getbackupscountbypaid: with no period given they count the
/// current calendar month in UTC.
/// </summary>
public class GetBackupsCountValuesTests(
    AspireAppFixture fixture)
    : BackupLifecycleTestBase(fixture)
{
    [Fact]
    public async Task GetBackupsCount_NoPeriod_CountsTheCurrentCalendarMonthInUtc()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await RunBackupAsync();

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var tomorrow = now.AddDays(1);

        // Act
        var byDefault = await _backupApi.GetBackupsCountAsync(cancellationToken: ct);
        var explicitPeriod = await _backupApi.GetBackupsCountAsync(monthStart, tomorrow, cancellationToken: ct);
        var countsByDefault = await _backupApi.GetBackupsCountsAsync(cancellationToken: ct);
        var countsExplicit = await _backupApi.GetBackupsCountsAsync(monthStart, tomorrow, cancellationToken: ct);

        // Assert
        byDefault.StatusCode.Should().Be(200);
        byDefault.Count.Should().Be(1);
        byDefault.Response.Should().Be(1);
        explicitPeriod.Response.Should().Be(byDefault.Response);
        countsExplicit.Response.Should().BeEquivalentTo(countsByDefault.Response);
    }
}
