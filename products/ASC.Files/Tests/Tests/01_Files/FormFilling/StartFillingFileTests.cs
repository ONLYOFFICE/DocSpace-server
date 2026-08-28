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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// <c>PUT /files/file/:fileId/startfilling</c>. <c>FileStorageService.StartFillingAsync</c> only
/// touches the form-filling properties when the file is a PDF sitting directly in a
/// <see cref="RoomType.FillingFormsRoom"/> - for every other file it is a no-op that just returns
/// the file, which is why a plain docx or a PDF outside such a room also answers 200.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class StartFillingFileTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    [Fact]
    public async Task StartFillingFile_FormInFillingFormsRoom_ReturnsForm()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest StartFilling Room");
        var form = await CreateFormInRoom(room.Id);

        await StartFormFilling(form.Id);

        // Act
        var result = (await _filesApi.StartFillingFileAsync(form.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(form.Id);
    }

    [Fact]
    public async Task StartFillingFile_NonExistentFileId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartFillingFileAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task StartFillingFile_RegularDocxFile_ReturnsFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Not A Form.docx", Owner);

        // Act
        var result = (await _filesApi.StartFillingFileAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task StartFillingFile_FormOutsideFillingFormsRoom_ReturnsForm()
    {
        // Arrange - the form lives in My Documents, not a FillingFormsRoom, and manageformfilling was
        // never called on it.
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var form = await CreateFile("Autotest Standalone Form.pdf", myDocsId);

        // Act
        var result = (await _filesApi.StartFillingFileAsync(form.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(form.Id);
    }
}
