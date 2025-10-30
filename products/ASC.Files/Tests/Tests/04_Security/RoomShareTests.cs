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
[Trait("Feature", "Rooms")]
public class RoomShareTests(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    public static TheoryData<RoomType, FileShare> DataWithFileShare => new MatrixTheoryData<RoomType, FileShare>(
        [RoomType.EditingRoom, RoomType.VirtualDataRoom],
        [FileShare.Read, FileShare.Editing, FileShare.Comment, FileShare.Review]
    );
    
    [Theory]
    [MemberData(nameof(ValidRoomTypesForShare))]
    public async Task CreatePrimaryExternalLink_ValidRoomType_ReturnsLinkData(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        // Act
        var result = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = result.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, sharedTo.RequestToken);
        var roomInfo = (await _foldersApi.GetFolderByFolderIdAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        // Assert
        result.Should().NotBeNull();
        result.Access.Should().Be(FileShare.Read);
        result.CanEditAccess.Should().BeFalse();
        result.IsOwner.Should().BeFalse();
        
        roomInfo.Should().NotBeNull();
        roomInfo.Current.Should().NotBeNull();
    }
    
    [Fact]
    public async Task CreatePrimaryExternalLink_FillingForm_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("filling form room title", roomType: RoomType.FillingFormsRoom), TestContext.Current.CancellationToken)).Response;
        
        // Act
        var result = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = result.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, sharedTo.RequestToken);
        var roomInfo = (await _foldersApi.GetFolderByFolderIdAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        // Assert
        result.Should().NotBeNull();
        result.Access.Should().Be(FileShare.FillForms);
        result.CanEditAccess.Should().BeFalse();
        result.IsOwner.Should().BeFalse();
        
        roomInfo.Should().NotBeNull();
        roomInfo.Current.Should().NotBeNull();
    }
    
    [Theory]
    [MemberData(nameof(InValidRoomTypesForShare))]
    public async Task CreatePrimaryExternalLink_RestrictedRoomType_ReturnsError(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken));
        
        // Verify error
        exception.ErrorCode.Should().Be(403);
    }
    
    [Theory]
    [MemberData(nameof(DataWithFileShare))]
    public async Task CreateExternalLink_RestrictedRoomType_ReturnsError(RoomType roomType, FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        var additionalLink = new RoomLinkRequest(
            access: fileShare,
            title: "Additional Link 1",
            linkType: LinkType.External);
        
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink, TestContext.Current.CancellationToken));
        
        // Verify error
        exception.ErrorCode.Should().Be(403);
    }
    
    [Theory]
    [MemberData(nameof(ValidRoomTypesForShare))]
    public async Task CreateMultipleLinks_InRoom_ReturnsAllLinks(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom =  (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room with multiple links", roomType: roomType), TestContext.Current.CancellationToken)).Response;

        // Act - Get a primary external link
        await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Create additional links
        var additionalLink1 = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Additional Link 1",
            linkType: LinkType.External);

        var additionalLink2 = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Additional Link 2",
            linkType: LinkType.External);

       await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink1, TestContext.Current.CancellationToken);
       await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink2, TestContext.Current.CancellationToken);

        // Get all links
        var allLinks = (await _roomsApi.GetRoomLinksAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        allLinks.Should().NotBeNull();
        allLinks.Should().HaveCountGreaterThanOrEqualTo(3); // Primary + 2 additional links

        // Verify links by title and access level
        allLinks.Should().Contain(link => 
            link.SharedLink.Title == "Additional Link 1" && 
            link.Access == FileShare.Read);

        allLinks.Should().Contain(link => 
            link.SharedLink.Title == "Additional Link 2" && 
            link.Access == FileShare.Editing);
    }
    
    [Fact]
    public async Task CreateMaximumFiveLinks_InRoom_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom =  (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room with multiple links", roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;

        // Act - Get a primary external link
        await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Create additional links
        var additionalLink1 = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Additional Link 1",
            linkType: LinkType.External);
        
       for (var i = 0; i < 5; i++)
       {
           await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink1, TestContext.Current.CancellationToken);
       }
       
       var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink1, TestContext.Current.CancellationToken));
       
       exception.ErrorCode.Should().Be(403);
    }
    

    [Theory]
    [MemberData(nameof(InvalidFileShareFillingForms))]
    public async Task CreateMultipleLinks_InFormFillingRoom_ReturnsError(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom =  (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room with multiple links", roomType: RoomType.FillingFormsRoom), TestContext.Current.CancellationToken)).Response;

        // Act - Get a primary external link
        await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Create additional links
        var additionalLink1 = new RoomLinkRequest(
            access: fileShare,
            title: "Additional Link 1",
            linkType: LinkType.External);
        
       var exception = await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.SetRoomLinkAsync(customRoom.Id, additionalLink1, TestContext.Current.CancellationToken));
       
       exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateMultipleLinks_InRoom_ReturnsUpdatedLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("room with links to update");

        // Create two additional links
        var link1Request = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Link to Update 1",
            linkType: LinkType.External);

        var link2Request = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Link to Update 2",
            linkType: LinkType.External);

        var link1Response = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, link1Request, TestContext.Current.CancellationToken)).Response;
        var link2Response = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, link2Request, TestContext.Current.CancellationToken)).Response;

        var link1SharedTo = link1Response.SharedLink;
        var link2SharedTo = link2Response.SharedLink;

        // Act - Update both links
        var updateLink1Request = new RoomLinkRequest(
            linkId: link1SharedTo.Id,
            access: FileShare.Editing,
            title: "Updated Link 1",
            linkType: LinkType.External,
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(5) });

        var updateLink2Request = new RoomLinkRequest(
            linkId: link2SharedTo.Id,
            access: FileShare.Comment,
            title: "Updated Link 2",
            linkType: LinkType.External,
            password: "testpassword");

        var updatedLink1Response = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, updateLink1Request, TestContext.Current.CancellationToken)).Response;
        var updatedLink2Response = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, updateLink2Request, TestContext.Current.CancellationToken)).Response;

        // Get all links after updates
        var allLinks = (await _roomsApi.GetRoomLinksAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var updatedLink1SharedTo = updatedLink1Response.SharedLink;
        var updatedLink2SharedTo = updatedLink2Response.SharedLink;

        // Verify first link updates
        updatedLink1SharedTo.Id.Should().Be(link1SharedTo.Id);
        updatedLink1SharedTo.Title.Should().Be("Updated Link 1");
        updatedLink1Response.Access.Should().Be(FileShare.Editing);
        updatedLink1SharedTo.ExpirationDate.Should().NotBeNull();

        // Verify second link updates
        updatedLink2SharedTo.Id.Should().Be(link2SharedTo.Id);
        updatedLink2SharedTo.Title.Should().Be("Updated Link 2");
        updatedLink2Response.Access.Should().Be(FileShare.Comment);
        updatedLink2SharedTo.Password.Should().Be("testpassword");

        // Verify links in the complete list
        allLinks.Should().Contain(link => 
                link.SharedLink.Title == "Updated Link 1" && 
                link.Access == FileShare.Editing);

        allLinks.Should().Contain(link => 
            link.SharedLink.Title == "Updated Link 2" && 
            link.Access == FileShare.Comment);
    }
    
    [Theory]
    [MemberData(nameof(ValidFileShare))]
    public async Task UpdatePrimaryExternalLink_ValidFileShare_ReturnsLinkData(FileShare fileShare)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var data = new RoomLinkRequest(sharedTo.Id, fileShare, new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1) }, false, customRoom.Title + " updated", LinkType.External, "11111111", true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        // Assert
        updatedExternalLink.Should().NotBeNull();
        updatedExternalLink.Access.Should().Be(data.Access);

        updatedSharedTo.Id.Should().Be(data.LinkId);
        //updatedSharedTo.ExpirationDate.UtcTime.Should().Be(data.ExpirationDate.UtcTime);
        updatedSharedTo.Title.Should().Be(data.Title);
        updatedSharedTo.Password.Should().Be(data.Password);
        updatedSharedTo.DenyDownload.Should().Be(data.DenyDownload);
    }
    
    [Fact]
    public async Task ExternalLinkDenyDownload_InternalUser_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        var file = await CreateFile("file_to_share.docx", customRoom.Id);
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, denyDownload: true);
        await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken);
        
        file = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        file.Should().NotBeNull();
        file.Security.Download.Should().BeTrue();
    }
    
    [Fact]
    public async Task ExternalLinkDenyDownload_ExternalUser_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        var file = await CreateFile("file_to_share.docx", customRoom.Id);
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, denyDownload: true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        file = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        file.Should().NotBeNull();
        file.Security.Download.Should().BeFalse();
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_ExternalUser_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        var file = await CreateFile("file_to_share.docx", customRoom.Id);
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        exception.ErrorCode.Should().Be(403);
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_ExternalUserRequirePassword_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareData = (await _sharingApi.GetExternalShareDataAsync(updatedSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        externalShareData.Status.Should().Be(Status.RequiredPassword);
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_CheckInvalidPassword_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
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
    public async Task ExternalLinkWithPassword_ExternalUserWithPassword_ReturnsFileData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        var file = await CreateFile("file_to_share.docx", customRoom.Id);
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = externalLink.SharedLink;

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedExternalLink.SharedLink;
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareDataWithHttpInfo = await _sharingApi.ApplyExternalSharePasswordWithHttpInfoAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password }, cancellationToken: TestContext.Current.CancellationToken);
        var setCookie = externalShareDataWithHttpInfo.Headers.ToDictionary()["Set-Cookie"];
        var anonymousSessionKey = setCookie.First();
       
        _filesClient.DefaultRequestHeaders.Add("Cookie", anonymousSessionKey);
        var createdFile = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove("Cookie");
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        file.Should().NotBeNull();
        createdFile.Title.Should().Be(file.Title);
    }

    [Fact]
    public async Task SetRoomLinkAsync_PublicRoomWithNoneAccess_ReturnsNewLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("Public Room Test", roomType: RoomType.PublicRoom), TestContext.Current.CancellationToken)).Response;

        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(publicRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = primaryLink.SharedLink;

        // Act - Set the link with FileShare.None
        var updateRequest = new RoomLinkRequest(
            linkId: originalSharedTo.Id, 
            access: FileShare.None, 
            linkType: LinkType.External);

        var updatedLink = (await _roomsApi.SetRoomLinkAsync(publicRoom.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedLink.SharedLink;

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();

        // Verify that a new link is returned
        updatedSharedTo.Id.Should().NotBe(originalSharedTo.Id);
        updatedSharedTo.RequestToken.Should().NotBe(originalSharedTo.RequestToken);

        // Get all links to make sure both exist
        var allLinks = (await _roomsApi.GetRoomLinksAsync(publicRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        allLinks.Should().HaveCount(1); // The original link should be replaced
    }

    [Fact]
    public async Task SetRoomLinkAsync_PublicRoomWithExpiration_ReturnsOldLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = await CreatePublicRoom("Public Room Test");

        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(publicRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = primaryLink.SharedLink;

        // Act - Set the link with FileShare.None
        var updateRequest = new RoomLinkRequest(
            linkId: originalSharedTo.Id, 
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1)}, 
            linkType: LinkType.External);

        var updatedLink = (await _roomsApi.SetRoomLinkAsync(publicRoom.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = updatedLink.SharedLink;

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();
        updatedSharedTo.ExpirationDate.Should().BeNull();
    }
    
    [Fact]
    public async Task SetRoomLinkAsync_CustomRoomWithNoneAccess_ReturnsNull()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("Public Room Test");

        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = primaryLink.SharedLink;

        // Act - Set the link with FileShare.None
        var updateRequest = new RoomLinkRequest(
            linkId: originalSharedTo.Id, 
            access: FileShare.None, 
            linkType: LinkType.External);

        var updatedLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        
        // Assert
        updatedLink.Should().BeNull();
        
        var allLinks = (await _roomsApi.GetRoomLinksAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        allLinks.Should().BeEmpty(); 
    }

    [Fact]
    public async Task AccessRoomWithMultipleLinks_DifferentPermissions_ReturnsCorrectAccess()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("room with multiple access links");
        var file = await CreateFile("file_in_room.docx", customRoom.Id);

        // Create multiple links with different permissions
        var readOnlyLink = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Read Only Link",
            linkType: LinkType.External);

        var editingLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Editing Link",
            linkType: LinkType.External);

        var readOnlyResponse = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, readOnlyLink, TestContext.Current.CancellationToken)).Response;
        var editingResponse = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, editingLink, TestContext.Current.CancellationToken)).Response;

        var readOnlySharedTo = readOnlyResponse.SharedLink;
        var editingSharedTo = editingResponse.SharedLink;

        // Act - Access with read-only link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, readOnlySharedTo.RequestToken);
        var readOnlyAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access with editing link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, editingSharedTo.RequestToken);
        var editingAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        readOnlyAccess.Should().NotBeNull();
        readOnlyAccess.Security.EditHistory.Should().BeFalse(); // Read-only shouldn't allow editing history
        readOnlyAccess.Security.Edit.Should().BeFalse(); // Read-only shouldn't allow editing

        editingAccess.Should().NotBeNull();
        editingAccess.Security.Edit.Should().BeTrue(); // Editing link should allow editing
    }

    [Fact]
    public async Task RoomShare_MultipleExternalLinks_CheckLastAccessRights()
    {
        // Arrange: owner creates a virtual data room and a file inside it
        await _filesClient.Authenticate(Initializer.Owner);
        var owner = Initializer.Owner;
        var user2 = await Initializer.InviteContact(EmployeeType.User);

        var customRoom = await CreateCustomRoom("room_last_link_rights");
        var file = await CreateFile("file_in_room_last_link_rights.docx", customRoom.Id);

        var accessTypes = new[] { FileShare.Read, FileShare.Comment, FileShare.Editing };

        foreach (var access in accessTypes)
        {
            // Owner: create an external link with the required access
            await _filesClient.Authenticate(owner);
            var linkRequest = new RoomLinkRequest(
                access: access,
                title: $"Link_{access}",
                linkType: LinkType.External);

            var linkResponse = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, linkRequest, TestContext.Current.CancellationToken)).Response;
            var requestToken = linkResponse.SharedLink.RequestToken;

            // User2: open the room via the external link (use the token), this should register the room for the user
            await _filesClient.Authenticate(user2);
            _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, requestToken);
            var openedRoom = (await _foldersApi.GetFolderByFolderIdAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            // Mark room as recent by Link (otherwise it will not be displayed in the list of rooms)
            await _sharingApi.GetExternalShareDataWithHttpInfoAsync(requestToken, folderId: customRoom.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken);
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

            openedRoom.Should().NotBeNull();
            var roomFile = openedRoom.Files.Find(f => f.Title == file.Title);
            roomFile.Should().NotBeNull();
            roomFile.Access.Should().Be(access);

            // Ensure the room appears in virtual rooms list
            var roomsList = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
            roomsList.Should().NotBeNull();
            roomsList.Folders.Should().Contain(f => f.Title == customRoom.Title);

            // Now check file access for user2 (without token) — rights should be persisted according to the last link
            var fileInfoAsUser2 = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            fileInfoAsUser2.Should().NotBeNull();
            fileInfoAsUser2.Access.Should().Be(access);
            fileInfoAsUser2.Security.Read.Should().BeTrue();

            switch (access)
            {
                case FileShare.Read:
                    fileInfoAsUser2.Security.Edit.Should().BeFalse();
                    fileInfoAsUser2.Security.Comment.Should().BeFalse();
                    break;
                case FileShare.Comment:
                    fileInfoAsUser2.Security.Edit.Should().BeFalse();
                    fileInfoAsUser2.Security.Comment.Should().BeTrue();
                    break;
                case FileShare.Editing:
                    fileInfoAsUser2.Security.Edit.Should().BeTrue();
                    fileInfoAsUser2.Security.Comment.Should().BeTrue();
                    break;
            }

            // Prepare for next iteration: re-authenticate owner to create the next link
            await _filesClient.Authenticate(owner);
        }
    }

    [Fact]
    public async Task RoomShare_PersonalRights_PreservedAfterVisitingExternalLinks()
    {
        // Arrange: owner creates a room and a file inside it, invites user2 and grants personal Read rights
        await _filesClient.Authenticate(Initializer.Owner);
        var owner = Initializer.Owner;
        var user2 = await Initializer.InviteContact(EmployeeType.User);

        var room = await CreateCustomRoom("room_personal_rights");
        var file = await CreateFile("file_in_room_personal_rights.docx", room.Id);

        var setRoomSecurityResult = (await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [ new RoomInvitation { Id = user2.Id, Access = FileShare.Read } ]
        }, TestContext.Current.CancellationToken)).Response;

        setRoomSecurityResult.Should().NotBeNull();

        // Owner creates two external links: Comment and Editing
        var commentLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(access: FileShare.Comment, title: "CommentLink", linkType: LinkType.External), TestContext.Current.CancellationToken)).Response;
        var commentToken = commentLinkResponse.SharedLink.RequestToken;

        var editLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(access: FileShare.Editing, title: "EditLink", linkType: LinkType.External), TestContext.Current.CancellationToken)).Response;
        var editToken = editLinkResponse.SharedLink.RequestToken;

        // Log out and check access to links for Anonymous
        await _filesClient.Authenticate(null);

        // Anonymous visits Comment link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, commentToken);

        var openedRoomByAnonymousViaCommentLink = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoomByAnonymousViaCommentLink.Should().NotBeNull();
        openedRoomByAnonymousViaCommentLink.Current.Access.Should().Be(FileShare.Comment);

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Anonymous visits Edit link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, editToken);

        var openedRoomByAnonymousViaEditLink = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoomByAnonymousViaEditLink.Should().NotBeNull();
        openedRoomByAnonymousViaEditLink.Current.Access.Should().Be(FileShare.Editing);

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Authenticate user2 and open room
        await _filesClient.Authenticate(user2);

        // Check personal Read rights
        var openedRoom = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoom.Should().NotBeNull();
        openedRoom.Current.Access.Should().Be(FileShare.Read);

        var openedFile = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoom.Should().NotBeNull();
        openedRoom.Current.Access.Should().Be(FileShare.Read);

        // User2 visits Comment link -> should still have personal Read rights on the file
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, commentToken);

        var openedRoomViaCommentLink = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoomViaCommentLink.Should().NotBeNull();
        openedRoomViaCommentLink.Current.Access.Should().Be(FileShare.Read);

        // Mark room as recent by Link
        await _sharingApi.GetExternalShareDataWithHttpInfoAsync(commentToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken);

        var openedFileViaCommentLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedFileViaCommentLink.Should().NotBeNull();
        openedFileViaCommentLink.Access.Should().Be(FileShare.Read);
        openedFileViaCommentLink.Security.Read.Should().BeTrue();
        openedFileViaCommentLink.Security.Comment.Should().BeFalse();
        openedFileViaCommentLink.Security.Edit.Should().BeFalse();

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);


        // User2 visits Edit link -> personal Read rights must still be preserved
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, editToken);

        var openedRoomViaEditLink = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedRoomViaEditLink.Should().NotBeNull();
        openedRoomViaEditLink.Current.Access.Should().Be(FileShare.Read);

        // Mark room as recent by Link
        await _sharingApi.GetExternalShareDataWithHttpInfoAsync(editToken, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken);

        var openedFileViaEditLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        openedFileViaEditLink.Should().NotBeNull();
        openedFileViaEditLink.Access.Should().Be(FileShare.Read);
        openedFileViaEditLink.Security.Read.Should().BeTrue();
        openedFileViaEditLink.Security.Comment.Should().BeFalse();
        openedFileViaEditLink.Security.Edit.Should().BeFalse();

        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
    }

    [Theory]
    [InlineData(RoomType.PublicRoom)]
    public async Task CheckEditAccess_InvalidRoomType_ReturnsFalse(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        primaryLink.CanEditAccess.Should().BeFalse();
    }
    
    [Theory]
    [InlineData(RoomType.CustomRoom)]
    public async Task CheckEditAccess_ValidRoomType_ReturnsTrue(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        primaryLink.CanEditInternal.Should().BeTrue();
    }
    
   public static TheoryData<RoomType> Bug77820Data => [RoomType.CustomRoom, RoomType.PublicRoom];

   
    [Theory]
    [MemberData(nameof(Bug77820Data))]
    [Trait("Category", "Bug")]
    [Trait("Bug", "77820")]
    public async Task RoomRemovedFromList_DeletingRoomLink_StatusOk(RoomType roomType)
    {
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        
        var user = await Initializer.InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await _sharingApi.GetExternalShareDataAsync(primaryLink.SharedLink.RequestToken, cancellationToken: TestContext.Current.CancellationToken);
        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FolderIds = [new(room.Id)] }, cancellationToken: TestContext.Current.CancellationToken);

        var response = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        response.Should().NotBeNull();
        response.Folders.Should().BeEmpty();
    }
}
