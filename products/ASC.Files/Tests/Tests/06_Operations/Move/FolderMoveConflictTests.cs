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
namespace ASC.Files.Tests.Tests._06_Operations.Move;

/// <summary>
/// Conflict resolution of PUT /api/2.0/files/fileops/move for a folder whose title the destination
/// already has.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FolderMoveConflictTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// With <c>Duplicate</c> both folders are kept, so the moved one takes the first free title of the
    /// destination, as a copied folder does, and keeps its contents.
    /// </summary>
    [Fact]
    public async Task MoveFolder_ConflictDuplicate_GivesMovedFolderNewName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        const string title = "Autotest Same Folder";

        var srcParent = await CreateFolder("Autotest MoveFolder Duplicate Src", myDocsFolderId);
        var destParent = await CreateFolder("Autotest MoveFolder Duplicate Dest", myDocsFolderId);
        var moved = await CreateFolder(title, srcParent.Id);
        await CreateFile("inside.docx", moved.Id);
        var existing = await CreateFolder(title, destParent.Id);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            ReturnSingleOperation = true,
            DestFolderId = new(destParent.Id),
            ConflictResolveType = FileConflictResolveType.Duplicate,
            FileIds = [],
            FolderIds = [new(moved.Id)]
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var srcFolders = (await GetContent(srcParent.Id)).Folders ?? [];
        srcFolders.Should().BeEmpty();

        var destFolders = (await GetContent(destParent.Id)).Folders ?? [];
        destFolders.Should().HaveCount(2);
        destFolders.Select(f => f.Title).Distinct().Should().HaveCount(2, "titles in the destination after the move");

        var existingContent = await GetContent(existing.Id);
        (existingContent.Files ?? []).Should().BeEmpty("the existing folder is left alone");

        var movedContent = await GetContent(moved.Id);
        (movedContent.Files ?? []).Select(f => f.Title).Should().Equal("inside.docx");
    }

    private async Task<FolderContentDto> GetContent(int folderId)
    {
        return (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
