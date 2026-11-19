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
/// POST /api/2.0/backup/createbackupschedule with a storageParams item without a key or a value, or a null item. Each
/// case proves the stored schedule survived.
/// </summary>
/// <remarks>
/// The controller read the key and value of every item without a check, so these ended in a NullReferenceException,
/// a 500. They are now refused with 400, as startbackup refuses them. The generated DTO always writes the
/// <c>key</c> and <c>value</c> of an item, so a missing value and a null value are the same request.
/// </remarks>
public class CreateBackupScheduleNullItemValidationTests(
    AspireAppFixture fixture)
    : BackupScheduleTestBase(fixture)
{
    private static readonly Dictionary<string, Func<CreateBackupScheduleRequestDto>> _nullItems = new()
    {
        ["a storageParams item without a value"] = () => WithItems(new ItemKeyValuePairObjectObject("module", null!)),
        ["a storageParams item without a key"] = () => WithItems(new ItemKeyValuePairObjectObject(null!, "1")),
        ["a null storageParams item"] = () => WithItems([null!])
    };

    public static TheoryData<string> NullItemCases => [.. _nullItems.Keys];

    [Theory]
    [MemberData(nameof(NullItemCases))]
    public async Task CreateBackupSchedule_ItemWithoutKeyOrValue_ReturnsBadRequestAndKeepsStoredSchedule(string name)
    {
        // Act
        var status = await SendInvalidScheduleAsync(() => SendTypedAsync(_nullItems[name]()));

        // Assert
        status.Should().Be(400);
    }

    private static CreateBackupScheduleRequestDto WithItems(params ItemKeyValuePairObjectObject[] storageParams)
    {
        var dto = Valid();
        dto.StorageParams = storageParams.ToList();

        return dto;
    }
}
