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
/// GET /api/2.0/backup/getbackupschedule: the answer without a schedule, and the fields of a stored one.
/// </summary>
public class GetBackupScheduleValuesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    [Fact]
    public async Task GetBackupSchedule_NoSchedule_HasNoResponseMember()
    {
        // Act & Assert
        await ExpectNoScheduleAsync();
    }

    [Fact]
    public async Task GetBackupSchedule_StoredSchedule_FollowsTheContract()
    {
        // Arrange
        await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = 12,
                CronParams = new BackupCronRequest(BackupPeriod.EveryWeek, 14, 5)
            },
            TestContext.Current.CancellationToken);

        // Act
        var answer = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        answer.StatusCode.Should().Be(200);
        answer.Count.Should().Be(1);

        var schedule = answer.Response;
        schedule.StorageType.Should().Be(BackupStorageType.DataStore);
        schedule.BackupsStored!.Value.Should().BeInRange(1, 30);
        schedule.StorageParams.Should().NotBeNull();
        schedule.CronParams.Period.Should().NotBeNull();
        schedule.CronParams.Hour.Should().BeInRange(0, 23);
        schedule.CronParams.Day.Should().BeInRange(1, 7);
        schedule.Dump.Should().BeFalse();

        // Never run yet: the .NET default date, not a missing value.
        schedule.LastBackupTime.Should().BeBefore(new DateTime(2000, 1, 1));
    }
}
