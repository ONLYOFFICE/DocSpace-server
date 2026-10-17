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


namespace ASC.Data.Backup.Tests.Tests.Restore;

/// <summary>
/// The restore endpoint with a body that names no usable source: a <c>backupId</c> that is not a GUID and
/// no <c>filePath</c>. Every request goes through <see cref="BackupTestBase"/>, whose guard refuses
/// anything that could reach the restore queue.
/// </summary>
/// <remarks>
/// BUG 84513: no usable source reaches the controller, which then looks up a <c>filePath</c> storage
/// parameter that is not there and ends in an unhandled exception (500). The same endpoint answers 400
/// when <c>backupId</c> is missing altogether, and the other actions of this controller return 400 for bad
/// input. Not run, because no request of these kinds can be proven unable to start a restore: any
/// <c>backupId</c> that is a GUID (so any test of a backup that does not exist), any <c>storageParams</c>
/// with a <c>filePath</c> (so a path that does not exist and a storage type that does not match it), a
/// <c>storageType</c> outside the enum (the missing-source error is raised first and hides it) and
/// <c>dump: true</c>.
/// </remarks>
public class StartRestoreNoSourceValidationTests(
    AspireAppFixture fixture)
    : BackupTestBase(fixture)
{
    private const string NotAGuid = "not-a-guid";

    private static readonly Dictionary<string, Func<StartRestoreNoSourceValidationTests, Task<int>>> _noSource = new()
    {
        // The generated DTO refuses a null backupId before anything is sent, so this one is verbatim JSON.
        ["a null backupId"] = t => t.SendRawAsync("""{"backupId":null}"""),
        ["an empty backupId"] = t => t.SendTypedAsync(new StartBackupRestoreRequestDto(backupId: "")),
        ["a backupId that is not a GUID"] = t => t.SendTypedAsync(new StartBackupRestoreRequestDto(backupId: NotAGuid)),
        ["a backupId that is not a GUID with DataStore storage"] = t => t.SendTypedAsync(
            new StartBackupRestoreRequestDto(backupId: NotAGuid, storageType: BackupStorageType.DataStore)),
        ["a backupId that is not a GUID with empty storageParams"] = t => t.SendTypedAsync(
            new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [])),
        ["a backupId that is not a GUID with null storageParams"] = t => t.SendTypedAsync(
            new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: null!)),
        ["a backupId that is not a GUID with unrelated storageParams"] = t => t.SendTypedAsync(
            new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [new ItemKeyValuePairObjectObject("folderId", "1")])),
        ["a backupId that is not a GUID with a null storageParams item"] = t => t.SendTypedAsync(
            new StartBackupRestoreRequestDto(backupId: NotAGuid, storageParams: [null!])),
        // The generated item always writes its key, and the guard refuses a null key, so this one is verbatim JSON.
        ["a backupId that is not a GUID with a storageParams item without a key"] = t => t.SendRawAsync(
            """{"backupId":"not-a-guid","storageParams":[{"value":"x"}]}""")
    };

    public static TheoryData<string> NoSourceCases => [.. _noSource.Keys];

    [Theory]
    [Trait("Bug", "84513")]
    [MemberData(nameof(NoSourceCases))]
    public async Task StartRestore_NoUsableSource_ReturnsBadRequestAndStartsNoRestore(string name)
    {
        // Act
        var status = await _noSource[name](this);

        // Assert
        await ExpectRestoreNotStartedAsync();
        status.Should().Be(400);
    }

    private async Task<int> SendTypedAsync(StartBackupRestoreRequestDto dto)
    {
        try
        {
            await StartRestoreThatCannotQueueAsync(dto);

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    private async Task<int> SendRawAsync(string rawJson)
    {
        using var response = await StartRawRestoreThatCannotQueueAsync(rawJson);

        return (int)response.StatusCode;
    }
}
