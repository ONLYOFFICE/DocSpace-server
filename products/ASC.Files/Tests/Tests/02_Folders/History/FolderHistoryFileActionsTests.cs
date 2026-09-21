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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - file and (sub)folder CRUD, move/copy and
/// ordering all being recorded in the room's history. Room-level settings and membership actions
/// are covered by <see cref="FolderHistoryRoomSettingsTests"/> and
/// <see cref="FolderHistoryMembershipTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryFileActionsTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_SubfolderCreatedInRoom_ContainsFolderCreated()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FolderCreated In Room");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FolderCreated);

        // Act
        await CreateFolder("New Subfolder In Room", room.Id);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FolderCreated, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderMovedToTrash_ContainsFolderMovedToTrash()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History FolderToTrash");
        var subfolder = await CreateFolder("Subfolder To Trash", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FolderMovedToTrash);

        // Act
        await _foldersApi.DeleteFolderAsync(subfolder.Id, new DeleteFolder(deleteAfter: false, immediately: false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FolderMovedToTrash);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderPermanentlyDeleted_ContainsFolderDeleted()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FolderDeleted");
        var subfolder = await CreateFolder("Subfolder Permanently Deleted", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FolderDeleted);

        // Act
        await _foldersApi.DeleteFolderAsync(subfolder.Id, new DeleteFolder(deleteAfter: false, immediately: true), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FolderDeleted, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileMovedToTrash_ContainsFileMovedToTrash()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History FileToTrash");
        var file = await CreateFile("File To Trash", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileMovedToTrash);

        // Act
        await _filesApi.DeleteFileAsync(file.Id, new Delete(immediately: false), cancellationToken: TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileMovedToTrash);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderMovedToAnotherRoom_ContainsFolderMoved()
    {
        // Arrange
        var sourceRoom = await CreateCustomRoom("Autotest Folder History FolderMoved Source");
        var destRoom = await CreateCustomRoom("Autotest Folder History FolderMoved Dest");
        var subfolder = await CreateFolder("Subfolder To Move", sourceRoom.Id);

        var beforeIds = await GetHistoryActionIdsAsync(sourceRoom.Id);
        beforeIds.Should().NotContain(MessageAction.FolderMoved);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto
            {
                FolderIds = [new BatchRequestDtoAllOfFolderIds(subfolder.Id)],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(destRoom.Id),
                DeleteAfter = false
            },
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(sourceRoom.Id, MessageAction.FolderMoved);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderCopiedToAnotherRoom_ContainsFolderCopied()
    {
        // Arrange
        var sourceRoom = await CreateCustomRoom("Autotest Folder History FolderCopied Source");
        var destRoom = await CreateCustomRoom("Autotest Folder History FolderCopied Dest");
        var subfolder = await CreateFolder("Subfolder To Copy", sourceRoom.Id);

        var beforeIds = await GetHistoryActionIdsAsync(sourceRoom.Id);
        beforeIds.Should().NotContain(MessageAction.FolderCopied);

        // Act
        await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto
            {
                FolderIds = [new BatchRequestDtoAllOfFolderIds(subfolder.Id)],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(destRoom.Id),
                DeleteAfter = false
            },
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(sourceRoom.Id, MessageAction.FolderCopied);
    }

    [Fact]
    public async Task GetFolderHistory_FileMovedToAnotherRoom_ContainsFileMoved()
    {
        // Arrange
        var sourceRoom = await CreateCustomRoom("Autotest Folder History FileMoved Source");
        var destRoom = await CreateCustomRoom("Autotest Folder History FileMoved Dest");
        var file = await CreateFile("File To Move", sourceRoom.Id);

        var beforeIds = await GetHistoryActionIdsAsync(sourceRoom.Id);
        beforeIds.Should().NotContain(MessageAction.FileMoved);

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto
            {
                FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(destRoom.Id),
                DeleteAfter = false
            },
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(sourceRoom.Id, MessageAction.FileMoved);
    }

    [Fact]
    public async Task GetFolderHistory_FileCopiedToAnotherRoom_ContainsFileCopied()
    {
        // Arrange
        var sourceRoom = await CreateCustomRoom("Autotest Folder History FileCopied Source");
        var destRoom = await CreateCustomRoom("Autotest Folder History FileCopied Dest");
        var file = await CreateFile("File To Copy", sourceRoom.Id);

        var beforeIds = await GetHistoryActionIdsAsync(sourceRoom.Id);
        beforeIds.Should().NotContain(MessageAction.FileCopied);

        // Act
        await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto
            {
                FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
                DestFolderId = new BatchRequestDtoAllOfDestFolderId(destRoom.Id),
                DeleteAfter = false
            },
            TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(sourceRoom.Id, MessageAction.FileCopied);
    }

    [Fact]
    public async Task GetFolderHistory_SubfolderRenamed_ContainsFolderRenamed()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FolderRenamed");
        var subfolder = await CreateFolder("Subfolder Before Rename", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FolderRenamed);

        // Act
        await _foldersApi.RenameFolderAsync(subfolder.Id, new CreateFolder("Subfolder After Rename"), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FolderRenamed, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileCreatedInRoom_ContainsFileCreated()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History FileCreated");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileCreated);

        // Act
        await CreateFile("New File In Room", room.Id);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileCreated);
    }

    [Fact]
    public async Task GetFolderHistory_FileUploadedViaChunkedSession_ContainsFileUploaded()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FileUploaded");

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileUploaded);

        var content = "test content"u8.ToArray();

        // Act
        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            room.Id,
            new SessionRequest("Uploaded File.docx", content.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        await using (var chunkStream = new MemoryStream(content))
        {
            await _filesOperationsApi.UploadAsyncSessionAsync(room.Id, session.Id, 1, new FileParameter(chunkStream), TestContext.Current.CancellationToken);
        }

        await _filesOperationsApi.FinalizeSessionAsync(room.Id, session.Id, TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileUploaded, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileRenamed_ContainsFileRenamed()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FileRenamed");
        var file = await CreateFile("File Before Rename", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileRenamed);

        // Act
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile(title: "File After Rename"), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileRenamed, displayName, timeoutSeconds: 30);
    }

    [Fact]
    public async Task GetFolderHistory_FilePermanentlyDeleted_ContainsFileDeleted()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FileDeleted");
        var file = await CreateFile("File To Delete Permanently", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileDeleted);

        // Act
        await _filesApi.DeleteFileAsync(file.Id, new Delete(immediately: true), cancellationToken: TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileDeleted, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileLocked_ContainsFileLocked()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FileLocked");
        var file = await CreateFile("File To Lock", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileLocked);

        // Act
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(lockFile: true), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileLocked, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileUnlocked_ContainsFileUnlocked()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History FileUnlocked");
        var file = await CreateFile("File To Unlock", room.Id);
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(lockFile: true), TestContext.Current.CancellationToken);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileUnlocked);

        // Act
        await _filesApi.LockFileAsync(file.Id, new LockFileParameters(lockFile: false), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileUnlocked, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FileOrderSet_ContainsFileIndexChanged()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateVirtualRoom("Autotest Folder History FileIndexChanged");
        var file = await CreateFile("File To Reorder", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.FileIndexChanged);

        // Act
        await _filesApi.SetFileOrderAsync(file.Id, new OrderRequestDto(5), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.FileIndexChanged, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_FolderOrderSet_ContainsFolderIndexChanged()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateVirtualRoom("Autotest Folder History FolderIndexChanged");
        var subfolder = await CreateFolder("Subfolder To Reorder", room.Id);

        var beforeIds = await GetHistoryActionIdsAsync(subfolder.Id);
        beforeIds.Should().NotContain(MessageAction.FolderIndexChanged);

        // Act
        await _foldersApi.SetFolderOrderAsync(subfolder.Id, new OrderRequestDto(5), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(subfolder.Id, MessageAction.FolderIndexChanged, displayName);
    }
}
