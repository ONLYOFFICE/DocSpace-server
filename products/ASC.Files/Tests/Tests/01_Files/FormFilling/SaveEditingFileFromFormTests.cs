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
/// <c>PUT /files/file/:fileId/saveediting</c> when posting the filled-in form binary back
/// (the TS suite calls this endpoint "saveediting/form", but it is the same
/// <c>SaveEditingFileFromForm</c> action - <c>EditorController.SaveEditingFileFromForm</c> - as the
/// regular save-editing call, just with a multipart file body). Saving replaces the file content and
/// bumps the version; it never re-classifies the file as a form, so <c>IsForm</c> stays whatever it
/// was when the file was created.
/// </summary>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class SaveEditingFileFromFormTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    private async Task<FileDtoInteger> SetupForm()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest SaveEditingFromForm Room " + Guid.NewGuid().ToString()[..8]);

        return await CreateFormInRoom(room.Id);
    }

    [Fact]
    public async Task SaveEditingFileFromForm_SubmittedFormBinary_SavesNewVersion()
    {
        // Arrange
        var form = await SetupForm();
        const int contentLength = 512;

        // Act
        var result = (await _filesApi.SaveEditingFileFromFormAsync(
            form.Id, file: BuildSubmittedFormFile(contentLength), forcesave: false,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(form.Id);
        result.FileExst.Should().Be(".pdf");
        result.IsForm.Should().BeTrue();
        result.Version.Should().Be(2);
        result.PureContentLength.Should().Be((long)contentLength);
        result.Comment.Should().Be("Edited");
    }

    /// <summary>
    /// BUG 81416: a non-existent file id currently answers with an error whose message leaks
    /// "Object reference" (a raw <see cref="NullReferenceException"/>) instead of a clean 404.
    /// </summary>
    [Trait("Bug", "81416")]
    [Fact]
    public async Task SaveEditingFileFromForm_NonExistentFileId_ReturnsNotFoundWithoutLeakingException()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveEditingFileFromFormAsync(
                999999999, file: BuildSubmittedFormFile(), cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().NotContain("Object reference");
    }

    [Fact]
    public async Task SaveEditingFileFromForm_Forcesave_SavesImmediately()
    {
        // Arrange
        var form = await SetupForm();
        const int contentLength = 512;

        // Act
        var result = (await _filesApi.SaveEditingFileFromFormAsync(
            form.Id, file: BuildSubmittedFormFile(contentLength), forcesave: true,
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Id.Should().Be(form.Id);
        result.IsForm.Should().BeTrue();
        result.Version.Should().Be(2);
        result.PureContentLength.Should().Be((long)contentLength);
        result.Comment.Should().Be("Edited");
    }
}
