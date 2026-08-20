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

namespace ASC.Files.Tests.Tests._01_Files;

[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileDeleteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFile_FolderMy_Owner_ReturnsOk()
    {
        var createdFile = await CreateFileInMy("test.docx", Owner);

        var results = (await _filesApi.DeleteFileAsync(createdFile.Id, new Delete { Immediately = true }, true, TestContext.Current.CancellationToken)).Response;
        var operationId = results.FirstOrDefault()?.Id;

        // Assert
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

        // Verify file no longer exists or has been moved to trash
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.GetFileInfoAsync(createdFile.Id, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteFile_NonExistingFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var nonExistingFileId = 99999; // Non-existing file ID

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                nonExistingFileId,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DeleteFile_NoPermissions_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var file = await CreateFile("file_no_permissions.docx", FolderType.USER, Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                file.Id,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteFile_FileLockedInRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var sourceFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        var lockedFile = (await _filesApi.LockFileAsync(sourceFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var targetFolderId = await GetUserFolderIdAsync(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                lockedFile.Id,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteFile_FileLocked_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var file = await CreateFile("locked_file.docx", FolderType.USER, Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                file.Id,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteFile_SharedFileLocked_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var file = await CreateFileInMy("file_security_info.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);

        var shareInfo = new List<FileShareParams>
        {
            new() { ShareTo = user1.Id, Access = FileShare.ReadWrite },
        };

        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                file.Id,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteFile_EditingFile_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var file = await CreateFile("editing_file.docx", FolderType.USER, Owner);
        await _filesApi.StartEditFileAsync(file.Id, new StartEdit(true), TestContext.Current.CancellationToken);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteFileAsync(
                file.Id,
                new Delete(false, true),
                false,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task MoveFileToTrash_FolderMy_Owner_ReturnsOk()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        await MoveFileToTrash(myId);
    }

    [Fact]
    public async Task MoveFileToTrash_CustomRoom_Owner_ReturnsOk()
    {
        var createdRoom = await CreateVirtualRoom("room");
        await MoveFileToTrash(createdRoom.Id);
    }

    [Fact]
    public async Task MoveFilesToTrash_WithoutFolderTypeFilter_ReturnsFilesFromMyDocumentsAndRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("trash_my.docx", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFile = await CreateFile("trash_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFile = await CreateFile("trash_public.docx", publicRoom.Id);

        var vdrRoom = await CreateVDRRoom("trash_vdr_room");
        var vdrRoomFile = await CreateFile("trash_vdr.docx", vdrRoom.Id);

        await MoveFilesToTrash(myFile, customRoomFile, publicRoomFile, vdrRoomFile);

        // Act
        var trash = await GetTrashAsync();

        // Assert - without the filter every trashed file is returned regardless of its original folder type
        trash.Files.Should().Contain(f => f.Title == myFile.Title);
        trash.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        trash.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        trash.Files.Should().Contain(f => f.Title == vdrRoomFile.Title);
    }

    [Fact]
    public async Task MoveFilesToTrash_FilterByMyDocuments_ReturnsOnlyMyDocumentsFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("trash_my.docx", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFile = await CreateFile("trash_custom.docx", customRoom.Id);

        await MoveFilesToTrash(myFile, customRoomFile);

        // Act
        var trash = await GetTrashAsync([FolderType.USER]);

        // Assert - the filter narrows the trash to files originally located in "My documents"
        trash.Files.Should().Contain(f => f.Title == myFile.Title);
        trash.Files.Should().NotContain(f => f.Title == customRoomFile.Title);
    }

    [Fact]
    public async Task MoveFilesToTrash_FilterByRooms_ReturnsOnlyRoomFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("trash_my.docx", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFile = await CreateFile("trash_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFile = await CreateFile("trash_public.docx", publicRoom.Id);

        await MoveFilesToTrash(myFile, customRoomFile, publicRoomFile);

        // Act - VirtualRooms is the common ancestor of every room, so it selects all files originally from rooms
        var trash = await GetTrashAsync([FolderType.VirtualRooms]);

        // Assert
        trash.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        trash.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        trash.Files.Should().NotContain(f => f.Title == myFile.Title);
    }

    [Theory]
    [MemberData(nameof(RoomTypeFilterCases))]
    public async Task MoveFilesToTrash_FilterBySpecificRoomType_ReturnsOnlyThatRoomFiles(RoomType roomType, FolderType folderTypeFilter)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("trash_my.docx", Owner);

        var targetRoom = await CreateRoom(roomType, "trash_target_room");
        var targetRoomFile = await CreateFile("trash_target.docx", targetRoom.Id);

        var otherRoom = await CreateCustomRoom("trash_other_room");
        var otherRoomFile = await CreateFile("trash_other.docx", otherRoom.Id);

        await MoveFilesToTrash(myFile, targetRoomFile, otherRoomFile);

        // Act
        var trash = await GetTrashAsync([folderTypeFilter]);

        // Assert
        trash.Files.Should().Contain(f => f.Title == targetRoomFile.Title);
        trash.Files.Should().NotContain(f => f.Title == myFile.Title);

        if (roomType != RoomType.CustomRoom)
        {
            trash.Files.Should().NotContain(f => f.Title == otherRoomFile.Title);
        }
    }

    [Fact]
    public async Task MoveFilesToTrash_FilterByMultipleFolderTypes_ReturnsFilesFromAllRequestedTypes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myFile = await CreateFileInMy("trash_my.docx", Owner);

        var customRoom = await CreateCustomRoom("trash_custom_room");
        var customRoomFile = await CreateFile("trash_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("trash_public_room");
        var publicRoomFile = await CreateFile("trash_public.docx", publicRoom.Id);

        await MoveFilesToTrash(myFile, customRoomFile, publicRoomFile);

        // Act
        var trash = await GetTrashAsync([FolderType.USER, FolderType.CustomRoom]);

        // Assert
        trash.Files.Should().Contain(f => f.Title == myFile.Title);
        trash.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        trash.Files.Should().NotContain(f => f.Title == publicRoomFile.Title);
    }

    [Fact]
    public async Task EmptyTrash_FilterByMyDocuments_RemovesOnlyMyDocumentsItems()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myId = await GetUserFolderIdAsync(Owner);
        var myFile = await CreateFileInMy("empty_trash_my.docx", Owner);
        var myFolder = await CreateFolder("empty_trash_my_folder", myId);

        var room = await CreateCustomRoom("empty_trash_room");
        var roomFile = await CreateFile("empty_trash_room.docx", room.Id);
        var roomFolder = await CreateFolder("empty_trash_room_folder", room.Id);

        await MoveToTrashAndWait([myFile, roomFile], [myFolder, roomFolder]);

        // Act
        await EmptyTrashAndWait([FolderType.USER]);

        // Assert - only the items originally located in "My documents" are gone
        var trash = await GetTrashAsync();

        trash.Files.Should().NotContain(f => f.Title == myFile.Title);
        trash.Folders.Should().NotContain(f => f.Title == myFolder.Title);
        trash.Files.Should().Contain(f => f.Title == roomFile.Title);
        trash.Folders.Should().Contain(f => f.Title == roomFolder.Title);
    }

    [Fact]
    public async Task EmptyTrash_FilterByRooms_RemovesOnlyRoomItems()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myId = await GetUserFolderIdAsync(Owner);
        var myFile = await CreateFileInMy("empty_trash_my.docx", Owner);
        var myFolder = await CreateFolder("empty_trash_my_folder", myId);

        var customRoom = await CreateCustomRoom("empty_trash_custom_room");
        var customRoomFile = await CreateFile("empty_trash_custom.docx", customRoom.Id);
        var customRoomFolder = await CreateFolder("empty_trash_custom_folder", customRoom.Id);

        var publicRoom = await CreatePublicRoom("empty_trash_public_room");
        var publicRoomFile = await CreateFile("empty_trash_public.docx", publicRoom.Id);

        await MoveToTrashAndWait([myFile, customRoomFile, publicRoomFile], [myFolder, customRoomFolder]);

        // Act - VirtualRooms is the common ancestor of every room, so it selects everything originally from rooms
        await EmptyTrashAndWait([FolderType.VirtualRooms]);

        // Assert
        var trash = await GetTrashAsync();

        trash.Files.Should().NotContain(f => f.Title == customRoomFile.Title);
        trash.Files.Should().NotContain(f => f.Title == publicRoomFile.Title);
        trash.Folders.Should().NotContain(f => f.Title == customRoomFolder.Title);
        trash.Files.Should().Contain(f => f.Title == myFile.Title);
        trash.Folders.Should().Contain(f => f.Title == myFolder.Title);
    }

    [Fact]
    public async Task EmptyTrash_WithoutFolderTypeFilter_RemovesAllItems()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var myId = await GetUserFolderIdAsync(Owner);
        var myFile = await CreateFileInMy("empty_trash_my.docx", Owner);
        var myFolder = await CreateFolder("empty_trash_my_folder", myId);

        var room = await CreateCustomRoom("empty_trash_room");
        var roomFile = await CreateFile("empty_trash_room.docx", room.Id);
        var roomFolder = await CreateFolder("empty_trash_room_folder", room.Id);

        await MoveToTrashAndWait([myFile, roomFile], [myFolder, roomFolder]);

        // Act
        await EmptyTrashAndWait();

        // Assert - without the filter the whole trash is cleared, as before
        var trash = await GetTrashAsync();

        trash.Files.Should().BeEmpty();
        trash.Folders.Should().BeEmpty();
    }

    public static TheoryData<RoomType, FolderType> RoomTypeFilterCases =>
        new()
        {
            { RoomType.CustomRoom, FolderType.CustomRoom },
            { RoomType.PublicRoom, FolderType.PublicRoom },
            { RoomType.EditingRoom, FolderType.EditingRoom },
            { RoomType.VirtualDataRoom, FolderType.VirtualDataRoom }
        };

    private async Task<FolderContentDtoInteger> GetTrashAsync(List<FolderType>? folderType = null)
    {
        var trashId = await GetTrashFolderIdAsync(Owner);

        return (await _foldersApi.GetFolderByFolderIdAsync(trashId, folderType: folderType?.Select(r=> (int)r).ToList(), cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task MoveFilesToTrash(params FileDtoInteger[] files)
    {
        foreach (var file in files)
        {
            await DeleteFileAndWaitForCompletion(file);
        }
    }

    private async Task MoveToTrashAndWait(FileDtoInteger[] files, FolderDtoInteger[] folders)
    {
        foreach (var file in files)
        {
            var results = (await _filesApi.DeleteFileAsync(file.Id, new Delete { Immediately = false }, true, TestContext.Current.CancellationToken)).Response;

            await WaitOperation(results.FirstOrDefault()?.Id, $"delete file {file.Title}");
        }

        foreach (var folder in folders)
        {
            var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { Immediately = false }, TestContext.Current.CancellationToken)).Response;

            await WaitOperation(results.FirstOrDefault()?.Id, $"delete folder {folder.Title}");
        }
    }

    /// <summary>
    /// Empties the trash, optionally only for the items originally located in the sections of the given types.
    /// The generated SDK client has no folderType parameter yet, so the request is issued directly.
    /// </summary>
    private async Task EmptyTrashAndWait(List<FolderType>? folderType = null)
    {
        var url = "api/2.0/files/fileops/emptytrash?single=true";

        if (folderType != null)
        {
            url = folderType.Aggregate(url, (current, type) => current + $"&folderType={(int)type}");
        }

        using var response = await _filesClient.PutAsync(url, null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var json = JsonDocument.Parse(body);
        var operations = json.RootElement.GetProperty("response");

        if (operations.GetArrayLength() == 0)
        {
            return;
        }

        await WaitOperation(operations[0].GetProperty("id").GetString(), "empty trash");
    }

    /// <summary>
    /// Waits for a file operation to finish. These tests trash several trees before the assertions,
    /// which takes longer than the budget of the shared helper, so the polling is done here.
    /// </summary>
    private async Task WaitOperation(string? operationId, string what)
    {
        if (operationId == null)
        {
            return;
        }

        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < TimeSpan.FromMinutes(2))
        {
            var statuses = (await _filesOperationsApi.GetOperationStatusesAsync(id: operationId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            // a finished operation is eventually dropped from the queue, so an empty answer
            // means it is over - any error it reported was visible while it was still listed
            if (statuses.Count == 0)
            {
                return;
            }

            statuses.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));

            if (statuses.TrueForAll(r => r.Finished))
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.Fail($"The operation '{what}' has not finished in time");
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

    private async Task MoveFileToTrash(int roomId)
    {
        var trashId = await GetTrashFolderIdAsync(Owner);

        var fileInMy = await CreateFile(Guid.NewGuid() + ".docx", roomId);
        var fileInMyNotForDelete = await CreateFile(Guid.NewGuid() + ".docx", roomId);
        var folderInMyFile = await CreateFolder(Guid.NewGuid().ToString(), roomId);
        var fileInMyInsideFolder = await CreateFile(Guid.NewGuid() + ".docx", folderInMyFile.Id);
        var folderInMy = await CreateFolder(Guid.NewGuid().ToString(), roomId);
        var folderInMyInsideFolder = await CreateFolder(Guid.NewGuid().ToString(), folderInMyFile.Id);

        await DeleteFileAndWaitForCompletion(fileInMy);
        await DeleteFileAndWaitForCompletion(fileInMyInsideFolder);
        await DeleteFolderAndWaitForCompletion(folderInMy);
        await DeleteFolderAndWaitForCompletion(folderInMyInsideFolder);

        // Verify file no longer exists or has been moved to trash
        var file = (await _filesApi.GetFileInfoAsync(fileInMy.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        file.Should().NotBeNull();
        file.Id.Should().Be(fileInMy.Id);
        file.FolderId.Should().Be(trashId);

        var trashData = (await _foldersApi.GetFolderByFolderIdAsync(trashId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        trashData.Files.Should().Contain(f => f.Title == fileInMy.Title);
        trashData.Files.Should().Contain(f => f.Title == fileInMyInsideFolder.Title);
        trashData.Files.Should().NotContain(f => f.Title == fileInMyNotForDelete.Title);
        trashData.Folders.Should().Contain(f => f.Title == folderInMy.Title);
        trashData.Folders.Should().Contain(f => f.Title == folderInMyInsideFolder.Title);

        trashData = (await _foldersApi.GetFolderByFolderIdAsync(trashId, roomId: roomId,  cancellationToken: TestContext.Current.CancellationToken)).Response;
        trashData.Files.Should().Contain(f => f.Title == fileInMy.Title);
        trashData.Files.Should().Contain(f => f.Title == fileInMyInsideFolder.Title);
        trashData.Files.Should().NotContain(f => f.Title == fileInMyNotForDelete.Title);
        trashData.Folders.Should().Contain(f => f.Title == folderInMy.Title);
        trashData.Folders.Should().Contain(f => f.Title == folderInMyInsideFolder.Title);
    }

    private async Task DeleteFileAndWaitForCompletion(FileDtoInteger fileInMy)
    {
        var results = (await _filesApi.DeleteFileAsync(fileInMy.Id, new Delete { Immediately = false }, true, TestContext.Current.CancellationToken)).Response;
        var operationId = results.FirstOrDefault()?.Id;

        // Assert
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }

    private async Task DeleteFolderAndWaitForCompletion(FolderDtoInteger folder)
    {
        var results = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder { Immediately = false }, TestContext.Current.CancellationToken)).Response;
        var operationId = results.FirstOrDefault()?.Id;

        // Assert
        if (results.Any(r => !r.Finished))
        {
            results = await WaitLongOperation(operationId);
        }

        results.Should().NotContain(x => !string.IsNullOrEmpty(x.Error));
    }
}
