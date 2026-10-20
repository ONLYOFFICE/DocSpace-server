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
/// POST /api/2.0/backup/createbackupschedule with a body the server has to reject. Each case first
/// stores a baseline schedule, then sends the invalid one and proves the baseline survived, so a
/// rejection that still overwrote the schedule is reported as that and not as a status. The test
/// stack runs as a standalone installation, so scheduled backups need no paid plan.
/// </summary>
/// <remarks>
/// BUG 84509: these bodies were answered with an unhandled exception (500) - a null reference for a
/// missing body or <c>cronParams</c>, a cron format error for a period, hour or day out of range, and
/// a missing dictionary key for a Documents schedule without <c>folderId</c> - instead of 400. Fixed by
/// rejecting a missing <c>cronParams</c> and <c>folderId</c> in the controller and checking the period,
/// hour and day ranges before the cron string is built, all with 400.
/// </remarks>
public class CreateBackupScheduleValidationTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private static readonly Dictionary<string, Func<CreateBackupScheduleValidationTests, Task<int>>> _failsWith500 = new()
    {
        // Raw, with a JSON content type and nothing in it - what the TypeScript client sends. The
        // generated .NET client sends no content type at all for a null body, which the framework
        // answers with 415 before the action is reached.
        ["a request without a body"] = t => t.SendRawAsync(""),
        // Verbatim JSON: the generated DTO always writes `storageParams`, `backupsStored` and `dump`.
        ["an empty body"] = t => t.SendRawAsync("{}"),
        ["a body without cronParams"] = t => t.SendTypedAsync(new CreateBackupScheduleRequestDto
        {
            StorageType = BackupStorageType.DataStore,
            BackupsStored = 5
        }),
        // Verbatim JSON: the generated DTO omits a null `cronParams` instead of sending it.
        ["null cronParams"] = t => t.SendRawAsync("""{"storageType":"DataStore","backupsStored":5,"cronParams":null}"""),
        ["a period outside the enum (3)"] = t => t.SendTypedAsync(Valid(new BackupCronRequest((BackupPeriod)3, 3))),
        ["a negative period"] = t => t.SendTypedAsync(Valid(new BackupCronRequest((BackupPeriod)(-1), 3))),
        ["hour 24"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryDay, 24))),
        ["a negative hour"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryDay, -1))),
        ["a weekly schedule without a day"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3))),
        ["a weekly schedule on day 0"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3, 0))),
        ["a weekly schedule on day 8"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3, 8))),
        ["a weekly schedule on a negative day"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryWeek, 3, -1))),
        ["a monthly schedule without a day"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3))),
        ["a monthly schedule on day 0"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3, 0))),
        ["a monthly schedule on day 32"] = t => t.SendTypedAsync(Valid(new BackupCronRequest(BackupPeriod.EveryMonth, 3, 32))),
        ["a Documents schedule without storageParams"] = t =>
        {
            var dto = Valid();
            dto.StorageType = BackupStorageType.Documents;
            return t.SendTypedAsync(dto);
        },
        ["a Documents schedule whose storageParams lack folderId"] = t =>
        {
            var dto = Valid();
            dto.StorageType = BackupStorageType.Documents;
            dto.StorageParams = [new ItemKeyValuePairObjectObject("x", "1")];
            return t.SendTypedAsync(dto);
        },
        ["a ThridpartyDocuments schedule without storageParams"] = t =>
        {
            var dto = Valid();
            dto.StorageType = BackupStorageType.ThridpartyDocuments;
            return t.SendTypedAsync(dto);
        },
        ["a body without storageType and without folderId"] = t => t.SendTypedAsync(new CreateBackupScheduleRequestDto
        {
            BackupsStored = 5,
            CronParams = new BackupCronRequest(BackupPeriod.EveryDay, 3)
        })
    };

    public static TheoryData<string> FailsWith500Cases => [.. _failsWith500.Keys];

    [Theory]
    [Trait("Bug", "84509")]
    [MemberData(nameof(FailsWith500Cases))]
    public async Task CreateBackupSchedule_InvalidBody_ReturnsBadRequestAndKeepsStoredSchedule(string name)
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => _failsWith500[name](this));

        // Assert
        status.Should().Be(400);
    }
}
