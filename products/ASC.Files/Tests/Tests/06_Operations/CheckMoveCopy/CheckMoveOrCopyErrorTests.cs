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
/// <c>GET /api/2.0/files/fileops/move</c> (<c>checkMoveOrCopyBatchItems</c>) — error handling.
/// Contract: an id that resolves to nothing is 404, an existing entity the caller may not touch is
/// 403, and a client-side input error is 400. A non-existent *source* item, by contrast, is not an
/// error at all — it is simply not there to conflict, so it is silently skipped.
/// </summary>
[Trait("Category", "Operations")]
public class CheckMoveOrCopyErrorTests(
    AspireAppFixture fixture)
    : CheckMoveCopyTestBase(fixture)
{
    [Fact]
    public async Task CheckMove_DestinationIsArchivedRoom_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove Archived.docx", Owner);
        var room = await CreateCustomRoom("Autotest CheckMove Archived Room");

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                DestFolderId = new(room.Id),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CheckMove_NonExistentFileId_ReturnsEmptyResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destFolder = await CreateCustomRoom("Autotest CheckMove NonExist File Room");

        // Act
        var response = (await CheckMoveOrCopyBatch(new BatchRequestDto
        {
            DestFolderId = new(destFolder.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(999999999)],
            FolderIds = []
        }, TestContext.Current.CancellationToken)).Response;

        // Assert
        response.Should().BeEmpty();
    }

    [Fact]
    [Trait("Bug", "81881")]
    public async Task CheckMove_NonExistentDestFolderId_ReturnsNotFound()
    {
        // A non-existent destFolderId used to return 403 (Access denied) instead of 404 (Not found),
        // misleading callers into thinking it was a permissions issue rather than a missing resource.

        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove NonExist Dest.docx", Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                DestFolderId = new(999999999),
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81882")]
    public async Task CheckMove_NoDestFolderId_ReturnsBadRequest()
    {
        // Omitting the required destFolderId used to return 403 (Access denied) instead of 400
        // (Bad request), hiding a client-side input error behind an access-denied response.

        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest CheckMove NoDestFolder.docx", Owner);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CheckMoveOrCopyBatch(new BatchRequestDto
            {
                ConflictResolveType = FileConflictResolveType.Skip,
                FileIds = [new(file.Id)],
                FolderIds = []
            }, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
