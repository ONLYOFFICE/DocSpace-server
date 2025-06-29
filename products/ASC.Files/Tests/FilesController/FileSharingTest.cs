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
    [Theory]
    [MemberData(nameof(ValidFileShare))]
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
    [MemberData(nameof(InvalidFileShare))]
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
    public async Task CreateMultipleLinks_ForFile_ReturnsAllLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var file = await CreateFile("file_with_multiple_links.docx", FolderType.USER, Initializer.Owner);

        // Create primary link
        var primaryLinkParams = new FileLinkRequest(
            access: FileShare.Read,
            //title: "Primary Link",
            primary: true);
        await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, primaryLinkParams, TestContext.Current.CancellationToken);

        // Create additional links with different permissions
        var additionalLink1 = new FileLinkRequest(
            access: FileShare.Comment
            //title: "Comment Link"
            );

        var additionalLink2 = new FileLinkRequest(
            access: FileShare.Editing
            //title: "Editing Link"
            );

        var additionalLink3 = new FileLinkRequest(
            access: FileShare.Review
            //title: "Review Link"
            );

        await _filesApi.SetExternalLinkAsync(file.Id, additionalLink1, TestContext.Current.CancellationToken);
        await _filesApi.SetExternalLinkAsync(file.Id, additionalLink2, TestContext.Current.CancellationToken);
        await _filesApi.SetExternalLinkAsync(file.Id, additionalLink3, TestContext.Current.CancellationToken);

        // Act - Get all links
        var links = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        links.Should().NotBeNull();
        links.Should().HaveCountGreaterThanOrEqualTo(4); // Primary + 3 additional links

        // Verify each access type is present
        links.Should().Contain(link => link.Access == FileShare.Read);
        links.Should().Contain(link => link.Access == FileShare.Comment);
        links.Should().Contain(link => link.Access == FileShare.Editing);
        links.Should().Contain(link => link.Access == FileShare.Review);
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
        var sharedTo = DeserializeSharedToLink(initialLink);
        
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

    [Fact]
    public async Task UpdateMultipleLinks_ForFile_ReturnsUpdatedLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var file = await CreateFile("file_with_links_to_update.docx", FolderType.USER, Initializer.Owner);

        // Create multiple links
        var link1Request = new FileLinkRequest(
            access: FileShare.Read
            //title: "Link to Update 1"
            );

        var link2Request = new FileLinkRequest(
            access: FileShare.Read
            //title: "Link to Update 2"
            );

        var link1Response = (await _filesApi.SetExternalLinkAsync(file.Id, link1Request, TestContext.Current.CancellationToken)).Response;
        var link2Response = (await _filesApi.SetExternalLinkAsync(file.Id, link2Request, TestContext.Current.CancellationToken)).Response;

        var link1SharedTo = DeserializeSharedToLink(link1Response);
        var link2SharedTo = DeserializeSharedToLink(link2Response);

        // Act - Update both links with different properties
        var updateLink1Request = new FileLinkRequest(
            linkId: link1SharedTo.Id,
            access: FileShare.Editing,
            //title: "Updated Link 1",
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(5) });

        var updateLink2Request = new FileLinkRequest(
            linkId: link2SharedTo.Id,
            access: FileShare.Comment,
            //: "Updated Link 2",
            password: "testpassword");

        var updatedLink1Response = (await _filesApi.SetExternalLinkAsync(file.Id, updateLink1Request, TestContext.Current.CancellationToken)).Response;
        var updatedLink2Response = (await _filesApi.SetExternalLinkAsync(file.Id, updateLink2Request, TestContext.Current.CancellationToken)).Response;

        // Get all links after updates
        var allLinks = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var updatedLink1SharedTo = DeserializeSharedToLink(updatedLink1Response);
        var updatedLink2SharedTo = DeserializeSharedToLink(updatedLink2Response);

        // Verify first link updates
        updatedLink1SharedTo.Id.Should().Be(link1SharedTo.Id);
        //updatedLink1SharedTo.Title.Should().Be("Updated Link 1");
        updatedLink1Response.Access.Should().Be(FileShare.Editing);
        updatedLink1SharedTo.ExpirationDate.Should().NotBeNull();

        // Verify second link updates
        updatedLink2SharedTo.Id.Should().Be(link2SharedTo.Id);
        //updatedLink2SharedTo.Title.Should().Be("Updated Link 2");
        updatedLink2Response.Access.Should().Be(FileShare.Comment);
        updatedLink2SharedTo.Password.Should().Be("testpassword");

        // Verify links in the complete list
        allLinks.Should().Contain(link => 
            //DeserializeSharedToLink(link).Title == "Updated Link 1" && 
            link.Access == FileShare.Editing);

        allLinks.Should().Contain(link => 
            //DeserializeSharedToLink(link).Title == "Updated Link 2" && 
            link.Access == FileShare.Comment);
    }
    
    
    [Theory]
    [MemberData(nameof(ValidFileShare))]
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
    [MemberData(nameof(ValidFileShare))]
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
    [MemberData(nameof(ValidFileShare))]
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

    [Fact]
    public async Task AccessFileWithMultipleLinks_DifferentPermissions_ReturnsCorrectAccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var file = await CreateFile("file_with_multiple_access_links.docx", FolderType.USER, Initializer.Owner);

        // Create multiple links with different permissions
        var readOnlyLink = new FileLinkRequest(
            access: FileShare.Read
            //title: "Read Only Link"
            );

        var editingLink = new FileLinkRequest(
            access: FileShare.Editing
            //title: "Editing Link"
            );

        var commentLink = new FileLinkRequest(
            access: FileShare.Comment
            //title: "Comment Link"
            );

        var readOnlyResponse = (await _filesApi.SetExternalLinkAsync(file.Id, readOnlyLink, TestContext.Current.CancellationToken)).Response;
        var editingResponse = (await _filesApi.SetExternalLinkAsync(file.Id, editingLink, TestContext.Current.CancellationToken)).Response;
        var commentResponse = (await _filesApi.SetExternalLinkAsync(file.Id, commentLink, TestContext.Current.CancellationToken)).Response;

        var readOnlySharedTo = DeserializeSharedToLink(readOnlyResponse);
        var editingSharedTo = DeserializeSharedToLink(editingResponse);
        var commentSharedTo = DeserializeSharedToLink(commentResponse);

        // Act - Access with read-only link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, readOnlySharedTo.RequestToken);
        var readOnlyAccess = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.File;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access with editing link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, editingSharedTo.RequestToken);
        var editingAccess = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.File;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access with comment link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, commentSharedTo.RequestToken);
        var commentAccess = (await _filesApi.OpenEditFileAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.File;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        // Read-only permissions
        readOnlyAccess.Should().NotBeNull();
        readOnlyAccess.Security.Edit.Should().BeFalse();
        readOnlyAccess.Security.Comment.Should().BeFalse();
        readOnlyAccess.Access.Should().Be(FileShare.Read);

        // Editing permissions
        editingAccess.Should().NotBeNull();
        editingAccess.Security.Edit.Should().BeTrue();
        editingAccess.Security.Comment.Should().BeTrue();
        editingAccess.Access.Should().Be(FileShare.Editing);

        // Comment permissions
        commentAccess.Should().NotBeNull();
        commentAccess.Security.Edit.Should().BeFalse();
        commentAccess.Security.Comment.Should().BeTrue();
        commentAccess.Access.Should().Be(FileShare.Comment);
    }
    
    private async Task<(string, int)> CreateFileAndShare(FileShare fileShare, bool primary = true, bool varInternal = false, DateTime? expirationDate = null)
    {
        await _filesClient.Authenticate(Initializer.Owner);
        
        var file = await CreateFile("file_update_link.docx", FolderType.USER, Initializer.Owner);
        
        // Create initial external link
        var initialLinkParams = new FileLinkRequest(
            access: fileShare,
            primary: primary,
            varInternal: varInternal
        );

        if (expirationDate != null)
        {
            initialLinkParams.ExpirationDate = new ApiDateTime { UtcTime = expirationDate.Value };
        }
        
        var initialLink = (await _filesApi.CreatePrimaryExternalLinkAsync(file.Id, initialLinkParams, TestContext.Current.CancellationToken)).Response;
        var fileShareLink = DeserializeSharedToLink(initialLink);
        
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

    [Fact]
    public async Task FileWithMultipleLinks_PasswordProtectedAndUnrestricted_WorksCorrectly()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var file = await CreateFile("file_with_mixed_links.docx", FolderType.USER, Initializer.Owner);

        // Create one link with password and one without
        var unrestrictedLink = new FileLinkRequest(
            access: FileShare.Read
            //title: "Unrestricted Link"
            );

        var passwordProtectedLink = new FileLinkRequest(
            access: FileShare.Editing,
            //title: "Password Protected Link",
            password: "securepassword123"
            );

        var unrestrictedResponse = (await _filesApi.SetExternalLinkAsync(file.Id, unrestrictedLink, TestContext.Current.CancellationToken)).Response;
        var passwordProtectedResponse = (await _filesApi.SetExternalLinkAsync(file.Id, passwordProtectedLink, TestContext.Current.CancellationToken)).Response;

        var unrestrictedSharedTo = DeserializeSharedToLink(unrestrictedResponse);
        var passwordProtectedSharedTo = DeserializeSharedToLink(passwordProtectedResponse);

        // Act & Assert - First try unrestricted link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, unrestrictedSharedTo.RequestToken);
        var fileWithUnrestrictedAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        fileWithUnrestrictedAccess.Should().NotBeNull();
        fileWithUnrestrictedAccess.Title.Should().Be(file.Title);

        // Then try password-protected link without providing password
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, passwordProtectedSharedTo.RequestToken);
        await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Get external share data to verify password is required
        var externalShareData = (await _filesSharingApi.GetExternalShareDataAsync(passwordProtectedSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
        externalShareData.Status.Should().Be(Status.RequiredPassword);

        // Now provide correct password
        var password = "securepassword123";
        var externalShareDataWithHttpInfo = await _filesSharingApi.ApplyExternalSharePasswordWithHttpInfoAsync(
            passwordProtectedSharedTo.RequestToken, 
            new ExternalShareRequestParam { Password = password }, 
            cancellationToken: TestContext.Current.CancellationToken);

        var setCookie = externalShareDataWithHttpInfo.Headers.ToDictionary()["Set-Cookie"];
        var anonymousSessionKey = setCookie.First();

        _filesClient.DefaultRequestHeaders.Add("Cookie", anonymousSessionKey);
        var fileWithPasswordProtectedAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove("Cookie");
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Verify the file is accessible with correct password
        fileWithPasswordProtectedAccess.Should().NotBeNull();
        //fileWithPasswordProtectedAccess.Title.Should().Be(file.Title);
        fileWithPasswordProtectedAccess.Security.Edit.Should().BeTrue(); // Should have editing permissions
    }
}
