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
/// What <c>restoreFileVersion</c> stores: the content of the chosen revision as a new version with the
/// "Restored from the revision of ..." comment. The <c>url</c> source is not covered: on the stand the Files
/// service gets no answer from the presigned address of its own portal. Bytes come from the upload-session helpers.
/// </summary>
[Trait("Category", "Files")]
[Trait("Feature", "Files")]
public class RestoreVersionContentTests(
    AspireAppFixture fixture)
    : _06_Operations.UploadSession.UploadSessionTestBase(fixture)
{
    private async Task<(int FileId, byte[] First, byte[] Second)> FileWithTwoContents(string title)
    {
        var folderId = await GetUserFolderIdAsync(Owner);
        var first = MakeBytes(200, 11);
        var second = MakeBytes(300, 12);
        var (fileId, _) = await PutFile(folderId, title, first);
        var (sameId, version) = await PutFile(folderId, title, second, createNewIfExist: false);

        sameId.Should().Be(fileId);
        version.Should().Be(2);

        return (fileId, first, second);
    }

    private async Task<FileDto> LatestRevision(int fileId)
    {
        var versions = (await _filesApi.GetFileVersionInfoAsync(fileId, TestContext.Current.CancellationToken)).Response;

        return versions.MaxBy(v => v.Version)!;
    }

    [Fact]
    public async Task RestoreVersion_StoresTheChosenRevisionAsANewVersionWithARestoreComment()
    {
        // Arrange
        var (fileId, first, _) = await FileWithTwoContents("Autotest Restore Content.bin");

        // Act
        await _filesApi.RestoreFileVersionAsync(fileId, version: 1, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        (await VersionsOf(fileId)).Should().Equal(1, 2, 3);
        await ExpectContent(fileId, first);
        (await LatestRevision(fileId)).Comment.Should().StartWith("Restored from the revision of");
    }
}
