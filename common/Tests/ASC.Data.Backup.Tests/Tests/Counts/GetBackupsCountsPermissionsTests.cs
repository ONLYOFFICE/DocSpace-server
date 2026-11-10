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

/// <summary>GET /api/2.0/backup/getbackupscountbypaid: the counters are visible to the portal administrators only.</summary>
public class GetBackupsCountsPermissionsTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    [Fact]
    public async Task GetBackupsCounts_Anonymous_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => _backupApi.GetBackupsCountsAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetBackupsCounts_DocSpaceAdmin_ReadsTheCountsOfThePortal()
    {
        // Arrange
        await RunBackupAsync();
        await AuthenticateAsMemberAsync(EmployeeType.DocSpaceAdmin);

        // Act
        var counts = await _backupApi.GetBackupsCountsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        counts.Response.Free.Should().Be(1);
        counts.Response.Paid.Should().Be(0);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetBackupsCounts_NonAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange: the owner reads the counters, so the endpoint works on this portal
        var ownerCounts = await _backupApi.GetBackupsCountsAsync(cancellationToken: TestContext.Current.CancellationToken);
        ownerCounts.Response.Should().NotBeNull();

        await AuthenticateAsMemberAsync(employeeType);

        // Act & Assert
        await ExpectForbiddenAsync(() => _backupApi.GetBackupsCountsAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
