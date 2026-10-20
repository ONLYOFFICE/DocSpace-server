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

/// <summary>Open-bug scenarios of <c>PUT /api/2.0/files/fileops/move</c>.</summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class MoveBatchBugTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// An unknown folder id in <c>folderIds</c> used to hit an unhandled NullReferenceException in the
    /// general permission check of a move/copy, and the endpoint answered 500. That check now refuses an
    /// unknown first folder with 404, as it already did for an unknown first file. The control call with
    /// an unknown fileId shows the destination itself is accepted.
    /// </summary>
    [Fact]
    [Trait("Bug", "84436")]
    public async Task MoveBatch_NonExistentFolderId_Returns404AndMovesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dest = await CreateFolder("Autotest MoveBatch GhostFolder Dest", myDocsFolderId);

        var control = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                BuildRequest(dest.Id, fileIds: [999999999]),
                TestContext.Current.CancellationToken));
        control.ErrorCode.Should().BeLessThan(500, "an unknown fileId is a client error");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(
                BuildRequest(dest.Id, folderIds: [999999999]),
                TestContext.Current.CancellationToken));

        // Assert
        var destAfter = await GetContent(dest.Id);
        (destAfter.Files ?? []).Should().BeEmpty();
        (destAfter.Folders ?? []).Should().BeEmpty();

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// Moving a file onto a same-named file with <c>Duplicate</c> must place it beside the existing
    /// one under a numeric-suffixed name, as copy does. The bug leaves two entries with one title.
    /// </summary>
    [Fact]
    [Trait("Bug", "84437")]
    public async Task MoveFile_ConflictDuplicate_GivesMovedFileNewName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        const string title = "same.docx";

        var srcFolder = await CreateFolder("Autotest MoveBatch NameClash Src", myDocsFolderId);
        var destFolder = await CreateFolder("Autotest MoveBatch NameClash Dest", myDocsFolderId);
        var copyDestFolder = await CreateFolder("Autotest MoveBatch NameClash Copy", myDocsFolderId);
        var srcFile = await CreateFile(title, srcFolder.Id);
        await CreateFile(title, destFolder.Id);
        await CreateFile(title, copyDestFolder.Id);

        // Control: copy with Duplicate renames the incoming file.
        await _filesOperationsApi.CopyBatchItemsAsync(
            BuildRequest(copyDestFolder.Id, fileIds: [srcFile.Id], conflictResolveType: FileConflictResolveType.Duplicate),
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var copied = (await GetContent(copyDestFolder.Id)).Files ?? [];
        copied.Should().HaveCount(2);
        copied.Select(f => f.Title).Distinct().Should().HaveCount(2, "copy honours Duplicate");

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(
            BuildRequest(destFolder.Id, fileIds: [srcFile.Id], conflictResolveType: FileConflictResolveType.Duplicate),
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert - the move itself worked, only the naming is in question
        var destFiles = (await GetContent(destFolder.Id)).Files ?? [];
        var srcFiles = (await GetContent(srcFolder.Id)).Files ?? [];
        destFiles.Should().HaveCount(2);
        srcFiles.Should().BeEmpty();

        destFiles.Select(f => f.Title).Distinct().Should().HaveCount(2, "titles in the destination after the move");
    }

    private static BatchRequestDto BuildRequest(int destFolderId, int[]? fileIds = null, int[]? folderIds = null, FileConflictResolveType conflictResolveType = FileConflictResolveType.Skip)
    {
        return new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(destFolderId),
            ConflictResolveType = conflictResolveType,
            FileIds = (fileIds ?? []).Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList(),
            FolderIds = (folderIds ?? []).Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList(),
            DeleteAfter = false
        };
    }

    private async Task<FolderContentDto> GetContent(int folderId)
    {
        return (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
