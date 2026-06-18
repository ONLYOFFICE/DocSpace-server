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
public class FileUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<FileShare> SharesWithRightsToLock =>
    [
        FileShare.RoomManager,
        FileShare.ContentCreator
    ];
    public static TheoryData<FileShare> SharesWithoutRightsToLock =>
    [
        FileShare.Editing,
        FileShare.FillForms,
        FileShare.Read
    ];
    
    [Fact]
    public async Task RenameFile_ValidTitle_ReturnsUpdatedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdFile = await CreateFileInMy("file_to_rename.docx", Owner);
        var newTitle = "renamed_file.docx";
        
        // Act
        var updateParams = new UpdateFile { Title = newTitle };
        var updatedFile = (await _filesApi.UpdateFileAsync(createdFile.Id, updateParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        updatedFile.Should().NotBeNull();
        updatedFile.Id.Should().Be(createdFile.Id);
        updatedFile.Title.Should().Be(updateParams.Title);
        updatedFile.Title.Should().Be(newTitle);
    }
        
    [Fact]
    public async Task RenameFile_NameLongerThan165Chars_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdFile = await CreateFileInMy("file_to_rename.docx", Owner);
        var longFileName = new string('a', 166) + ".docx"; // 166 characters + 5 for extension = 171 characters
        var updateParams = new UpdateFile { Title = longFileName };
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.UpdateFileAsync(
                createdFile.Id, 
                updateParams, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task LockFile_InMy_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdFile = await CreateFileInMy("file_to_lock.docx", Owner);
        
        // Act & Assert
        var result =  (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;
        
        result.Locked.Should().BeTrue();
    }
    
    [Fact]
    public async Task LockFile_AsOwner_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        
        // Act
        var result = (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is locked
        var fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeTrue();
        
        // Act
        result = (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is unlocked
        fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeNull();
    }
    
    [Theory]
    [MemberData(nameof(SharesWithRightsToLock))]
    public async Task LockFile_ReturnsSuccess(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        
        await _roomsApi.SetRoomSecurityAsync(createdRoom.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = roomAdmin.Id, Access = fileShare }
            ]
        }, TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(roomAdmin);
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        
        // Act
        var result = (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is locked
        var fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeTrue();
        
        // Act
        result = (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is unlocked
        fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeNull();
    }
    
    [Theory]
    [MemberData(nameof(SharesWithoutRightsToLock))]
    public async Task LockFile_Returns403(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        
        await _roomsApi.SetRoomSecurityAsync(createdRoom.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = roomAdmin.Id, Access = fileShare }
            ]
        }, TestContext.Current.CancellationToken);
        
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        await _filesClient.Authenticate(roomAdmin);
        
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(403);
    }
    
    [Fact]
    public async Task LockFileContentCreator_DifferentOwner_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var roomAdmin1 = await InviteContact(EmployeeType.RoomAdmin);
        var roomAdmin2 = await InviteContact(EmployeeType.RoomAdmin);
        
        await _roomsApi.SetRoomSecurityAsync(createdRoom.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = roomAdmin1.Id, Access = FileShare.ContentCreator },
                new RoomInvitation { Id = roomAdmin2.Id, Access = FileShare.ContentCreator }
            ]
        }, TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(roomAdmin1);
        
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);

        await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(roomAdmin2);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(), TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(403);
    }
    
    [Fact]
    public async Task LockFileRoomManager_DifferentOwner_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock");
        var roomAdmin1 = await InviteContact(EmployeeType.RoomAdmin);
        var roomAdmin2 = await InviteContact(EmployeeType.RoomAdmin);
        
        await _roomsApi.SetRoomSecurityAsync(createdRoom.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = roomAdmin1.Id, Access = FileShare.RoomManager },
                new RoomInvitation { Id = roomAdmin2.Id, Access = FileShare.RoomManager }
            ]
        }, TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(roomAdmin1);
        
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        
        // Act
        await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(roomAdmin2);
        
        var result = (await _filesApi.LockFileAsync(createdFile.Id, new LockFileParameters(), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is unlocked
        var fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeNull();
    }
    
    [Fact]
    public async Task UpdateComment_ValidComment_ReturnsUpdatedComment()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        
        var file = await CreateFileInMy("file_with_comment.docx", Owner);
        var newComment = "This is a test comment";
        
        // Act
        var commentParams = new UpdateComment(1, newComment);
        var result = (await _filesOperationsApi.UpdateFileCommentAsync(file.Id, commentParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().Be(newComment);
        
        // Verify comment was updated
        var updatedFile = await GetFile(file.Id);
        updatedFile.Comment.Should().Be(newComment);
    }
}
