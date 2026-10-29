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


namespace ASC.Data.Backup.Tests.Tests.Schedule;

/// <summary>
/// POST /api/2.0/backup/createbackupschedule: only the portal owner and the portal administrators may
/// schedule backups. The test stack runs as a standalone installation, which needs no paid plan.
/// </summary>
public class CreateBackupSchedulePermissionsTests(
    AspireAppFixture fixture)
    : BackupPermissionsTests(fixture)
{
    private static CreateBackupScheduleRequestDto ValidSchedule()
    {
        return new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = 5,
            CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 3)
        };
    }

    [Fact]
    public async Task CreateBackupSchedule_Anonymous_ReturnsUnauthorized()
    {
        // Act & Assert
        await ExpectUnauthorizedAsync(() => _backupApi.CreateBackupScheduleAsync(ValidSchedule(), TestContext.Current.CancellationToken));
    }

    /// <remarks>
    /// The TypeScript suite expects 402 here and marks it as failing on a local instance. A standalone
    /// installation does not require a paid plan, so the owner's schedule is stored.
    /// </remarks>
    [Fact]
    public async Task CreateBackupSchedule_OwnerOnStandalone_DoesNotRequirePaidPlan()
    {
        // Act
        var created = await _backupApi.CreateBackupScheduleAsync(ValidSchedule(), TestContext.Current.CancellationToken);

        // Assert
        created.Response.Should().BeTrue();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateBackupSchedule_NonAdmin_ReturnsForbidden(EmployeeType employeeType)
    {
        // Arrange
        await AuthenticateAsMemberAsync(employeeType);

        // Act & Assert
        await ExpectForbiddenAsync(() => _backupApi.CreateBackupScheduleAsync(ValidSchedule(), TestContext.Current.CancellationToken));
    }
}
