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

namespace ASC.Files.Tests.Tests._06_Operations.CheckMoveCopy;

/// <summary>
/// <c>GET /api/2.0/files/fileops/move</c> (<c>checkMoveOrCopyBatchItems</c>) — conflict detection.
/// The endpoint reports back the items that already have a same-named entry at the destination, so
/// the UI can offer a conflict-resolution dialog before the actual move/copy runs.
/// </summary>
[Trait("Category", "Operations")]
public class CheckMoveOrCopyConflictTests(
    AspireAppFixture fixture)
    : CheckMoveCopyTestBase(fixture)
{
    [Fact]
    public async Task CheckMove_SameNamedFileInDestination_AppearsInResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string conflictTitle = "Autotest CheckMove ConflictFile.docx";
        var file = await CreateFileInMy(conflictTitle, Owner);

        var destFolder = await CreateCustomRoom("Autotest CheckMove Conflict Room");
        await CreateFile(conflictTitle, destFolder.Id);

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().ContainSingle().Which.Title.Should().Be(conflictTitle);
    }

    [Fact]
    public async Task CheckMove_NoSameNamedFileInDestination_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove NoConflict.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove NoConflict Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_OverwriteResolveType_ReturnsConflictItem()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string conflictTitle = "Autotest CheckMove Overwrite.docx";
        var file = await CreateFileInMy(conflictTitle, Owner);

        var destFolder = await CreateCustomRoom("Autotest CheckMove Overwrite Room");
        await CreateFile(conflictTitle, destFolder.Id);

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().ContainSingle().Which.Title.Should().Be(conflictTitle);
    }

    [Fact]
    public async Task CheckMove_DuplicateResolveType_NoConflict_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Duplicate.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Duplicate Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Duplicate,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_SourceEqualsDestination_ReturnsConflictItem()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileTitle = "Autotest CheckMove SameFolder.docx";
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile(fileTitle, myDocsFolderId);

        // Act
        // Moving a file into the folder it already lives in: the file itself is the same-named
        // entry at the destination, so the endpoint reports it as a conflict.
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(myDocsFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().ContainSingle().Which.Title.Should().Be(fileTitle);
    }
}
