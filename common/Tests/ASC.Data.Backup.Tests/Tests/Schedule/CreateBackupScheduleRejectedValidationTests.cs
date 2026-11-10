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
/// POST /api/2.0/backup/createbackupschedule with a body the server refuses with a 4xx. Each case first
/// stores a baseline schedule, then sends the invalid one and proves the baseline survived, so a
/// rejection that still overwrote the schedule is reported as that and not as a status. Values the
/// framework itself refuses never reach the controller; they are sent as verbatim JSON where the generated
/// DTO cannot carry them (a number above the Int32 range, a string where a number is expected, an object
/// where an array is).
/// </summary>
/// <remarks>
/// BUG 84511: a <c>folderId</c> that is not an integer made the server look for a third-party provider and
/// answer a misleading 404 ("Selector '' not found") instead of 400.
/// </remarks>
public class CreateBackupScheduleRejectedValidationTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private static CreateBackupScheduleRequestDto WithStored(int backupsStored)
    {
        var dto = Valid();
        dto.BackupsStored = backupsStored;

        return dto;
    }

    private static CreateBackupScheduleRequestDto Documents(string folderId)
    {
        var dto = Valid();
        dto.StorageType = BackupStorageType.Documents;
        dto.StorageParams = [new ItemKeyValuePairObjectObject("folderId", folderId)];

        return dto;
    }

    private static readonly Dictionary<string, Func<CreateBackupScheduleRejectedValidationTests, Task<int>>> _rejectedAs400 = new()
    {
        ["backupsStored 0"] = t => t.SendTypedAsync(WithStored(0)),
        ["backupsStored -1"] = t => t.SendTypedAsync(WithStored(-1)),
        ["backupsStored 31"] = t => t.SendTypedAsync(WithStored(31)),
        ["backupsStored 2147483647"] = t => t.SendTypedAsync(WithStored(int.MaxValue)),
        ["backupsStored above Int32 range"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":2147483648,"cronParams":{"period":0,"hour":3}}"""),
        ["fractional backupsStored"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":2.5,"cronParams":{"period":0,"hour":3}}"""),
        ["non-numeric backupsStored"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":"abc","cronParams":{"period":0,"hour":3}}"""),
        ["a non-numeric storageType"] = t => t.SendRawAsync("""{"storageType":"abc","backupsStored":5,"cronParams":{"period":0,"hour":3}}"""),
        ["a non-numeric period"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":"abc","hour":3}}"""),
        ["a non-numeric hour"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":0,"hour":"x"}}"""),
        ["a fractional hour"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":0,"hour":3.5}}"""),
        ["a non-numeric day"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":2,"hour":3,"day":"x"}}"""),
        ["storageParams as an object instead of an array"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":0,"hour":3},"storageParams":{"folderId":"1"}}"""),
        ["storageParams as a string"] = t => t.SendRawAsync("""{"storageType":4,"backupsStored":5,"cronParams":{"period":0,"hour":3},"storageParams":"x"}""")
    };

    public static TheoryData<string> RejectedAs400Cases => [.. _rejectedAs400.Keys];

    [Theory]
    [MemberData(nameof(RejectedAs400Cases))]
    public async Task CreateBackupSchedule_InvalidValue_ReturnsBadRequestAndKeepsStoredSchedule(string name)
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => _rejectedAs400[name](this));

        // Assert
        status.Should().Be(400);
    }

    [Fact]
    public async Task CreateBackupSchedule_FolderThatDoesNotExist_ReturnsNotFoundAndKeepsStoredSchedule()
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => SendTypedAsync(Documents("99999999")));

        // Assert
        status.Should().Be(404);
    }

    /// <summary>
    /// BUG 84511: a non-numeric folderId of a Documents schedule used to be looked up as a third-party folder
    /// and answered with a misleading 404 ("Selector '' not found"). It is now refused with 400, as
    /// startbackup already did.
    /// </summary>
    [Fact]
    [Trait("Bug", "84511")]
    public async Task CreateBackupSchedule_NonNumericFolderId_ReturnsBadRequestAndKeepsStoredSchedule()
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => SendTypedAsync(Documents("invalid_value")));

        // Assert
        status.Should().Be(400);
    }
}
