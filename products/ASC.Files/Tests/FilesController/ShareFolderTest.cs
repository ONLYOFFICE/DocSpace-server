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

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class ShareFolderTest(
    FilesApiFactory filesFactory,
    WepApiFactory apiFactory,
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram)
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Fact]
    public async Task CreatePrimaryExternalLink_FolderInMy_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file", folder.Id);
        
        // Act
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(result);

        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, sharedTo.RequestToken);
        var folderInfo = (await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var fileInfo = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        result.Should().NotBeNull();
        result.Access.Should().Be(FileShare.Read);
        result.CanEditAccess.Should().BeFalse();
        result.IsOwner.Should().BeFalse();

        folderInfo.Should().NotBeNull();
        folderInfo.Current.Should().NotBeNull();
        folderInfo.Current.Title.Should().Be(folder.Title);
        
        fileInfo.Should().NotBeNull();
        fileInfo.Title.Should().Be(file.Title);
    }

    [Fact]
    public async Task UpdatePrimaryExternalLink_ValidData_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        
        // Act
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(result);

        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1) }, folder.Title + " updated", "11111111", true);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);

        // Assert
        updatedExternalLink.Should().NotBeNull();
        updatedExternalLink.Access.Should().Be(data.Access);

        updatedSharedTo.Id.Should().Be(data.LinkId);
        updatedSharedTo.ExpirationDate.UtcTime.Should().Be(data.ExpirationDate.UtcTime);
        updatedSharedTo.Title.Should().Be(data.Title);
        updatedSharedTo.Password.Should().Be(data.Password);
        updatedSharedTo.DenyDownload.Should().Be(data.DenyDownload);
    }
    
    [Fact]
    public async Task ExternalLinkDenyDownload_InternalUser_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file_to_share.docx", folder.Id);
        
        // Act
        var externalLink =  (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, denyDownload: true);
        await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken);
        
        file = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        file.Should().NotBeNull();
        file.FolderId.Should().Be(folder.Id);
        file.Security.Download.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExternalLinkDenyDownload_ExternalUser_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file_to_share.docx", folder.Id);
        
        // Act
        var externalLink =  (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, denyDownload: true);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        file = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        file.Should().NotBeNull();
        file.Security.Download.Should().BeFalse();
    }
    
    [Fact]
    public async Task CreatePrimaryExternalLink_FolderWithFileInMyByOwner_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file", folder.Id);
        
        // Act
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(result);

        var data = new FolderLinkRequest(sharedTo.Id, password: "11111111");
        await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, sharedTo.RequestToken);
        await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_ExternalUserRequirePassword_ReturnsRequiredPassword()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        
        // Act
        var externalLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, password: "11111111");
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareData = (await _filesSharingApi.GetExternalShareDataAsync(updatedSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        externalShareData.Status.Should().Be(Status.RequiredPassword);
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_CheckPassword_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        
        // Act
        var externalLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = DeserializeSharedToLink(externalLink);

        var password = "11111111";
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, password: password);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareDataWrongPassword = (await _filesSharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password + "1" }, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var externalShareData = (await _filesSharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password }, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        externalShareDataWrongPassword.Status.Should().Be(Status.InvalidPassword);
        externalShareData.Status.Should().Be(Status.Ok);
    }
}