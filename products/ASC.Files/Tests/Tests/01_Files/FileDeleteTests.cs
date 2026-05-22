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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileDeleteTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteFile_FolderMy_Owner_ReturnsOk()
    {
        var createdFile = await CreateFileInMy("test.docx", Initializer.Owner);

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
        await _filesClient.Authenticate(Initializer.Owner);
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
        await _filesClient.Authenticate(Initializer.Owner);

        var file = await CreateFile("file_no_permissions.docx", FolderType.USER, Initializer.Owner);

        var user = await Initializer.InviteContact(EmployeeType.User);
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
        await _filesClient.Authenticate(Initializer.Owner);
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var sourceFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        var lockedFile = (await _filesApi.LockFileAsync(sourceFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;

        var user = await Initializer.InviteContact(EmployeeType.User);
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
        await _filesClient.Authenticate(Initializer.Owner);

        var file = await CreateFile("locked_file.docx", FolderType.USER, Initializer.Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);

        var user = await Initializer.InviteContact(EmployeeType.User);
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
        await _filesClient.Authenticate(Initializer.Owner);

        var file = await CreateFileInMy("file_security_info.docx", Initializer.Owner);
        var user1 = await Initializer.InviteContact(EmployeeType.User);

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
        await _filesClient.Authenticate(Initializer.Owner);

        var file = await CreateFile("editing_file.docx", FolderType.USER, Initializer.Owner);
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
        await _filesClient.Authenticate(Initializer.Owner);
        var myId = await GetUserFolderIdAsync(Initializer.Owner);
        await MoveFileToTrash(myId);
    }

    [Fact]
    public async Task MoveFileToTrash_CustomRoom_Owner_ReturnsOk()
    {
        var createdRoom = await CreateVirtualRoom("room");
        await MoveFileToTrash(createdRoom.Id);
    }

    private async Task MoveFileToTrash(int roomId)
    {
        var trashId = await GetTrashFolderIdAsync(Initializer.Owner);

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
