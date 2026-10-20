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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/copy</c> with a <c>folderIds</c> entry that does not exist.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CopyUnknownFolderBugTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    /// <summary>
    /// An unknown folder id in <c>folderIds</c> used to hit an unhandled NullReferenceException in the
    /// general permission check of a move/copy, and the endpoint answered 500. That check now refuses an
    /// unknown first folder with 404, as it already did for an unknown first file. The control call with
    /// an unknown fileId shows the destination itself is accepted.
    /// </summary>
    [Fact]
    [Trait("Bug", "84435")]
    public async Task CopyBatch_NonExistentFolderId_Returns404AndCopiesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dest = await CreateFolder("Autotest CopyBatch GhostFolder Dest", myDocsFolderId);

        var control = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(dest.Id, fileIds: [999999999]),
                TestContext.Current.CancellationToken));
        control.ErrorCode.Should().BeLessThan(500, "an unknown fileId is a client error");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(dest.Id, folderIds: [999999999]),
                TestContext.Current.CancellationToken));

        // Assert
        var destAfter = await GetFolderContent(dest.Id);
        FileTitles(destAfter).Should().BeEmpty();
        FolderTitles(destAfter).Should().BeEmpty();

        exception.ErrorCode.Should().Be(404);
    }
}
