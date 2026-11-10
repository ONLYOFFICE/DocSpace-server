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
/// POST /api/2.0/backup/startbackup with a storage the controller refuses. Without a usable
/// <c>storageType</c> the portal documents are the target, and they need an integer <c>folderId</c>; a
/// folder or a third-party connection that does not exist is a 404. Every case proves the request started
/// nothing.
/// </summary>
public class StartBackupStorageParamsValidationTests(
    AspireAppFixture fixture)
    : StartBackupValidationTestBase(fixture)
{
    private static readonly Dictionary<string, Func<StartBackupStorageParamsValidationTests, Task<BackupRejection>>> _noFolder = new()
    {
        // Raw, with a JSON content type and nothing in it: the generated client sends no content type at
        // all for a null body, which the framework answers with 415 before the action is reached.
        ["a request without a body"] = t => t.SendRejectedRawAsync(""),
        // Verbatim JSON: the generated DTO always writes `storageParams` and `dump`.
        ["an empty body"] = t => t.SendRejectedRawAsync("{}"),
        ["a null storageType"] = t => t.SendRejectedAsync(new StartBackupRequestDto { StorageType = null }),
        ["a Documents request without storageParams"] = t => t.SendRejectedAsync(new StartBackupRequestDto { StorageType = BackupStorageType.Documents }),
        ["a Documents request with empty storageParams"] = t => t.SendRejectedAsync(Documents()),
        ["a Documents request whose storageParams lack folderId"] = t => t.SendRejectedAsync(Documents(Item("x", "1"))),
        ["a Documents request with duplicate folderId keys"] = t => t.SendRejectedAsync(Documents(Item("folderId", "1"), Item("folderId", "2"))),
        ["an empty folderId"] = t => t.SendRejectedAsync(Documents(Item("folderId", ""))),
        ["a whitespace folderId"] = t => t.SendRejectedAsync(Documents(Item("folderId", " "))),
        ["a fractional folderId"] = t => t.SendRejectedAsync(Documents(Item("folderId", "1.5"))),
        ["a folderId above the Int32 range"] = t => t.SendRejectedAsync(Documents(Item("folderId", "9223372036854775807"))),
        ["a folderId above the Int64 range"] = t => t.SendRejectedAsync(Documents(Item("folderId", "99999999999999999999")))
    };

    public static TheoryData<string> NoFolderCases => [.. _noFolder.Keys];

    [Theory]
    [MemberData(nameof(NoFolderCases))]
    public async Task StartBackup_NoUsableFolder_ReturnsBadRequestAndStartsNothing(string name)
    {
        // Act
        var rejection = await _noFolder[name](this);

        // Assert
        rejection.Status.Should().Be(400);
        rejection.HasError().Should().BeTrue(rejection.Explain);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483647")]
    public async Task StartBackup_FolderThatDoesNotExist_ReturnsNotFoundAndStartsNothing(string folderId)
    {
        // Act
        var rejection = await SendRejectedAsync(Documents(Item("folderId", folderId)));

        // Assert
        rejection.Status.Should().Be(404);
        rejection.HasError().Should().BeTrue(rejection.Explain);
    }

    [Fact]
    public async Task StartBackup_ThirdPartyConnectionThatDoesNotExist_ReturnsNotFoundAndStartsNothing()
    {
        // Act
        var rejection = await SendRejectedAsync(new StartBackupRequestDto
        {
            StorageType = BackupStorageType.ThridpartyDocuments,
            StorageParams = [Item("folderId", "webdav-99999")]
        });

        // Assert
        rejection.Status.Should().Be(404);
        rejection.HasError().Should().BeTrue(rejection.Explain);
    }
}
