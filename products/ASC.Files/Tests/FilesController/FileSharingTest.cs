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

using System.Text.Json;

using ASC.Files.Core.ApiModels.ResponseDto;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class FileSharingTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    //   FileShare.None
    public static TheoryData<FileShare> Data =>
    [
        FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.Read
    ];
    
    public static TheoryData<FileShare> InvalidData =>
    [
        FileShare.None, FileShare.ReadWrite, FileShare.Varies, FileShare.RoomManager, FileShare.ContentCreator
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreatePrimaryExternalLink_ValidFileShare_ReturnsLinkData(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_to_share.docx", FolderType.USER, Initializer.Owner);
        
        // Act
        var linkParams = new FileLinkRequest(access: fileShare);
        
        await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken);
        
        // Act
        var result = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        //result.ShareLink.Should().NotBeNullOrEmpty();
        result.Access.Should().Be(fileShare); // Read access
    }

    [Theory]
    [MemberData(nameof(InvalidData))]
    public async Task CreatePrimaryExternalLink_InvalidFileShare_ReturnsError(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_to_share.docx", FolderType.USER, Initializer.Owner);
        
        // Act
        var linkParams = new FileLinkRequest(access: fileShare);

        await Assert.ThrowsAsync<ApiException>(async () => 
            await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, linkParams, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetLinks_WithMultipleLinks_ReturnsAllLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_with_multiple_links.docx", FolderType.USER, Initializer.Owner);
        
        // Create a primary external link
        var primaryLinkParams = new FileLinkRequest(access: FileShare.Read);
        
        await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, primaryLinkParams, TestContext.Current.CancellationToken);
        
        // Create an additional external link
        var additionalLinkParams = new FileLinkRequest(access: FileShare.Editing);
        
        await _filesApi.SetExternalLinkAsync(file.Id, additionalLinkParams, TestContext.Current.CancellationToken);
        
        // Act
        var links = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        links.Should().NotBeNull();
        links.Should().HaveCountGreaterThanOrEqualTo(2);
        links.Should().Contain(link => link.Access == FileShare.Read); // Primary link with read access
        links.Should().Contain(link => link.Access == FileShare.Editing); // Additional link with editing access
    }
    
    [Fact]
    public async Task SetExternalLink_UpdateExistingLink_ReturnsUpdatedLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_update_link.docx", FolderType.USER, Initializer.Owner);
        
        // Create initial external link
        var initialLinkParams = new FileLinkRequest(
            access: FileShare.Read, // Read access
            primary: true
        );
        
        var initialLink = (await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, initialLinkParams, TestContext.Current.CancellationToken)).Response;
        var sharedToJObject = initialLink.SharedTo as JObject;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>(sharedToJObject.ToString(), JsonSerializerOptions.Web);
        
        // Act - Update the link
        var updateLinkParams = new FileLinkRequest(
            access: FileShare.Editing, // Read/Write access
            linkId: sharedTo.Id
        );

        var updatedLink = (await _filesApi.SetExternalLinkAsync(file.Id, updateLinkParams, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        updatedLink.Should().NotBeNull();
        updatedLink.Access.Should().Be(FileShare.Editing); // Updated access level
    }
    
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task PrimaryExternalLink_ExternalUsers_ReturnsFileData(FileShare fileShare)
    {
        // Arrange and Act
        var (share, fileId) = await CreateFileAndShare(fileShare);
        var openEditResult = await TryOpenEditAsync(share, fileId);
        
        // Assert
        openEditResult.Should().NotBeNull();
        openEditResult.Access.Should().Be(fileShare);
        openEditResult.Shared.Should().Be(true);
    }

    [Theory]
    [MemberData(nameof(Data))]
    public async Task PrimaryExternalLink_InternalUsers_ReturnsFileData(FileShare fileShare)
    {
        // Arrange and Act
        var (share, fileId) = await CreateFileAndShare(fileShare);
        
        // Assert
        var user = await Initializer.InviteContact(EmployeeType.User);
        var openEditResult = await TryOpenEditAsync(share, fileId, user);
        
        // Assert
        openEditResult.Should().NotBeNull();
        openEditResult.Access.Should().Be(fileShare);
        openEditResult.Shared.Should().Be(true);
    }
    
    [Theory]
    [MemberData(nameof(Data))]
    public async Task PrimaryInternalLink_ExternalUsers_ReturnsError(FileShare fileShare)
    {        
        // Arrange and Act
        var (share, fileId) = await CreateFileAndShare(fileShare, varInternal: true);
        
        // Assert
        await TryOpenEditAsync(share, fileId, throwException: true);
    }
    
    [Fact]
    public async Task PrimaryExternalLink_WithDateNotExpired_ReturnsFileData()
    {
        const FileShare fileShare = FileShare.Read;
        // Arrange and Act
        var (share, fileId) = await CreateFileAndShare(fileShare, expirationDate: DateTime.UtcNow.AddDays(1));
        
        // Assert
        var openEditResult = await TryOpenEditAsync(share, fileId);

        // Assert
        openEditResult.Should().NotBeNull();
        openEditResult.Access.Should().Be(fileShare);
        openEditResult.Shared.Should().Be(true);
    }
    
    [Fact]
    public async Task PrimaryExternalLink_WithDateExpired_ReturnsFileData()
    {
        // Arrange and Act
        const int seconds = 1;
        var (share, fileId) = await CreateFileAndShare(FileShare.Read, expirationDate: DateTime.UtcNow.AddSeconds(seconds));
        await Task.Delay(TimeSpan.FromSeconds(seconds), TestContext.Current.CancellationToken);
        
        // Assert
        await TryOpenEditAsync(share, fileId, throwException: true);
    }
    
    private async Task<(string, int)> CreateFileAndShare(FileShare fileShare, bool primary = true, bool varInternal = false, DateTime? expirationDate = null)
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_update_link.docx", FolderType.USER, Initializer.Owner);
        
        // Create initial external link
        var initialLinkParams = new FileLinkRequest(
            access: fileShare,
            primary: primary,
            @internal: varInternal
        );

        if (expirationDate != null)
        {
            initialLinkParams.ExpirationDate = new ApiDateTime { UtcTime = expirationDate.Value };
        }
        
        var initialLink = (await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, initialLinkParams, TestContext.Current.CancellationToken)).Response;
        var sharedTo = initialLink.SharedTo as JObject;
        var fileShareLink = JsonSerializer.Deserialize<FileShareLink>(sharedTo.ToString(), JsonSerializerOptions.Web);
        
        return (fileShareLink.RequestToken, file.Id);
    }
    
    private async Task<FileDtoInteger?> TryOpenEditAsync(string share, int fileId, User? user = null, bool throwException = false)
    {
        if (user != null)
        {
            await _filesClient.Authenticate(user);
        }
        else
        {
            _filesClient.DefaultRequestHeaders.Authorization = null;
        }

        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, share);
        
        if (throwException)
        {
            await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
            return null;
        }

        var openEditResult = (await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        return openEditResult.File;
    }
}
