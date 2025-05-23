// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using ASC.Files.Tests.Factory;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class UpdateFileTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task RenameFile_ValidTitle_ReturnsUpdatedFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdFile = await CreateFile("file_to_rename.docx", FolderType.USER, Initializer.Owner);
        var newTitle = "renamed_file.docx";
        
        // Act
        var updateParams = new UpdateFile { Title = newTitle };
        var updatedFile = (await _filesFilesApi.UpdateFileAsync(createdFile.Id, updateParams, TestContext.Current.CancellationToken)).Response;
        
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
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdFile = await CreateFile("file_to_rename.docx", FolderType.USER, Initializer.Owner);
        var longFileName = new string('a', 166) + ".docx"; // 166 characters + 5 for extension = 171 characters
        var updateParams = new UpdateFile { Title = longFileName };
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesFilesApi.UpdateFileAsync(
                createdFile.Id, 
                updateParams, 
                cancellationToken: TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(400);
    }
    
    [Fact]
    public async Task LockFile_InMy_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdFile = await CreateFile("file_to_lock.docx", FolderType.USER, Initializer.Owner);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesFilesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken));
        
        exception.ErrorCode.Should().Be(403);
    }
    
    [Fact]
    public async Task LockFile_AsOwner_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock", Initializer.Owner);
        var createdFile = await CreateFile("file_to_lock.docx", createdRoom.Id);
        
        // Act
        var result = (await _filesFilesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdFile.Id);
        
        // Verify the file is locked
        var fileInfo = await GetFile(createdFile.Id);
        fileInfo.Locked.Should().BeTrue();
    }
    
    [Fact]
    public async Task UnlockFile_LockedFile_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var createdRoom = await CreateVirtualRoom("room_to_lock", Initializer.Owner);
        var createdFile = await CreateFile("file_to_unlock.docx", createdRoom.Id);
        
        // Lock the file first
        await _filesFilesApi.LockFileAsync(createdFile.Id, new LockFileParameters(true), TestContext.Current.CancellationToken);
        
        // Act
        var result = (await _filesFilesApi.LockFileAsync(createdFile.Id, new LockFileParameters(), TestContext.Current.CancellationToken)).Response;
        
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
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_with_comment.docx", FolderType.USER, Initializer.Owner);
        var newComment = "This is a test comment";
        
        // Act
        var commentParams = new UpdateComment(1, newComment);
        var result = (await _filesOperationsApi.UpdateCommentAsync(file.Id, commentParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        result.Should().Be(newComment);
        
        // Verify comment was updated
        var updatedFile = await GetFile(file.Id);
        updatedFile.Comment.Should().Be(newComment);
    }
}
