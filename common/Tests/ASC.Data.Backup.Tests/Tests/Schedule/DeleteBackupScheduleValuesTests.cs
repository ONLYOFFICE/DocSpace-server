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
/// DELETE /api/2.0/backup/deletebackupschedule: deleting a schedule that is not there, and deleting one twice.
/// </summary>
public class DeleteBackupScheduleValuesTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    [Fact]
    public async Task DeleteBackupSchedule_NoSchedule_AnswersTrue()
    {
        // Arrange
        await ExpectNoScheduleAsync();

        // Act
        var deleted = await _backupApi.DeleteBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        deleted.StatusCode.Should().Be(200);
        deleted.Response.Should().BeTrue();
        await ExpectNoScheduleAsync();
    }

    [Fact]
    public async Task DeleteBackupSchedule_Twice_AnswersTrueBothTimesAndANewScheduleCanBeCreated()
    {
        // Arrange
        await SetScheduleAsync(EveryDay(3));

        // Act & Assert
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var deleted = await _backupApi.DeleteBackupScheduleAsync(cancellationToken: TestContext.Current.CancellationToken);

            deleted.StatusCode.Should().Be(200, $"deletion {attempt}");
            deleted.Response.Should().BeTrue($"deletion {attempt}");
            await ExpectNoScheduleAsync();
        }

        await SetScheduleAsync(new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = 5,
            CronParams = new BackupCronRequest(BackupPeriod.EveryWeek, 8, 2)
        });
    }
}
