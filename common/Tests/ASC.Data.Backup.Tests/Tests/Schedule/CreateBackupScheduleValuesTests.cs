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
/// POST /api/2.0/backup/createbackupschedule with values on the edge of what is valid, and with a schedule
/// that replaces an existing one. Each case stores the schedule, reads it back and checks the fields the
/// request set. The test stack runs as a standalone installation, so scheduled backups need no paid plan.
/// </summary>
public class CreateBackupScheduleValuesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    // What a stored schedule has to read back as: backupsStored defaults to 1, the period and hour are the
    // ones sent, and the day only matters when the request set one.
    private sealed record Expected(int BackupsStored, BackupPeriod Period, int Hour, int? Day = null);

    private sealed record Case(CreateBackupScheduleRequestDto Dto, Expected Expected);

    private static readonly Dictionary<string, Func<Case>> _cases = new()
    {
        ["hour 0 (lower bound)"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryDay, 0)),
            new Expected(5, BackupPeriod.EveryDay, 0)),
        ["hour 23 (upper bound)"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryDay, 23)),
            new Expected(5, BackupPeriod.EveryDay, 23)),
        ["weekly on day 1 (Sunday)"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3, 1)),
            new Expected(5, BackupPeriod.EveryWeek, 3, 1)),
        ["weekly on day 7 (Saturday)"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3, 7)),
            new Expected(5, BackupPeriod.EveryWeek, 3, 7)),
        ["monthly on day 1"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3, 1)),
            new Expected(5, BackupPeriod.EveryMonth, 3, 1)),
        ["monthly on day 29"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3, 29)),
            new Expected(5, BackupPeriod.EveryMonth, 3, 29)),
        ["monthly on day 31 (upper bound)"] = () => new Case(
            Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3, 31)),
            new Expected(5, BackupPeriod.EveryMonth, 3, 31)),
        ["backupsStored 1 (lower bound)"] = () => new Case(
            EveryDay(3, backupsStored: 1),
            new Expected(1, BackupPeriod.EveryDay, 3)),
        ["backupsStored 30 (upper bound)"] = () => new Case(
            EveryDay(3, backupsStored: 30),
            new Expected(30, BackupPeriod.EveryDay, 3)),
        // The generated DTO always writes `backupsStored`, so "omitted" and "null" are the same request.
        ["backupsStored null (defaults to 1)"] = () => new Case(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                BackupsStored = null,
                CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 3)
            },
            new Expected(1, BackupPeriod.EveryDay, 3)),
        ["storageParams null for DataStore"] = () => new Case(
            new CreateBackupScheduleRequestDto
            {
                StorageType = BackupStorageType.DataStore,
                StorageParams = null!,
                BackupsStored = 5,
                CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 3)
            },
            new Expected(5, BackupPeriod.EveryDay, 3))
    };

    public static TheoryData<string> ValidCases => [.. _cases.Keys];

    [Theory]
    [MemberData(nameof(ValidCases))]
    public async Task CreateBackupSchedule_ValidValues_StoresThem(string name)
    {
        // Arrange
        var (dto, expected) = _cases[name]();

        // Act
        var schedule = await SetScheduleAsync(dto);

        // Assert
        schedule.StorageType.Should().Be(BackupStorageType.DataStore);
        schedule.BackupsStored.Should().Be(expected.BackupsStored);
        schedule.CronParams.Period.Should().Be(expected.Period);
        schedule.CronParams.Hour.Should().Be(expected.Hour);

        if (expected.Day is { } day)
        {
            schedule.CronParams.Day.Should().Be(day);
        }
    }

    [Fact]
    public async Task CreateBackupSchedule_ExistingSchedule_IsReplacedByTheNewOne()
    {
        // Arrange
        await SetScheduleAsync(Valid());

        // Act
        var schedule = await SetScheduleAsync(new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = 3,
            CronParams = new BackupCronRequest(BackupPeriod.EveryMonth, 1, 15)
        });

        // Assert
        schedule.BackupsStored.Should().Be(3);
        schedule.CronParams.Period.Should().Be(BackupPeriod.EveryMonth);
        schedule.CronParams.Hour.Should().Be(1);
        schedule.CronParams.Day.Should().Be(15);
    }
}
