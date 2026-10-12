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
/// <c>GET /api/2.0/files/fileops/move</c> (<c>checkMoveOrCopyBatchItems</c>) — functional coverage of
/// destination room types and batch shapes. The endpoint never performs the move/copy itself; it only
/// reports which of the requested items would conflict with something already at <c>destFolderId</c>,
/// so a "no conflict" case is asserted by an empty response.
/// </summary>
[Trait("Category", "Operations")]
public class CheckMoveOrCopyDestinationTests(
    AspireAppFixture fixture)
    : CheckMoveCopyTestBase(fixture)
{
    public static TheoryData<RoomType, bool> DestinationRoomTypes => new()
    {
        { RoomType.CustomRoom, true },
        { RoomType.EditingRoom, false },
        { RoomType.FillingFormsRoom, false },
        { RoomType.PublicRoom, false },
        { RoomType.VirtualDataRoom, false },
    };

    [Theory]
    [MemberData(nameof(DestinationRoomTypes))]
    public async Task CheckMove_ToRoomType_ReturnsEmptyResponse(RoomType roomType, bool isFolder)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var destFolder = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest CheckMove Dest {roomType}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        var checkParams = new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip
        };

        if (isFolder)
        {
            var folder = await CreateFolderInMy($"Autotest CheckMove Source Folder {roomType}", Owner);
            checkParams.FolderIds = [new(folder.Id)];
            checkParams.FileIds = [];
        }
        else
        {
            var file = await CreateFileInMy($"Autotest CheckMove To {roomType}.docx", Owner);
            checkParams.FileIds = [new(file.Id)];
            checkParams.FolderIds = [];
        }

        // Act
        var response = (await CheckMoveOrCopyBatch(
            checkParams, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_MultipleFiles_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file1 = await CreateFileInMy("Autotest CheckMove Multi1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest CheckMove Multi2.docx", Owner);

        var destFolder = await CreateCustomRoom("Autotest CheckMove Multi Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file1.Id), new(file2.Id)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckCopy_DeleteAfterFalse_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckCopy File.docx", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckCopy Dest Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [],
            DeleteAfter = false
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_FilesAndFoldersTogether_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Mixed File.docx", Owner);
        var folder = await CreateFolderInMy("Autotest CheckMove Mixed Folder", Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Mixed Dest Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(file.Id)],
            FolderIds = [new(folder.Id)]
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_ContentTrue_FolderContentsOnly_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolderInMy("Autotest CheckMove Content True Folder", Owner);
        await CreateFile("Autotest CheckMove Content True File.docx", folder.Id);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Content True Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FolderIds = [new(folder.Id)],
            FileIds = [],
            Content = true
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckMove_EmptyRequest_NoIds_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove Empty Dest Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }
}
