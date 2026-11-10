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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// Who may call <c>PUT /api/2.0/files/fileops/copy</c> and what access level a destination room
/// requires, across roles (Owner, DocSpaceAdmin, RoomAdmin, User, Guest) and room-invitation
/// levels (<see cref="FileShare.ContentCreator"/>/<see cref="FileShare.Read"/>/
/// <see cref="FileShare.Editing"/>).
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CopyPermissionsTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "65580")]
    public async Task CopyFolder_User_CannotCopyARoom_ReturnsAccessDenied()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyPerm NoCopyRoom Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var userMyDocsId = await GetUserFolderIdAsync(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(userMyDocsId, folderIds: [room.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CopyFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Anon File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Anon Room");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CopyFile_Owner_CanCopyFileToRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Owner File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Owner Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_DocSpaceAdmin_CanCopyFileToRoom()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var myDocsId = await GetUserFolderIdAsync(admin);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Admin File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Admin Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_RoomAdmin_CanCopyFileToOwnRoom()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var myDocsId = await GetUserFolderIdAsync(roomAdmin);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm RoomAdmin File.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm RoomAdmin Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_UserWithContentCreatorInDestRoom_CanCopy()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm User Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(destRoom.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm User File.docx", myDocsId);

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_UserWithoutAccessToDestRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm UserNoAccess Room");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm UserNoAccess File.docx", myDocsId);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_GuestWithoutAccessToDestRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm GuestNoAccess Room");
        var guest = await InviteMember(EmployeeType.Guest);

        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Guest File.docx", myDocsId);

        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_RoomAdminContentCreator_CopiesToLevel3Subfolder()
    {
        // Arrange: catches ContentCreator being denied write access to a deeply nested subfolder.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyBatch Perm RoomAdmin L3 Room");
        var level2 = await CreateFolder("Autotest Perm RoomAdmin L3 L2", room.Id);
        var level3 = await CreateFolder("Autotest Perm RoomAdmin L3 L3", level2.Id);

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.ContentCreator);

        await _filesClient.Authenticate(roomAdmin);
        var myDocsId = await GetUserFolderIdAsync(roomAdmin);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm RoomAdmin L3 File.docx", myDocsId);

        // Act
        await CopyAndWait(BuildCopyRequest(level3.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(level3.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_UserContentCreator_CopiesToLevel3Subfolder()
    {
        // Arrange: catches a plain User with ContentCreator being denied the same nested write.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyBatch Perm User L3 Room");
        var level2 = await CreateFolder("Autotest Perm User L3 L2", room.Id);
        var level3 = await CreateFolder("Autotest Perm User L3 L3", level2.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm User L3 File.docx", myDocsId);

        // Act
        await CopyAndWait(BuildCopyRequest(level3.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(level3.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_UserWithReadAccessToDestRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Read Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(destRoom.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Read File.docx", myDocsId);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_UserWithEditingAccessToDestRoom_ReturnsError()
    {
        // Arrange: Editing allows editing existing files but not adding new content.
        await _filesClient.Authenticate(Owner);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Editing Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(destRoom.Id, user, FileShare.Editing);

        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);
        var sourceFile = await CreateFile("Autotest CopyBatch Perm Editing File.docx", myDocsId);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CopyBatchItemsAsync(
                BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CopyFile_GuestWithContentCreatorAccess_CanCopy()
    {
        // Arrange: the guest has no personal MyDocs, so the source file lives in a room it can read.
        await _filesClient.Authenticate(Owner);
        var guest = await InviteMember(EmployeeType.Guest);

        var srcRoom = await CreateCustomRoom("Autotest CopyBatch Perm Guest CC SrcRoom");
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm Guest CC DestRoom");

        await InviteToRoom(srcRoom.Id, guest, FileShare.Read);
        await InviteToRoom(destRoom.Id, guest, FileShare.ContentCreator);

        var sourceFile = await CreateFile("Autotest CopyBatch Perm Guest CC File.docx", srcRoom.Id);

        await _filesClient.Authenticate(guest);

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_UserContentCreatorInBothRooms_CanCopyBetweenRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyBatch Perm CC SrcRoom");
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Perm CC DestRoom");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(srcRoom.Id, user, FileShare.ContentCreator);
        await InviteToRoom(destRoom.Id, user, FileShare.ContentCreator);

        var sourceFile = await CreateFile("Autotest CopyBatch Perm CC SrcFile.docx", srcRoom.Id);

        await _filesClient.Authenticate(user);

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destRoom.Id)).Should().Contain(sourceFile.Title);
    }

    /// <summary>
    /// Copying a folder from a room to My Documents as a ContentCreator used to return 403 even
    /// though copying a single file from the same folder succeeded (BUG 81906): the folder-level
    /// permission check ignored the ContentCreator role. Fixed: the folder copy now succeeds too.
    /// </summary>
    [Fact]
    [Trait("Bug", "81906")]
    public async Task CopyFolder_UserContentCreator_CopiesFolderFromRoomToMyDocs()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyFolder Perm Room");
        var folder = await CreateFolder("Autotest CopyFolder Perm Folder", room.Id);
        await CreateFile("Autotest CopyFolder Perm File.docx", folder.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var myDocsId = await GetUserFolderIdAsync(user);

        // Act
        await CopyAndWait(BuildCopyRequest(myDocsId, folderIds: [folder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(myDocsId)).Should().Contain(folder.Title);
    }

    /// <summary>
    /// Production report: the portal owner copies a folder with documents from a room of their own
    /// into a subfolder of a room another user created and shared with the owner as RoomManager.
    /// The copy must succeed, not answer 403.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin, RoomType.CustomRoom, RoomType.CustomRoom)]
    [InlineData(EmployeeType.RoomAdmin, RoomType.VirtualDataRoom, RoomType.VirtualDataRoom)]
    [InlineData(EmployeeType.RoomAdmin, RoomType.EditingRoom, RoomType.CustomRoom)]
    [InlineData(EmployeeType.RoomAdmin, RoomType.PublicRoom, RoomType.CustomRoom)]
    [InlineData(EmployeeType.DocSpaceAdmin, RoomType.CustomRoom, RoomType.CustomRoom)]
    public async Task CopyFolder_OwnerAsRoomManagerOfForeignRoom_CopiesFolderFromOwnRoomToSubfolder(
        EmployeeType destRoomCreatorType,
        RoomType destRoomType,
        RoomType srcRoomType)
    {
        // Arrange
        var destRoomCreator = await InviteMember(destRoomCreatorType);
        await _filesClient.Authenticate(destRoomCreator);
        var destRoom = await CreateRoom(new CreateRoomRequestDto("Autotest CopyFolder Foreign Dest Room", roomType: destRoomType));
        var destSubfolder = await CreateFolder("Autotest CopyFolder Foreign Dest Subfolder", destRoom.Id);
        await InviteToRoom(destRoom.Id, Owner, FileShare.RoomManager);

        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateRoom(new CreateRoomRequestDto("Autotest CopyFolder Foreign Src Room", roomType: srcRoomType));
        var folder = await CreateFolder("Autotest CopyFolder Foreign Folder", srcRoom.Id);
        const string innerFileTitle = "Autotest CopyFolder Foreign Inner.docx";
        await CreateFile(innerFileTitle, folder.Id);
        var nestedFolder = await CreateFolder("Autotest CopyFolder Foreign Nested", folder.Id);
        const string nestedFileTitle = "Autotest CopyFolder Foreign Nested.docx";
        await CreateFile(nestedFileTitle, nestedFolder.Id);

        // Act
        await CopyAndWait(BuildCopyRequest(destSubfolder.Id, folderIds: [folder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(destSubfolder.Id)).Should().Contain(folder.Title);
        var copiedFolderId = await FindEntryIdByTitle(destSubfolder.Id, folder.Title, inFolders: true);
        var copiedContent = await GetFolderContent(copiedFolderId);
        FileTitles(copiedContent).Should().Contain(innerFileTitle);
        FolderTitles(copiedContent).Should().Contain(nestedFolder.Title);
        var copiedNestedId = await FindEntryIdByTitle(copiedFolderId, nestedFolder.Title, inFolders: true);
        FileTitles(await GetFolderContent(copiedNestedId)).Should().Contain(nestedFileTitle);
    }

    /// <summary>
    /// The production case behind the test above: the source room is shared with a group, its
    /// members add documents to the owner's folder, and none of them is a member of the destination
    /// room. The owner copying that folder must still succeed. It used to answer 403 "Access denied"
    /// as soon as a member had one of those documents open in the editor: the folder check ran the
    /// "file is being edited" test of a move on every file of the subtree. Fixed: a copy skips it, as
    /// copying a single file already did.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CopyFolder_OwnerAsRoomManagerOfForeignRoom_CopiesFolderWithFilesOfOtherMembers(bool shareViaGroup, bool memberFileOpenForEditing)
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var destRoom = await CreateCustomRoom("Autotest CopyFolder Members Dest Room");
        var destSubfolder = await CreateFolder("Autotest CopyFolder Members Dest Subfolder", destRoom.Id);
        await InviteToRoom(destRoom.Id, Owner, FileShare.RoomManager);

        await _filesClient.Authenticate(Owner);
        var member = await InviteMember(EmployeeType.User);
        var srcRoom = await CreateCustomRoom("Autotest CopyFolder Members Src Room");
        if (shareViaGroup)
        {
            var group = (await _groupApi.AddGroupAsync(
                new GroupRequestDto([member.Id], member.Id, $"Autotest CopyFolder Group {Guid.NewGuid():N}"),
                TestContext.Current.CancellationToken)).Response;
            await _roomsApi.SetRoomSecurityAsync(srcRoom.Id, new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = group.Id, Access = FileShare.ContentCreator }],
                Notify = false
            }, TestContext.Current.CancellationToken);
        }
        else
        {
            await InviteToRoom(srcRoom.Id, member, FileShare.ContentCreator);
        }

        var folder = await CreateFolder("Autotest CopyFolder Members Folder", srcRoom.Id);
        const string ownerFileTitle = "Autotest CopyFolder Members Owner.docx";
        await CreateFile(ownerFileTitle, folder.Id);

        await _filesClient.Authenticate(member);
        const string memberFileTitle = "Autotest CopyFolder Members Member.docx";
        var memberFile = await CreateFile(memberFileTitle, folder.Id);
        if (memberFileOpenForEditing)
        {
            // copying only reads the file, so a co-author editing it must not block the copy
            await _filesApi.StartEditFileAsync(memberFile.Id, new StartEditRequest(true), TestContext.Current.CancellationToken);
        }

        await _filesClient.Authenticate(Owner);

        // Act
        await CopyAndWait(BuildCopyRequest(destSubfolder.Id, folderIds: [folder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(destSubfolder.Id)).Should().Contain(folder.Title);
        var copiedFolderId = await FindEntryIdByTitle(destSubfolder.Id, folder.Title, inFolders: true);
        FileTitles(await GetFolderContent(copiedFolderId)).Should().Contain([ownerFileTitle, memberFileTitle]);
    }

    [Fact]
    public async Task CopyFile_OwnerAsRoomManagerOfForeignRoom_CopiesFileFromOwnRoomToSubfolder()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var destRoom = await CreateCustomRoom("Autotest CopyFile Foreign Dest Room");
        var destSubfolder = await CreateFolder("Autotest CopyFile Foreign Dest Subfolder", destRoom.Id);
        await InviteToRoom(destRoom.Id, Owner, FileShare.RoomManager);

        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyFile Foreign Src Room");
        var sourceFile = await CreateFile("Autotest CopyFile Foreign File.docx", srcRoom.Id);

        // Act
        await CopyAndWait(BuildCopyRequest(destSubfolder.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(destSubfolder.Id)).Should().Contain(sourceFile.Title);
    }
}
