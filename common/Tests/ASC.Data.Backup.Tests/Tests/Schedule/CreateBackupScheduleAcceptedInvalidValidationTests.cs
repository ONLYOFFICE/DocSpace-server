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
/// POST /api/2.0/backup/createbackupschedule with a storage the server cannot ever run a backup into. Each
/// case first stores a baseline schedule, then sends the invalid one and proves the baseline survived.
/// </summary>
/// <remarks>
/// BUG 84510: the server accepts these with 200 and stores them, replacing the working schedule with one
/// that can never run. They have to be rejected with 400.
/// </remarks>
public class CreateBackupScheduleAcceptedInvalidValidationTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private static CreateBackupScheduleRequestDto WithStorage(BackupStorageType storageType, params ItemKeyValuePairObjectObject[] storageParams)
    {
        var dto = Valid();
        dto.StorageType = storageType;
        dto.StorageParams = storageParams.Length == 0 ? null! : storageParams.ToList();

        return dto;
    }

    private static readonly Dictionary<string, Func<CreateBackupScheduleRequestDto>> _acceptedButInvalid = new()
    {
        ["storageType 99"] = () => WithStorage((BackupStorageType)99),
        ["a negative storageType"] = () => WithStorage((BackupStorageType)(-1)),
        ["a ThirdPartyConsumer schedule with an unknown module"] = () => WithStorage(
            BackupStorageType.ThirdPartyConsumer, new ItemKeyValuePairObjectObject("module", "no-such-module")),
        ["a ThirdPartyConsumer schedule without a module"] = () => WithStorage(BackupStorageType.ThirdPartyConsumer)
    };

    public static TheoryData<string> AcceptedButInvalidCases => [.. _acceptedButInvalid.Keys];

    [Theory]
    [Trait("Bug", "84510")]
    [MemberData(nameof(AcceptedButInvalidCases))]
    public async Task CreateBackupSchedule_StorageThatCannotRun_ReturnsBadRequestAndKeepsStoredSchedule(string name)
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => SendTypedAsync(_acceptedButInvalid[name]()));

        // Assert
        status.Should().Be(400);
    }
}
