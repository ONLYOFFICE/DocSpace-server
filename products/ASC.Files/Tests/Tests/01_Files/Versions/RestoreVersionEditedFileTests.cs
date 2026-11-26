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

namespace ASC.Files.Tests.Tests._01_Files.Versions;

/// <summary>
/// Restoring an earlier version of a file that is open for editing - through <c>restoreversion</c> or through
/// <c>lastVersion</c> of <c>PUT file/{fileId}</c> - is refused with 403, as saving an edited file is.
/// <c>EntryManager.UpdateToVersionFileAsync</c> raised a plain exception for it, and a 500 came back.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class RestoreVersionEditedFileTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    private const string EditedFileMessage = "Cannot update the file because it's opened for editing";

    [Fact]
    public async Task RestoreFileVersion_FileOpenForEditing_Returns403()
    {
        // Arrange
        var fileId = await EditedFileWithTwoVersions("Autotest Restore Edited.docx");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(fileId, version: 1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Be(EditedFileMessage);
    }

    [Fact]
    public async Task UpdateFile_LastVersionOfFileOpenForEditing_Returns403()
    {
        // Arrange
        var fileId = await EditedFileWithTwoVersions("Autotest Update Last Version Edited.docx");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(fileId, new UpdateFileRequest { LastVersion = 1 }, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        ErrorMessageOf(exception.ErrorContent!.ToString()).Should().Be(EditedFileMessage);
    }

    // A docx, because the editing flag is only reliable on it.
    private async Task<int> EditedFileWithTwoVersions(string title)
    {
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion(title);

        await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken);

        return file.Id;
    }
}
