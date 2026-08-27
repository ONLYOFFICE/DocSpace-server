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

namespace ASC.Files.Tests.Tests._02_Folders.Deletion;

/// <summary>
/// <c>DELETE /files/folder/{folderId}</c> — positive/functional coverage (immediate delete, move to
/// Trash, deletion of nested subtrees and cross-user deletion) plus the trash filter-by-origin
/// behaviour. Access control lives in <see cref="FolderDeletePermissionsTests"/>.
/// </summary>
/// <remarks>
/// The endpoint publishes the delete and then returns whatever <c>GetOperationResults</c> currently
/// holds, so an already-finished operation can be pruned before the response is built and the array
/// comes back empty. The TypeScript suite asserts it is non-empty, which only holds on a slower
/// deployment; here the assertions are on the outcome — the folder is gone, or it is in the Trash —
/// and any operation that *is* reported must have finished without an error.
/// </remarks>
[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderDeleteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFolder_Immediately_MyDocuments_Owner_FolderGone()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder To Delete Immediately", myId);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFolder_NotImmediately_MyDocuments_Owner_MovesToTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        const string folderTitle = "Autotest Folder To Trash";
        var folder = await CreateFolder(folderTitle, myId);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = false }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert - not permanently deleted, just moved
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        var trash = await GetTrashAsync();
        trash.Folders.Should().Contain(f => f.Title == folderTitle);
    }

    [Fact]
    public async Task DeleteFolder_CreatedByDocSpaceAdmin_Owner_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Cross-User Delete");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(admin);
        var folder = await CreateFolder("Autotest Folder Created By DocSpaceAdmin", room.Id);

        // Act
        await _filesClient.Authenticate(Owner);
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFolder_InsideRoom_Owner_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room For Folder Delete In Room");
        var folder = await CreateFolder("Autotest Folder In Room", room.Id);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFolder_WithNestedSubfolders_Owner_ParentAndChildBothGone()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var parent = await CreateFolder("Autotest Parent Folder", myId);
        var child = await CreateFolder("Autotest Child Folder", parent.Id);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(parent.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(parent.Id, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(child.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFolder_WithFilesInside_Owner_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder With Files", myId);
        await CreateFile("Autotest File Inside Folder.docx", folder.Id);

        // Act
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Count == 0 || results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        // Assert
        results.Should().NotContain(operation => !operation.Finished || !string.IsNullOrEmpty(operation.Error));

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_AlreadyDeleted_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder For Double Delete", myId);

        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { DeleteAfter = true, Immediately = true }, TestContext.Current.CancellationToken)).Response;

        if (results.Any(r => !r.Finished))
        {
            await WaitLongOperation(results.FirstOrDefault()?.Id);
        }

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(
                folder.Id,
                new DeleteFolder(true, true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "79459")]
    public async Task DeleteFolder_NonExistingFolder_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var nonExistingFolderId = 99999; // Non-existing folder ID

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(
                nonExistingFolderId,
                new DeleteFolder(false, true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteFolder_NoPermissions_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var folder = await CreateFolder("folder_no_permissions", FolderType.USER, Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.DeleteFolderAsync(
                folder.Id,
                new DeleteFolder(false, true),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFoldersToTrash_WithoutFolderTypeFilter_ReturnsFoldersFromMyDocumentsAndRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFolder = await CreateFolderInMy("trash_my_folder", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFolder = await CreateFolder("trash_custom_folder", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFolder = await CreateFolder("trash_public_folder", publicRoom.Id);

        var vdrRoom = await CreateVDRRoom("trash_vdr_room");
        var vdrRoomFolder = await CreateFolder("trash_vdr_folder", vdrRoom.Id);

        await MoveFoldersToTrash(myFolder, customRoomFolder, publicRoomFolder, vdrRoomFolder);

        // Act
        var trash = await GetTrashAsync();

        // Assert - without the filter every trashed folder is returned regardless of its original folder type
        trash.Folders.Should().Contain(f => f.Title == myFolder.Title);
        trash.Folders.Should().Contain(f => f.Title == customRoomFolder.Title);
        trash.Folders.Should().Contain(f => f.Title == publicRoomFolder.Title);
        trash.Folders.Should().Contain(f => f.Title == vdrRoomFolder.Title);
    }

    [Fact]
    public async Task MoveFoldersToTrash_FilterByMyDocuments_ReturnsOnlyMyDocumentsFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFolder = await CreateFolderInMy("trash_my_folder", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFolder = await CreateFolder("trash_custom_folder", customRoom.Id);

        await MoveFoldersToTrash(myFolder, customRoomFolder);

        // Act
        var trash = await GetTrashAsync([FolderType.USER]);

        // Assert - the filter narrows the trash to folders originally located in "My documents"
        trash.Folders.Should().Contain(f => f.Title == myFolder.Title);
        trash.Folders.Should().NotContain(f => f.Title == customRoomFolder.Title);
    }

    [Fact]
    public async Task MoveFoldersToTrash_FilterByRooms_ReturnsOnlyRoomFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFolder = await CreateFolderInMy("trash_my_folder", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFolder = await CreateFolder("trash_custom_folder", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFolder = await CreateFolder("trash_public_folder", publicRoom.Id);

        await MoveFoldersToTrash(myFolder, customRoomFolder, publicRoomFolder);

        // Act - VirtualRooms is the common ancestor of every room, so it selects all folders originally from rooms
        var trash = await GetTrashAsync([FolderType.VirtualRooms]);

        // Assert
        trash.Folders.Should().Contain(f => f.Title == customRoomFolder.Title);
        trash.Folders.Should().Contain(f => f.Title == publicRoomFolder.Title);
        trash.Folders.Should().NotContain(f => f.Title == myFolder.Title);
    }

    [Theory]
    [MemberData(nameof(RoomTypeFilterCases))]
    public async Task MoveFoldersToTrash_FilterBySpecificRoomType_ReturnsOnlyThatRoomFolders(RoomType roomType, FolderType folderTypeFilter)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFolder = await CreateFolderInMy("trash_my_folder", Owner);

        var targetRoom = await CreateRoom(roomType, "trash_target_room");
        var targetRoomFolder = await CreateFolder("trash_target_folder", targetRoom.Id);

        var otherRoom = await CreateCustomRoom("trash_other_room");
        var otherRoomFolder = await CreateFolder("trash_other_folder", otherRoom.Id);

        await MoveFoldersToTrash(myFolder, targetRoomFolder, otherRoomFolder);

        // Act
        var trash = await GetTrashAsync([folderTypeFilter]);

        // Assert
        trash.Folders.Should().Contain(f => f.Title == targetRoomFolder.Title);
        trash.Folders.Should().NotContain(f => f.Title == myFolder.Title);

        if (roomType != RoomType.CustomRoom)
        {
            trash.Folders.Should().NotContain(f => f.Title == otherRoomFolder.Title);
        }
    }

    [Fact]
    public async Task MoveFoldersToTrash_FilterByMultipleFolderTypes_ReturnsFoldersFromAllRequestedTypes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFolder = await CreateFolderInMy("trash_my_folder", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFolder = await CreateFolder("trash_custom_folder", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFolder = await CreateFolder("trash_public_folder", publicRoom.Id);

        await MoveFoldersToTrash(myFolder, customRoomFolder, publicRoomFolder);

        // Act
        var trash = await GetTrashAsync([FolderType.USER, FolderType.CustomRoom]);

        // Assert
        trash.Folders.Should().Contain(f => f.Title == myFolder.Title);
        trash.Folders.Should().Contain(f => f.Title == customRoomFolder.Title);
        trash.Folders.Should().NotContain(f => f.Title == publicRoomFolder.Title);
    }

    public static TheoryData<RoomType, FolderType> RoomTypeFilterCases =>
        new()
        {
            { RoomType.CustomRoom, FolderType.CustomRoom },
            { RoomType.PublicRoom, FolderType.PublicRoom },
            { RoomType.EditingRoom, FolderType.EditingRoom },
            { RoomType.VirtualDataRoom, FolderType.VirtualDataRoom }
        };

    /// <summary>
    /// Reads the Trash, polling on a deadline until it holds at least <paramref name="expectedFolders"/>
    /// folders (or the deadline passes, returning the last observed state). The move-to-trash
    /// operation reports finished before the listing reflects it, and under a full-suite run that
    /// lag regularly outlives a bare read.
    /// </summary>
    private async Task<FolderContentDtoInteger> GetTrashAsync(List<FolderType>? folderType = null, int expectedFolders = 1)
    {
        var trashId = await GetTrashFolderIdAsync(Owner);
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var trash = (await _foldersApi.GetFolderByFolderIdAsync(trashId, folderType: folderType?.Select(r => (int)r).ToList(), cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (trash.Folders.Count >= expectedFolders || DateTime.UtcNow >= deadline)
            {
                return trash;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    private async Task MoveFoldersToTrash(params FolderDtoInteger[] folders)
    {
        foreach (var folder in folders)
        {
            await DeleteFolderAndWaitForCompletion(folder);
        }
    }

    private async Task DeleteFolderAndWaitForCompletion(FolderDtoInteger folder)
    {
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { Immediately = false }, TestContext.Current.CancellationToken)).Response;
        var operationId = results.FirstOrDefault()?.Id;

        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    private async Task<FolderDtoInteger> CreateRoom(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => await CreateCustomRoom(title),
        RoomType.PublicRoom => await CreatePublicRoom(title),
        RoomType.EditingRoom => await CreateCollaborationRoom(title),
        RoomType.VirtualDataRoom => await CreateVDRRoom(title),
        RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type")
    };
}
