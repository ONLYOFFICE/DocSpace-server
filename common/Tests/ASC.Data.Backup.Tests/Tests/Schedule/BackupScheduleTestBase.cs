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
/// What the schedule suites share: valid and baseline bodies, the two ways of sending a schedule, the
/// "stored schedule survived a rejected request" check and the read-back of a stored schedule. The test
/// stack runs as a standalone installation, so scheduled backups need no paid plan.
/// </summary>
public abstract class BackupScheduleTestBase(AspireAppFixture fixture) : BackupLifecycleTestBase(fixture)
{
    protected const string CreateSchedulePath = "api/2.0/backup/createbackupschedule";

    private static readonly CreateBackupScheduleRequestDto _baselineSchedule = new()
    {
        StorageType = BackupStorageType.DataStore,
        BackupsStored = 7,
        CronParams = new BackupCronRequest(BackupPeriod.EveryWeek, 5, 2)
    };

    /// <summary>A valid DataStore schedule that individual cases break one field of.</summary>
    protected static CreateBackupScheduleRequestDto Valid(BackupCronRequest? cronParams = null)
    {
        return new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = 5,
            CronParams = cronParams ?? new BackupCronRequest(BackupPeriod.EveryDay, 3)
        };
    }

    /// <summary>A valid DataStore schedule that runs every day at the given hour.</summary>
    protected static CreateBackupScheduleRequestDto EveryDay(int hour, int backupsStored = 5)
    {
        return new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = backupsStored,
            CronParams = new BackupCronRequest(BackupPeriod.EveryDay, hour)
        };
    }

    /// <summary>
    /// Stores a baseline schedule, sends the invalid one and proves the baseline survived. The baseline
    /// being stored proves the endpoint accepts a valid request at all. The side effect is checked before
    /// the status is returned on purpose: a rejected request that overwrote the schedule must be reported
    /// as that, not as a status.
    /// </summary>
    /// <param name="send">Sends the invalid request and returns its status.</param>
    protected async Task<int> SendInvalidScheduleAsync(Func<Task<int>> send)
    {
        // Arrange
        var created = await _backupApi.CreateBackupScheduleAsync(_baselineSchedule, TestContext.Current.CancellationToken);
        created.Response.Should().BeTrue();

        var before = (await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        before.BackupsStored.Should().Be(7);

        // Act
        var status = await send();

        // Assert
        var after = (await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Should().BeEquivalentTo(before);

        return status;
    }

    /// <summary>Sends a typed schedule and returns the status of the answer.</summary>
    protected async Task<int> SendTypedAsync(CreateBackupScheduleRequestDto? dto)
    {
        try
        {
            await _backupApi.CreateBackupScheduleAsync(dto, TestContext.Current.CancellationToken);

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    /// <summary>Sends a verbatim JSON body, with a JSON content type, and returns the status of the answer.</summary>
    protected async Task<int> SendRawAsync(string rawJson)
    {
        using var response = await _backup.PostRawAsync(CreateSchedulePath, rawJson, TestContext.Current.CancellationToken);

        return (int)response.StatusCode;
    }

    /// <summary>Creates the schedule, reads it back and checks the fields the request set.</summary>
    protected async Task<ScheduleDto> SetScheduleAsync(CreateBackupScheduleRequestDto dto)
    {
        var ct = TestContext.Current.CancellationToken;

        var created = await _backupApi.CreateBackupScheduleAsync(dto, ct);
        created.StatusCode.Should().Be(200);
        created.Response.Should().BeTrue();

        var schedule = (await _backupApi.GetBackupScheduleAsync(cancellationToken: ct)).Response;
        schedule.StorageType.Should().Be(dto.StorageType);
        // An omitted backupsStored is stored as the default of 1.
        schedule.BackupsStored.Should().Be(dto.BackupsStored ?? 1);
        schedule.CronParams.Period.Should().Be(dto.CronParams!.Period);
        schedule.CronParams.Hour.Should().Be(dto.CronParams.Hour);

        if (dto.CronParams.Day is { } day)
        {
            schedule.CronParams.Day.Should().Be(day);
        }

        return schedule;
    }

    /// <summary>Proves the portal has no schedule: the read answers 200 with no response member.</summary>
    protected async Task ExpectNoScheduleAsync()
    {
        var answer = await _backupApi.GetBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        answer.StatusCode.Should().Be(200);
        answer.Response.Should().BeNull();
        answer.Count.Should().Be(0);
    }

    /// <summary>A text value of the environment, empty when it is not set.</summary>
    protected static string EnvironmentValue(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? "";
    }
}
