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

using LinkType = DocSpace.API.SDK.Model.LinkType;
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
    public static TheoryData<RoomType> Data =>
    [
        RoomType.EditingRoom, RoomType.VirtualDataRoom
    ];
    
    [Fact]
    public async Task CreatePrimaryExternalLink_CustomRoom_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
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
        result.CanEditAccess.Should().Be(false);
        result.IsOwner.Should().Be(false);
        
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
        await Assert.ThrowsAsync<ApiException>(async () => await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken));
    }
    
    [Fact]
    public async Task UpdatePrimaryExternalLink_ValidFileShare_ReturnsLinkData()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1) }, customRoom.Title + " updated", LinkType.External, "11111111", true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, denyDownload: true);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, updatedSharedTo.RequestToken);
        await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
    }
    
    [Fact]
    public async Task ExternalLinkWithPassword_ExternalUserRequirePassword_ReturnsError()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: "11111111");
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
        var customRoom = await CreateCustomRoom("custom room");
        
        // Act
        var externalLink = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(customRoom.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
        var sharedTo = JsonSerializer.Deserialize<FileShareLink>((externalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);

        var password = "11111111";
        var data = new RoomLinkRequest(sharedTo.Id, FileShare.Editing, linkType: LinkType.External, password: password);
        var updatedExternalLink = (await _roomsApi.SetRoomLinkAsync(customRoom.Id, data, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = JsonSerializer.Deserialize<FileShareLink>((updatedExternalLink.SharedTo as JObject).ToString(), JsonSerializerOptions.Web);
        
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
}
