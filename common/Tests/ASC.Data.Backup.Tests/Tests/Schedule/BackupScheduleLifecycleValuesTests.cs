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
/// POST /api/2.0/backup/createbackupschedule over the life of a schedule: changing it, changing one
/// setting of it, creating the same one again, and the manual backup operations that must leave it alone.
/// Every step stores the schedule and reads it back. The test stack runs as a standalone installation, so
/// scheduled backups need no paid plan.
/// </summary>
public class BackupScheduleLifecycleValuesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private static CreateBackupScheduleRequestDto Scheduled(BackupPeriod period, int hour, int? day, int backupsStored = 5)
    {
        return new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = backupsStored,
            CronParams = new BackupCronRequest(period, hour, day)
        };
    }

    [Fact]
    public async Task CreateBackupSchedule_ChangedFromDailyToWeeklyToMonthlyAndBack_StoresEachOne()
    {
        // Act & Assert
        var daily = await SetScheduleAsync(EveryDay(3));
        daily.CronParams.Day.Should().Be(0);

        await SetScheduleAsync(Scheduled(BackupPeriod.EveryWeek, 4, 3));
        await SetScheduleAsync(Scheduled(BackupPeriod.EveryMonth, 5, 20));

        var backToDaily = await SetScheduleAsync(EveryDay(6));
        backToDaily.CronParams.Day.Should().Be(0, "the day of the month is gone");
    }

    [Fact]
    public async Task CreateBackupSchedule_OneSettingChangedAtATime_KeepsTheOthers()
    {
        // Arrange
        await SetScheduleAsync(Scheduled(BackupPeriod.EveryWeek, 4, 3, backupsStored: 4));

        // Act & Assert - only the hour
        var hourChanged = await SetScheduleAsync(Scheduled(BackupPeriod.EveryWeek, 22, 3, backupsStored: 4));
        hourChanged.CronParams.Day.Should().Be(3);
        hourChanged.BackupsStored.Should().Be(4);

        // only the day of the week
        var dayChanged = await SetScheduleAsync(Scheduled(BackupPeriod.EveryWeek, 22, 6, backupsStored: 4));
        dayChanged.CronParams.Hour.Should().Be(22);
        dayChanged.BackupsStored.Should().Be(4);

        // only backupsStored
        var storedChanged = await SetScheduleAsync(Scheduled(BackupPeriod.EveryWeek, 22, 6, backupsStored: 9));
        storedChanged.CronParams.Hour.Should().Be(22);
        storedChanged.CronParams.Day.Should().Be(6);

        // only the day of the month, after the schedule became monthly
        await SetScheduleAsync(Scheduled(BackupPeriod.EveryMonth, 22, 10, backupsStored: 4));
        var monthDayChanged = await SetScheduleAsync(Scheduled(BackupPeriod.EveryMonth, 22, 25, backupsStored: 4));
        monthDayChanged.CronParams.Hour.Should().Be(22);
        monthDayChanged.BackupsStored.Should().Be(4);
    }

    [Fact]
    public async Task CreateBackupSchedule_IdenticalScheduleAgain_ChangesNothing()
    {
        // Act
        var first = await SetScheduleAsync(EveryDay(7));
        var second = await SetScheduleAsync(EveryDay(7));

        // Assert
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task DeleteBackupHistory_StoredSchedule_SurvivesIt()
    {
        // Arrange
        var schedule = await SetScheduleAsync(EveryDay(9));
        await RunBackupAsync();

        // Act
        var deleted = await _backupApi.DeleteBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        deleted.Response.Should().BeTrue();

        var history = await _backupApi.GetBackupHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);
        history.Response.Should().BeEmpty();

        var after = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);
        after.Response.Should().BeEquivalentTo(schedule);
    }

    [Fact]
    public async Task DeleteBackup_StoredSchedule_SurvivesIt()
    {
        // Arrange
        var schedule = await SetScheduleAsync(EveryDay(9));
        var taskId = await RunBackupAsync();

        // Act
        var deleted = await _backupApi.DeleteBackupAsync(Guid.Parse(taskId), TestContext.Current.CancellationToken);

        // Assert
        deleted.Response.Should().BeTrue();

        (await ListHistoryIdsAsync()).Should().NotContain(Canonical(taskId));

        var after = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);
        after.Response.Should().BeEquivalentTo(schedule);
    }
}
