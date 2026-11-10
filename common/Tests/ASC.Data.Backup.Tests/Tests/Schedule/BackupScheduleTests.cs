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
/// The backup schedule endpoints with valid input: createbackupschedule, getbackupschedule and
/// deletebackupschedule, as the owner and as a DocSpace admin. The test stack runs as a standalone
/// installation, so scheduled backups need no paid plan.
/// </summary>
public class BackupScheduleTests(
    AspireAppFixture fixture)
    : BackupFunctionalTestBase(fixture)
{
    [Theory]
    [InlineData(BackupCaller.Owner, BackupPeriod.EveryDay, 3, null, 5)]
    [InlineData(BackupCaller.DocSpaceAdmin, BackupPeriod.EveryMonth, 0, 1, 10)]
    public async Task CreateBackupSchedule_TemporaryStorage_StoresIt(BackupCaller caller, BackupPeriod period, int hour, int? day, int backupsStored)
    {
        // Arrange
        await ActAsAsync(caller);

        // Act
        var created = await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = backupsStored,
                CronParams = new BackupCronRequest(period, hour, day)
            },
            TestContext.Current.CancellationToken);

        // Assert
        created.StatusCode.Should().Be(200);
        created.Response.Should().BeTrue();

        var schedule = (await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        schedule.StorageType.Should().Be(BackupStorageType.DataStore);
        schedule.BackupsStored.Should().Be(backupsStored);
        schedule.CronParams.Period.Should().Be(period);
        schedule.CronParams.Hour.Should().Be(hour);
        schedule.CronParams.Day.Should().Be(day ?? 0);
    }

    [Fact]
    public async Task CreateBackupSchedule_BackupRoom_StoresIt()
    {
        // Arrange
        await ActAsAsync(BackupCaller.Owner);
        var roomId = await CreateBackupRoomAsync("Autotest Backup Schedule Room");

        // Act
        var created = await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.Documents,
                StorageParams = [new ItemKeyValuePairObjectObject("folderId", roomId.ToString())],
                BackupsStored = 3,
                CronParams = new BackupCronRequest(BackupPeriod.EveryWeek, 2, 1)
            },
            TestContext.Current.CancellationToken);

        // Assert
        created.StatusCode.Should().Be(200);
        created.Response.Should().BeTrue();

        var schedule = (await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        schedule.StorageType.Should().Be(BackupStorageType.Documents);
        schedule.BackupsStored.Should().Be(3);
        schedule.CronParams.Period.Should().Be(BackupPeriod.EveryWeek);
        schedule.CronParams.Hour.Should().Be(2);
        schedule.CronParams.Day.Should().Be(1);
    }

    [Theory]
    [InlineData(BackupCaller.Owner, BackupPeriod.EveryWeek, 5, 2, 7)]
    [InlineData(BackupCaller.DocSpaceAdmin, BackupPeriod.EveryMonth, 1, 15, 3)]
    public async Task GetBackupSchedule_TemporaryStorage_ReturnsAllFields(BackupCaller caller, BackupPeriod period, int hour, int day, int backupsStored)
    {
        // Arrange
        await ActAsAsync(caller);
        await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = backupsStored,
                CronParams = new BackupCronRequest(period, hour, day)
            },
            TestContext.Current.CancellationToken);

        // Act
        var result = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.StorageType.Should().Be(BackupStorageType.DataStore);
        result.Response.BackupsStored.Should().Be(backupsStored);
        result.Response.CronParams.Period.Should().Be(period);
        result.Response.CronParams.Hour.Should().Be(hour);
        result.Response.CronParams.Day.Should().Be(day);
    }

    [Fact]
    public async Task GetBackupSchedule_BackupRoom_ReturnsStorageTypeAndFolder()
    {
        // Arrange
        await ActAsAsync(BackupCaller.Owner);
        var roomId = await CreateBackupRoomAsync("Autotest Get Schedule Room");
        await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.Documents,
                StorageParams = [new ItemKeyValuePairObjectObject("folderId", roomId.ToString())],
                BackupsStored = 4,
                CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 0)
            },
            TestContext.Current.CancellationToken);

        // Act
        var result = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(200);
        result.Response.StorageType.Should().Be(BackupStorageType.Documents);
        result.Response.BackupsStored.Should().Be(4);
        result.Response.CronParams.Period.Should().Be(BackupPeriod.EveryDay);
        result.Response.CronParams.Hour.Should().Be(0);
        result.Response.StorageParams.Should().ContainKey("folderId").WhoseValue.Should().Be(roomId.ToString());
    }

    [Theory]
    [MemberData(nameof(BackupCallerData.All), MemberType = typeof(BackupCallerData))]
    public async Task DeleteBackupSchedule_ExistingSchedule_ReturnsTrue(BackupCaller caller)
    {
        // Arrange
        await ActAsAsync(caller);
        await _backupApi.CreateBackupScheduleAsync(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = 5,
                CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 3)
            },
            TestContext.Current.CancellationToken);

        // Act
        var deleted = await _backupApi.DeleteBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        deleted.StatusCode.Should().Be(200);
        deleted.Response.Should().BeTrue();
    }
}
