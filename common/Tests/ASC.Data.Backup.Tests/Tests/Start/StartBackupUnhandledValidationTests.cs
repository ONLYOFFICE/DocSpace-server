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


namespace ASC.Data.Backup.Tests.Tests.Start;

/// <summary>
/// POST /api/2.0/backup/startbackup with a storageParams item without a key or a value, or a third-party
/// request without parameters. Each case proves the request started nothing.
/// </summary>
/// <remarks>
/// BUG 84512: these end in an unhandled exception (500) instead of the 400 that the same controller returns
/// for a <c>folderId</c> of the wrong form ("wrong folder type"). The generated DTO always writes the
/// <c>key</c> and <c>value</c> of an item, so "without a value" and "a null folderId" are the same request.
/// </remarks>
public class StartBackupUnhandledValidationTests(
    AspireAppFixture fixture)
    : StartBackupValidationTestBase(fixture)
{
    private static readonly Dictionary<string, Func<StartBackupRequestDto>> _unhandled = new()
    {
        ["a storageParams item without a value"] = () => Documents(Item("folderId", null)),
        ["a storageParams item without a key"] = () => Documents(Item(null, "1")),
        ["a null storageParams item"] = () => new StartBackupRequestDto { StorageType = BackupStorageType.Documents, StorageParams = [null!] },
        ["a ThridpartyDocuments request without storageParams"] = () => new StartBackupRequestDto { StorageType = BackupStorageType.ThridpartyDocuments }
    };

    public static TheoryData<string> UnhandledCases => [.. _unhandled.Keys];

    [Theory]
    [Trait("Bug", "84512")]
    [MemberData(nameof(UnhandledCases))]
    public async Task StartBackup_ItemWithoutKeyOrValue_ReturnsBadRequestAndStartsNothing(string name)
    {
        // Act
        var rejection = await SendRejectedAsync(_unhandled[name]());

        // Assert
        rejection.Status.Should().Be(400);
    }
}
