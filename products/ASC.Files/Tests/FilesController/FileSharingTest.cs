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

using FileShare = Docspace.Model.FileShare;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class FileSharingTest(
    FilesApiFactory filesFactory, 
    WebApplicationFactory<WebApiProgram> apiFactory, 
    WebApplicationFactory<PeopleProgram> peopleFactory,
    WebApplicationFactory<FilesServiceProgram> filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    //   FileShare.None
    public static TheoryData<FileShare> Data =>
    [
        FileShare.Editing, FileShare.CustomFilter, FileShare.Review, FileShare.Comment, FileShare.Read, FileShare.FillForms, FileShare.Restrict
    ];
    
    public static TheoryData<FileShare> InvalidData =>
    [
        FileShare.None, FileShare.ReadWrite, FileShare.Varies, FileShare.RoomManager, FileShare.ContentCreator
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreatePrimaryExternalLink_ValidFile_ReturnsLinkData(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_to_share.docx", FolderType.USER, Initializer.Owner);
        
        // Act
        var linkParams = new FileLinkRequest(access: fileShare);
        
        var result = (await _filesFilesApi.CreatePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        //result.ShareLink.Should().NotBeNullOrEmpty();
        result.Access.Should().Be(fileShare); // Read access
    }

    [Theory]
    [MemberData(nameof(InvalidData))]
    public async Task CreatePrimaryExternalLink_ValidFile_ReturnsError(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_to_share.docx", FolderType.USER, Initializer.Owner);
        
        // Act
        var linkParams = new FileLinkRequest(access: fileShare);

        await Assert.ThrowsAsync<Docspace.Client.ApiException>(async () => 
            await _filesFilesApi.CreatePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task GetFilePrimaryExternalLink_ExistingLink_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_with_link.docx", FolderType.USER, Initializer.Owner);
        
        // Create a primary external link first
        var linkParams = new FileLinkRequest(access: FileShare.Read);
        
        await _filesFilesApi.CreatePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken);
        
        // Act
        var result = (await _filesFilesApi.GetFilePrimaryExternalLinkAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        //result.ShareLink.Should().NotBeNullOrEmpty();
        result.Access.Should().Be(FileShare.Read); // Read access
    }
    

    [Fact]
    public async Task GetLinks_WithMultipleLinks_ReturnsAllLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_with_multiple_links.docx", FolderType.USER, Initializer.Owner);
        
        // Create a primary external link
        var primaryLinkParams = new FileLinkRequest(access: FileShare.Read);
        
        await _filesFilesApi.CreatePrimaryExternalLinkAsync(file.Id, primaryLinkParams, TestContext.Current.CancellationToken);
        
        // Create an additional external link
        var additionalLinkParams = new FileLinkRequest(access: FileShare.Editing);
        
        await _filesFilesApi.SetExternalLinkAsync(file.Id, additionalLinkParams, TestContext.Current.CancellationToken);
        
        // Act
        var links = (await _filesFilesApi.GetLinksAsync(file.Id, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        links.Should().NotBeNull();
        links.Should().HaveCountGreaterThanOrEqualTo(2);
        links.Should().Contain(link => link.Access == FileShare.Read); // Primary link with read access
        links.Should().Contain(link => link.Access == FileShare.Editing); // Additional link with editing access
    }
    
    // [Fact]
    // public async Task SetExternalLink_UpdateExistingLink_ReturnsUpdatedLink()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     
    //     var file = await CreateFile("file_update_link.docx", FolderType.USER, Initializer.Owner);
    //     
    //     // Create initial external link
    //     var initialLinkParams = new FileLinkRequest(
    //         access: FileShare.ReadWrite, // Read access
    //         primary: true
    //     );
    //     
    //     var initialLink = (await _filesFilesApi.CreatePrimaryExternalLinkAsync(file.Id, initialLinkParams, TestContext.Current.CancellationToken)).Response;
    //     
    //     // Act - Update the link
    //     var updateLinkParams = new FileLinkRequest(
    //         access: FileShare.Read, // Read/Write access
    //         linkId: initialLink
    //     );
    //     
    //     var updatedLink = (await _filesFilesApi.SetExternalLinkAsync(file.Id, updateLinkParams, TestContext.Current.CancellationToken)).Response;
    //     
    //     // Assert
    //     updatedLink.Should().NotBeNull();
    //     updatedLink.Id.Should().Be(initialLink.Id); // Same link ID
    //     updatedLink.Access.Should().Be(2); // Updated access level
    // }
}
