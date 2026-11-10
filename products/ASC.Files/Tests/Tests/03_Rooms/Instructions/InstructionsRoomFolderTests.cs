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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

[Trait("Category", "Rooms")]
[Trait("Feature", "Instructions")]
public class InstructionsRoomFolderTests(AspireAppFixture fixture) : InstructionsRoomTestBase(fixture)
{
    [Fact]
    public async Task CreateFolder_InRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Create Folder");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateFolder("Autotest Skills Subfolder", roomId));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateUploadSession_WithRelativePath_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Relative Path");
        var content = Skill("pdf-review");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CreateUploadSessionInFolderAsync(
                roomId,
                new SessionRequest("pdf-review.md", content.Length, relativePath: "nested"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFolder_IntoRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Copy Folder");
        var folder = await CreateFolderInMy("Autotest Skills Folder", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(FolderRequest(roomId, folder.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFolder_IntoRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Move Folder");
        var folder = await CreateFolderInMy("Autotest Skills Folder", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(FolderRequest(roomId, folder.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private static BatchRequestDto FolderRequest(int destFolderId, int folderId)
    {
        return new BatchRequestDto
        {
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [],
            FolderIds = [new(folderId)],
            ReturnSingleOperation = true
        };
    }
}
