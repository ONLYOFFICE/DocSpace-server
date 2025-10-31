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

using ASC.Files.Tests.ApiFactories;

namespace ASC.Files.Tests.Tests._04_Security;

[Collection("Test Collection")]
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class FolderShareTests(
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
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        
        // Act
        var linkParams = new FolderLinkRequest(access: fileShare);
        await _foldersApi.CreateFolderPrimaryExternalLinkAsync(folder.Id, linkParams, TestContext.Current.CancellationToken);
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        result.Should().NotBeNull();
        //result.ShareLink.Should().NotBeNullOrEmpty();
        result.Access.Should().Be(fileShare);
    }
    
    // [Fact]
    // public async Task PrimaryExternalLink_ShortLink_ReturnsOk()
    // {
    //     // Arrange
    //     await _filesClient.Authenticate(Initializer.Owner);
    //     var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;
    //     var folder = await CreateFolder("folder",  room.Id);
    //     var linkParams = new FolderLinkRequest(access: FileShare.Read);
    //     
    //     // Act
    //     var externalLink =  (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(folder.Id, linkParams, TestContext.Current.CancellationToken)).Response;
    //     var sharedTo = DeserializeSharedToLink(externalLink);
    //
    //     var result = await apiFactory.HttpClient.GetAsync(sharedTo.ShareLink, TestContext.Current.CancellationToken);
    //     result.StatusCode.Should().Be(HttpStatusCode.OK);
    // }
    
    [Fact]
    public async Task CreatePrimaryExternalLink_ByDefault_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file", folder.Id);
        
        // Act
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = result.SharedLink;

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
        sharedTo.DenyDownload.Should().BeFalse();
        sharedTo.ExpirationDate.Should().BeNull();
        sharedTo.Internal.Should().BeFalse();

        folderInfo.Should().NotBeNull();
        folderInfo.Current.Should().NotBeNull();
        folderInfo.Current.Title.Should().Be(folder.Title);
        folderInfo.Current.Shared.Should().BeTrue();
        
        fileInfo.Should().NotBeNull();
        fileInfo.Title.Should().Be(file.Title);
        fileInfo.ParentShared.Should().BeTrue();
    }
    
    [Theory]
    [MemberData(nameof(InvalidFileShare))]
    public async Task CreatePrimaryExternalLink_InvalidFileShare_ReturnsError(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        //Assert
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var linkParams = new FolderLinkRequest(access: fileShare);
        
        // Act

        await Assert.ThrowsAsync<ApiException>(async () => 
            await _foldersApi.CreateFolderPrimaryExternalLinkAsync(folder.Id, linkParams, TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task UpdatePrimaryExternalLink_ValidData_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        
        // Act
        var result = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = result.SharedLink;

        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1) }, folder.Title + " updated", "11111111", true, true);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;

        // Assert
        updatedExternalLink.Should().NotBeNull();
        updatedExternalLink.Access.Should().Be(data.Access);
        updatedSharedTo.Id.Should().Be(data.LinkId);
        updatedSharedTo.ExpirationDate.Should().NotBeNull();
        updatedSharedTo.ExpirationDate.UtcTime.Should().Be(data.ExpirationDate.UtcTime);
        updatedSharedTo.Internal.Should().Be(data.Internal);
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
        var sharedTo = externalLink.SharedLink;
        
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
        var sharedTo = externalLink.SharedLink;

        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, denyDownload: true);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
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
        var sharedTo = result.SharedLink;

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
        var sharedTo = externalLink.SharedLink;

        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, password: "11111111");
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareData = (await _sharingApi.GetExternalShareDataAsync(updatedSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
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
        var sharedTo = externalLink.SharedLink;

        var password = "11111111";
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.Editing, password: password);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareDataWrongPassword = (await _sharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password + "1" }, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var externalShareData = (await _sharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password }, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        externalShareDataWrongPassword.Status.Should().Be(Status.InvalidPassword);
        externalShareData.Status.Should().Be(Status.Ok);
    }
    
    [Fact]
    public async Task SetFolderLinkAsync_NoneAccess_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        
        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var file = await CreateFile("file", folder.Id);
        
        var linkParams = new FolderLinkRequest(access: FileShare.Read);
        var externalLink = (await _foldersApi.CreateFolderPrimaryExternalLinkAsync(folder.Id, linkParams, TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;
        
        // Act
        var data = new FolderLinkRequest(sharedTo.Id, FileShare.None);
        var updatedExternalLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, data, TestContext.Current.CancellationToken)).Response;
        var folderInfo = (await _foldersApi.GetFolderInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var fileInfo = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        // Assert
        updatedExternalLink.Should().BeNull();
        folderInfo.Shared.Should().BeFalse();
        fileInfo.Shared.Should().BeFalse();
        
        var allLinks = (await _foldersApi.GetFolderLinksAsync(folderInfo.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        allLinks.Should().BeEmpty(); 
    }
    
    [Fact]
    public async Task CheckEditAccess_VDR_ReturnsFalse()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateVDRRoom("room title");
        var folder = await CreateFolder("folder title", room.Id);
        
        // Get the primary external link
        var primaryLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedToLink = primaryLink.SharedLink;

        sharedToLink.Internal.Should().BeTrue();
        primaryLink.CanEditAccess.Should().BeFalse();

        var updatedLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, new FolderLinkRequest(sharedToLink.Id, @internal: false), TestContext.Current.CancellationToken)).Response;
        var updatedSharedToLink = updatedLink.SharedLink;
        updatedSharedToLink.Internal.Should().BeTrue();
    }
    
    [Fact]
    public async Task CheckEditAccess_PublicRoom_ReturnsFalse()
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreatePublicRoom("room title");
        var folder = await CreateFolder("folder title", room.Id);
        
        // Get the primary external link
        var primaryLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedToLink = primaryLink.SharedLink;

        sharedToLink.Internal.Should().BeFalse();
        primaryLink.CanEditAccess.Should().BeFalse();

        var updatedLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, new FolderLinkRequest(sharedToLink.Id, @internal: true), TestContext.Current.CancellationToken)).Response;
        var updatedSharedToLink = updatedLink.SharedLink;
        updatedSharedToLink.Internal.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    [InlineData(RoomType.EditingRoom)]
    public async Task CheckEditAccess_ValidRoomType_ReturnsTrue(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        var folder = await CreateFolder("folder title", room.Id);
        
        // Get the primary external link
        var primaryLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedToLink = primaryLink.SharedLink;
        
        sharedToLink.Internal.Should().BeFalse();
        primaryLink.CanEditInternal.Should().BeTrue();
        
        var updatedLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, new FolderLinkRequest(sharedToLink.Id, @internal: true), TestContext.Current.CancellationToken)).Response;
        updatedLink.Should().BeNull();
    }
    
    [Theory]
    [InlineData(RoomType.FillingFormsRoom)]
    public async Task CheckEditAccess_InValidRoomType_ReturnsTrue(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        var folder = await CreateFolder("folder title", room.Id);
        
        // Get the primary external link
        await Assert.ThrowsAsync<ApiException>(async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task GetFolderSecurityInfo_SharedFolder_ReturnsSecurityInformation()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var user1 = await Initializer.InviteContact(EmployeeType.User);
        var user2 = await Initializer.InviteContact(EmployeeType.User);

        // Share the folder with different access levels
        var shareInfo = new List<FileShareParams>
        {
            new() { ShareTo = user1.Id, Access = FileShare.Read },
            new() { ShareTo = user2.Id, Access = FileShare.Editing }
        };

        var securityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = shareInfo
        };

        // Set folder security info
        var result = (await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, securityRequest, TestContext.Current.CancellationToken)).Response;
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r => r.SubjectType.Should().BeOneOf(SubjectType.Group, SubjectType.User));
        
        // Act
        var securityInfos = (await _sharingApi.GetFolderSecurityInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        securityInfos.Should().NotBeEmpty();
        securityInfos.Should().HaveCountGreaterThanOrEqualTo(2); // At least 2 users + owner

        // Verify user1 has read access
        var user1Security = securityInfos.FirstOrDefault(s => s.SharedToUser.Id == user1.Id);
        user1Security.Should().NotBeNull();
        user1Security!.Access.Should().Be(FileShare.Read);

        // Verify user2 has editing access
        var user2Security = securityInfos.FirstOrDefault(s => s.SharedToUser.Id == user2.Id);
        user2Security.Should().NotBeNull();
        user2Security!.Access.Should().Be(FileShare.Editing);
        
        // Verify that the file is accessible by group members with correct permissions
        await _filesClient.Authenticate(user1);
        var folderAsUser1 = (await _foldersApi.GetFolderInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderAsUser1.Should().NotBeNull();
        folderAsUser1.Access.Should().Be(FileShare.Read);
        folderAsUser1.Security.Read.Should().BeTrue();
        folderAsUser1.Security.Edit.Should().BeFalse();
        
        var sharedFolderIdAsUser1 = await GetFolderIdAsync(FolderType.SHARE, user1);
        var sharedFolderAsUser1 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser1, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser1.Should().NotBeNull();
        sharedFolderAsUser1.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Read);
        
        await _filesClient.Authenticate(user2);
        var folderAsUser2 = (await _foldersApi.GetFolderInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderAsUser2.Should().NotBeNull();
        folderAsUser2.Access.Should().Be(FileShare.Editing);
        folderAsUser2.Security.Read.Should().BeTrue();
        
        var sharedFolderIdAsUser2 = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolderAsUser2 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser2, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser2.Should().NotBeNull();
        sharedFolderAsUser2.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Editing);
    }
    
    [Fact]
    public async Task GetFolderSecurityInfo_SharedFolderGroup_ReturnsSecurityInformation()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var folder = await CreateFolderInMy("folder", Initializer.Owner);
        var user1 = await Initializer.InviteContact(EmployeeType.User);
        var user2 = await Initializer.InviteContact(EmployeeType.User);

        // Create a group
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([user1.Id, user2.Id], Initializer.Owner.Id, "TestFolderGroup"), TestContext.Current.CancellationToken)).Response;
        
        // Share the folder with the group
        var shareInfo = new List<FileShareParams>
        {
            new() { ShareTo = group.Id, Access = FileShare.Editing }
        };

        var securityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = shareInfo
        };

        // Set folder security info
        var result = (await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, securityRequest, TestContext.Current.CancellationToken)).Response;
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r => r.SubjectType.Should().BeOneOf(SubjectType.Group, SubjectType.User));
        
        // Act
        var securityInfos = (await _sharingApi.GetFolderSecurityInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        securityInfos.Should().NotBeEmpty();
        securityInfos.Should().HaveCountGreaterThanOrEqualTo(2); // At least 2 users + owner

        // Verify group has read access
        var user1Security = securityInfos.FirstOrDefault(s => s.SharedToGroup?.Id == group.Id);
        user1Security.Should().NotBeNull();
        user1Security!.Access.Should().Be(FileShare.Editing);
        
        // Verify that the file is accessible by group members with correct permissions
        await _filesClient.Authenticate(user1);
        var folderAsUser1 = (await _foldersApi.GetFolderInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderAsUser1.Should().NotBeNull();
        folderAsUser1.Access.Should().Be(FileShare.Editing);
        folderAsUser1.Security.Read.Should().BeTrue();
        folderAsUser1.Security.Edit.Should().BeFalse();
        
        var sharedFolderIdAsUser1 = await GetFolderIdAsync(FolderType.SHARE, user1);
        var sharedFolderAsUser1 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser1, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser1.Should().NotBeNull();
        sharedFolderAsUser1.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Editing);
        
        await _filesClient.Authenticate(user2);
        var folderAsUser2 = (await _foldersApi.GetFolderInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        folderAsUser2.Should().NotBeNull();
        folderAsUser2.Access.Should().Be(FileShare.Editing);
        folderAsUser2.Security.Read.Should().BeTrue();
        
        var sharedFolderIdAsUser2 = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolderAsUser2 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser2, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser2.Should().NotBeNull();
        sharedFolderAsUser2.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Editing);
    }
    
    [Fact]
    public async Task GetFolderSecurityInfo_SharedFolderWithGroup_ReturnsGroupSecurityInformation()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        await _peopleClient.Authenticate(Initializer.Owner);
        
        var folder = await CreateFolderInMy("folder_security_info_group", Initializer.Owner);
        var fileInFolder = await CreateFile("file_in_folder", folder.Id);
        
        // Add users to the group
        var user1 = await Initializer.InviteContact(EmployeeType.User);
        var user2 = await Initializer.InviteContact(EmployeeType.User);
        
        // Create a group
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([user1.Id, user2.Id], Initializer.Owner.Id, "TestFolderGroup"), TestContext.Current.CancellationToken)).Response;
        
        // Share the folder with the group
        var shareInfo = new List<FileShareParams>
        {
            new() { ShareTo = group.Id, Access = FileShare.Editing }
        };

        var securityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = shareInfo
        };

        // Set folder security info for the group
        var result = (await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, securityRequest, TestContext.Current.CancellationToken)).Response;
        result.Should().NotBeNull();
        result.Should().AllSatisfy(r => r.SubjectType.Should().BeOneOf(SubjectType.Group, SubjectType.User));

        // Act
        var securityInfos = (await _sharingApi.GetFolderSecurityInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        securityInfos.Should().NotBeEmpty();

        // Verify the group has editing access
        var groupSecurity = securityInfos.FirstOrDefault(s => s.SharedToGroup?.Id == group.Id);
        groupSecurity.Should().NotBeNull();
        groupSecurity!.Access.Should().Be(FileShare.Editing);
        groupSecurity.SharedToGroup.Should().NotBeNull();
        groupSecurity.SharedToGroup.Name.Should().Be("TestFolderGroup");

        // Verify that the file in the folder is accessible by group members with correct permissions
        await _filesClient.Authenticate(user1);
        var fileAsUser1 = (await _filesApi.GetFileInfoAsync(fileInFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        fileAsUser1.Should().NotBeNull();
        fileAsUser1.Access.Should().Be(FileShare.Editing);
        fileAsUser1.Security.Edit.Should().BeTrue();

        var sharedFolderIdAsUser1 = await GetFolderIdAsync(FolderType.SHARE, user1);
        var sharedFolderAsUser1 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser1, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser1.Should().NotBeNull();
        sharedFolderAsUser1.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Editing);
        
        await _filesClient.Authenticate(user2);
        var fileAsUser2 = (await _filesApi.GetFileInfoAsync(fileInFolder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        fileAsUser2.Should().NotBeNull();
        fileAsUser2.Access.Should().Be(FileShare.Editing);
        fileAsUser2.Security.Edit.Should().BeTrue();
        
        var sharedFolderIdAsUser2 = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolderAsUser2 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderIdAsUser2, cancellationToken: TestContext.Current.CancellationToken)).Response;
        sharedFolderAsUser2.Should().NotBeNull();
        sharedFolderAsUser2.Folders.Should().Contain(r => r.Title == folder.Title && r.Access == FileShare.Editing);
    }

    [Fact]
    public async Task SharedFolder_NewItemsCount_IncreasesAfterSharingFoldersAndAddingFiles()
    {
        // Step 1: Authenticate as user1
        await _filesClient.Authenticate(Initializer.Owner);
        var user1 = Initializer.Owner;
        var user2 = await Initializer.InviteContact(EmployeeType.User);

        // Step 2: Create folder1 and folder2 in "My Documents"
        var folder1 = await CreateFolderInMy("folder_new_item_1", user1);
        var folder2 = await CreateFolderInMy("folder_new_item_2", user1);

        // Step 3: Create file1 in folder2
        var file1 = await CreateFile("file_new_item_1.docx", folder2.Id);

        // Step 4: Set File Security with read rights for folder1 to user2
        var shareInfo1 = new List<FileShareParams>
        {
            new() { ShareTo = user2.Id, Access = FileShare.Read }
        };
        var securityRequest1 = new SecurityInfoSimpleRequestDto
        {
            Share = shareInfo1
        };
        var result1 = (await _sharingApi.SetFolderSecurityInfoAsync(folder1.Id, securityRequest1, TestContext.Current.CancellationToken)).Response;
        result1.Should().NotBeNull();

        // Step 5: Authenticate as user2
        await _filesClient.Authenticate(user2);

        // Step 6: Get Folder with type FolderType.SHARE
        var sharedFolderId = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolder = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Step 7: Check New for equality 1 for empty folder
        sharedFolder.Should().NotBeNull();
        sharedFolder.New.Should().Be(1);

        // Step 8: Authenticate as user1
        await _filesClient.Authenticate(user1);

        // Step 9: Set File Security with read rights for folder2 to user2
        var shareInfo2 = new List<FileShareParams>
        {
            new() { ShareTo = user2.Id, Access = FileShare.Read }
        };
        var securityRequest2 = new SecurityInfoSimpleRequestDto
        {
            Share = shareInfo2
        };
        var result2 = (await _sharingApi.SetFolderSecurityInfoAsync(folder2.Id, securityRequest2, TestContext.Current.CancellationToken)).Response;
        result2.Should().NotBeNull();

        // Step 10: Authenticate as user2
        await _filesClient.Authenticate(user2);

        // Step 11: Get Folder with type FolderType.SHARE
        var sharedFolderId2 = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolder2 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderId2, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Step 12: Check New for equality 2
        sharedFolder2.Should().NotBeNull();
        sharedFolder2.New.Should().Be(2);

        // Step 13: Authenticate as user1
        await _filesClient.Authenticate(user1);

        // Step 14: Create file2 in folder2
        var file2 = await CreateFile("file_new_item_2.docx", folder2.Id);

        // Step 15: Authenticate as user2
        await _filesClient.Authenticate(user2);

        // Step 16: Get Folder with type FolderType.SHARE
        var sharedFolderId3 = await GetFolderIdAsync(FolderType.SHARE, user2);
        var sharedFolder3 = (await _foldersApi.GetFolderByFolderIdAsync(sharedFolderId3, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Step 17: Check New for equality 3
        sharedFolder3.Should().NotBeNull();
        sharedFolder3.New.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "77476")]
    public async Task SharedFolderWithFile_RemoveUsersFromFileShare_ReturnsOk()
    {
        // Step 1: Authenticate as owner
        await _filesClient.Authenticate(Initializer.Owner);
        var owner = Initializer.Owner;

        // Step 2: Share folder in "My Documents" with full access
        var folder = await CreateFolderInMy("shared_folder", owner);
        var user2 = await Initializer.InviteContact(EmployeeType.RoomAdmin);

        var folderShareInfo = new List<FileShareParams>
        {
            new() { ShareTo = user2.Id, Access = FileShare.ReadWrite }
        };
        var folderSecurityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = folderShareInfo
        };
        var folderShareResult = (await _sharingApi.SetFolderSecurityInfoAsync(folder.Id, folderSecurityRequest, TestContext.Current.CancellationToken)).Response;
        folderShareResult.Should().NotBeNull();

        // Step 3: Create document inside folder and add users with different access levels
        var file = await CreateFile("document.docx", folder.Id);
        var user3 = await Initializer.InviteContact(EmployeeType.User);
        var user4 = await Initializer.InviteContact(EmployeeType.User);

        var fileShareInfo = new List<FileShareParams>
        {
            new() { ShareTo = user3.Id, Access = FileShare.Read },
            new() { ShareTo = user4.Id, Access = FileShare.Editing }
        };
        var fileSecurityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = fileShareInfo
        };
        var fileShareResult = (await _sharingApi.SetFileSecurityInfoAsync(file.Id, fileSecurityRequest, TestContext.Current.CancellationToken)).Response;
        fileShareResult.Should().NotBeNull();

        // Step 4: Authenticate as user2 (who has full access to folder and file)
        await _filesClient.Authenticate(user2);

        // Step 5: Get file security info to verify initial sharing
        var initialSecurityInfos = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        initialSecurityInfos.Should().NotBeEmpty();
        initialSecurityInfos.Should().Contain(s => s.SharedToUser.Id == user3.Id && s.Access == FileShare.Read);
        initialSecurityInfos.Should().Contain(s => s.SharedToUser.Id == user4.Id && s.Access == FileShare.Editing);

        // Step 6: Remove users from file share list by setting access to None
        var removeShareInfo = new List<FileShareParams>
        {
            new() { ShareTo = user3.Id, Access = FileShare.None },
            new() { ShareTo = user4.Id, Access = FileShare.None }
        };
        var removeSecurityRequest = new SecurityInfoSimpleRequestDto
        {
            Share = removeShareInfo
        };
        await Assert.ThrowsAsync<ApiException>(async() => 
            await _sharingApi.SetFileSecurityInfoAsync(file.Id, removeSecurityRequest, TestContext.Current.CancellationToken));
    }
}