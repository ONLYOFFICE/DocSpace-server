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
/// Moving inside one room by a Content creator: the move needs the delete right on the source, so a Content creator
/// moves a file of their own into a folder of the room, and is refused with 403 for the Owner's file.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileMoveWithinRoomTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task MoveFile_ContentCreatorOwnFileWithinRoom_Moved()
    {
        // Arrange
        var (room, creator) = await CreateRoomWithContentCreator();

        await _filesClient.Authenticate(creator);
        var folder = await CreateFolder("Autotest Move Within Own Folder", room.Id);
        var file = await CreateFile("Autotest Move Within Own File.docx", room.Id);

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(BuildMove(folder.Id, file.Id), TestContext.Current.CancellationToken)).Response;
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (content.Files ?? []).Select(f => f.Title).Should().Contain(file.Title);
    }

    [Fact]
    public async Task MoveFile_ContentCreatorOwnersFileWithinRoom_Returns403()
    {
        // Arrange
        var (room, creator) = await CreateRoomWithContentCreator();
        var ownersFile = await CreateFile("Autotest Move Within Owners File.docx", room.Id);

        await _filesClient.Authenticate(creator);
        var folder = await CreateFolder("Autotest Move Within Dest Folder", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.MoveBatchItemsAsync(BuildMove(folder.Id, ownersFile.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private async Task<(FolderDto Room, User Creator)> CreateRoomWithContentCreator()
    {
        var creator = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Move Within Room");
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);

        return (room, creator);
    }

    private static BatchRequestDto BuildMove(int destFolderId, int fileId)
    {
        return new BatchRequestDto
        {
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };
    }
}
