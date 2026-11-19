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

namespace ASC.Files.Tests.Tests._06_Operations.CheckDestFolder;

/// <summary>
/// <c>GET /api/2.0/files/fileops/checkdestfolder</c> with a listed file id that names no file. For a form-filling
/// room the check reads every listed file to see whether it is a PDF form, and an unknown id used to end as 500 on
/// the missing file. It is now not accepted, as an unknown id is left out of the answer for any other destination.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckDestFolderUnknownFileTests(
    AspireAppFixture fixture)
    : CheckDestFolderTestBase(fixture)
{
    private const int UnknownFileId = int.MaxValue;

    [Fact]
    public async Task CheckDestFolder_UnknownFileToFillingFormsRoom_ReturnsNoneAllowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest CheckDestFolder FillingFormsRoom Unknown");

        // Act
        var result = await CheckDestFolder(fileIds: [UnknownFileId], destFolderId: room.Id);

        // Assert
        result.Result.Should().Be(CheckDestFolderResult.NoneAllowed);
        result.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckDestFolder_FormAndUnknownFileToFillingFormsRoom_ReturnsPartAllowedWithTheForm()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        const string formTitle = "Autotest CheckDestFolder Form Beside Unknown.pdf";
        var formFileId = await UploadOoFormAsync(myDocsFolderId, formTitle);
        var room = await CreateFillingFormsRoom("Autotest CheckDestFolder FillingFormsRoom Form And Unknown");

        // Act
        var result = await CheckDestFolder(fileIds: [formFileId, UnknownFileId], destFolderId: room.Id);

        // Assert
        result.Result.Should().Be(CheckDestFolderResult.PartAllowed);
        result.Files.Should().ContainSingle().Which.Title.Should().Be(formTitle);
    }

    /// <summary>
    /// The same unknown id with a destination that takes every file: the shape the form-filling room now follows.
    /// </summary>
    [Fact]
    public async Task CheckDestFolder_UnknownFileToCustomRoom_ReturnsNoneAllowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CheckDestFolder CustomRoom Unknown");

        // Act
        var result = await CheckDestFolder(fileIds: [UnknownFileId], destFolderId: room.Id);

        // Assert
        result.Result.Should().Be(CheckDestFolderResult.NoneAllowed);
        result.Files.Should().BeEmpty();
    }
}
