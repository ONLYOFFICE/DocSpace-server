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

using LinkType = Docspace.Model.LinkType;
using Task = System.Threading.Tasks.Task;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class RoomSharingTest(
    FilesApiFactory filesFactory, 
    WepApiFactory apiFactory, 
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram) 
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    public static TheoryData<RoomType> ValidRoomTypesForShare =>
    [
        RoomType.CustomRoom, RoomType.PublicRoom
    ];
    
    public static TheoryData<RoomType> Data =>
    [
        RoomType.EditingRoom, RoomType.VirtualDataRoom
    ];
    
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
        var sharedToJObject = result.SharedTo as JObject;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>(sharedToJObject.ToString(), JsonSerializerOptions.Web);
        
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
        var sharedToJObject = result.SharedTo as JObject;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>(sharedToJObject.ToString(), JsonSerializerOptions.Web);
        
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
    [MemberData(nameof(Data))]
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
            DeserializeSharedToLink(link).Title == "Additional Link 1" && 
            link.Access == FileShare.Read);

        allLinks.Should().Contain(link => 
            DeserializeSharedToLink(link).Title == "Additional Link 2" && 
            link.Access == FileShare.Editing);
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

        var link1SharedTo = DeserializeSharedToLink(link1Response);
        var link2SharedTo = DeserializeSharedToLink(link2Response);

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
        var updatedLink1SharedTo = DeserializeSharedToLink(updatedLink1Response);
        var updatedLink2SharedTo = DeserializeSharedToLink(updatedLink2Response);

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
                DeserializeSharedToLink(link).Title == "Updated Link 1" && 
                link.Access == FileShare.Editing);

        allLinks.Should().Contain(link => 
            DeserializeSharedToLink(link).Title == "Updated Link 2" && 
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
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new RoomLinkRequest(sharedTo.Id, fileShare, new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1) }, customRoom.Title + " updated", LinkType.External, "11111111", true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
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
        var sharedTo = DeserializeSharedToLink(externalLink);
        
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
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, denyDownload: true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
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
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
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
        var sharedTo = DeserializeSharedToLink(externalLink);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareData = (await _filesSharingApi.GetExternalShareDataAsync(updatedSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
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
        var sharedTo = DeserializeSharedToLink(externalLink);

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareDataWrongPassword = (await _filesSharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password + "1" }, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var externalShareData = (await _filesSharingApi.ApplyExternalSharePasswordAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password }, cancellationToken: TestContext.Current.CancellationToken)).Response;
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
        var sharedTo = DeserializeSharedToLink(externalLink);

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedExternalLink);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        var externalShareDataWithHttpInfo = await _filesSharingApi.ApplyExternalSharePasswordWithHttpInfoAsync(updatedSharedTo.RequestToken, new ExternalShareRequestParam { Password = password }, cancellationToken: TestContext.Current.CancellationToken);
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
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

        // Act - Set the link with FileShare.None
        var updateRequest = new RoomLinkRequest(
            linkId: originalSharedTo.Id, 
            access: FileShare.None, 
            linkType: LinkType.External);

        var updatedLink = (await _roomsApi.SetRoomLinkAsync(publicRoom.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedLink);

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
    public async Task SetRoomLinkAsync_CustomRoomWithNoneAccess_ReturnsNull()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("Public Room Test");

        // Get the primary external link
        var primaryLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

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

        var readOnlySharedTo = DeserializeSharedToLink(readOnlyResponse);
        var editingSharedTo = DeserializeSharedToLink(editingResponse);

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
}
