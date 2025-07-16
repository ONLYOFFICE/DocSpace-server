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

using LinkType = DocSpace.Sdk.Model.LinkType;

namespace ASC.Files.Tests.FilesController;

[Collection("Test Collection")]
public class ShareInheritanceTest(
    FilesApiFactory filesFactory,
    WepApiFactory apiFactory,
    PeopleFactory peopleFactory,
    FilesServiceFactory filesServiceProgram)
    : BaseTest(filesFactory, apiFactory, peopleFactory, filesServiceProgram)
{
    [Theory]
    [MemberData(nameof(ValidRoomTypesForShare))]
    public async Task RoomWithLink_FolderWithoutLink_FileWithoutLink_InheritsRoomPermissions(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("room title", roomType: roomType), TestContext.Current.CancellationToken)).Response;
        var folder = await CreateFolder("folder without link", room.Id);
        var file = await CreateFile("file without link.docx", folder.Id);

        // Create room link with editing access
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link",
            linkType: LinkType.External);

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);

        // Act - Access file as external user through room link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Edit.Should().BeTrue(); // Should inherit editing permissions from room
        fileAccess.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task RoomWithLink_FolderWithLink_FileWithoutLink_InheritsFolderPermissions()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room with link");
        var folder = await CreateFolder("folder with link", room.Id);
        var file = await CreateFile("file without link.docx", folder.Id);

        // Create room link with editing access
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link",
            linkType: LinkType.External);

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);

        // Create folder link with read-only access (more restrictive than room)
        var folderLink = new FolderLinkRequest(
            access: FileShare.Read,
            title: "Folder Link");

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Act - Access file through folder link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccessViaFolder = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access file through room link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        var fileAccessViaRoom = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        // When accessed via folder link, should have folder's permissions (read-only)
        fileAccessViaFolder.Should().NotBeNull();
        fileAccessViaFolder.Security.Edit.Should().BeFalse();
        fileAccessViaFolder.Access.Should().Be(FileShare.Read);

        // When accessed via room link, should have room's permissions (editing)
        fileAccessViaRoom.Should().NotBeNull();
        fileAccessViaRoom.Security.Edit.Should().BeTrue();
        fileAccessViaRoom.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task RoomWithLink_FolderWithLink_FileWithLink_UsesOwnPermissions()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room with link");
        var folder = await CreateFolder("folder with link", room.Id);
        var file = await CreateFile("file with link.docx", folder.Id);

        // Create room link with editing access
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link",
            linkType: LinkType.External);

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);

        // Create folder link with read-only access
        var folderLink = new FolderLinkRequest(
            access: FileShare.Read,
            title: "Folder Link");

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Create file link with comment access
        var fileLink = new FileLinkRequest(
            access: FileShare.Comment);

        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Act - Access file through file link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccessViaFileLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access file through folder link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccessViaFolderLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access file through room link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        var fileAccessViaRoomLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        // When accessed via file's own link, should have file's permissions (comment)
        fileAccessViaFileLink.Should().NotBeNull();
        fileAccessViaFileLink.Security.Comment.Should().BeTrue();
        fileAccessViaFileLink.Security.Edit.Should().BeFalse();
        fileAccessViaFileLink.Access.Should().Be(FileShare.Comment);

        // When accessed via folder link, should have folder's permissions (read-only)
        fileAccessViaFolderLink.Should().NotBeNull();
        fileAccessViaFolderLink.Security.Comment.Should().BeFalse();
        fileAccessViaFolderLink.Security.Edit.Should().BeFalse();
        fileAccessViaFolderLink.Access.Should().Be(FileShare.Read);

        // When accessed via room link, should have room's permissions (editing)
        fileAccessViaRoomLink.Should().NotBeNull();
        fileAccessViaRoomLink.Security.Comment.Should().BeTrue();
        fileAccessViaRoomLink.Security.Edit.Should().BeTrue();
        fileAccessViaRoomLink.Access.Should().Be(FileShare.Editing);
    }


    [Fact]
    public async Task FolderWithLink_FileWithoutLink_InheritsFolderPermissions()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder with link", Initializer.Owner);
        var file = await CreateFile("file without link.docx", folder.Id);

        // Create folder link with read-only access
        var folderLink = new FolderLinkRequest(
            access: FileShare.Read,
            title: "Folder Link");

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Act - Access file through folder link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Edit.Should().BeFalse(); // Should inherit read-only permissions
        fileAccess.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task FolderWithLink_FileWithLink_UsesOwnPermissions()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder with link", Initializer.Owner);
        var file = await CreateFile("file with link.docx", folder.Id);

        // Create folder link with read-only access
        var folderLink = new FolderLinkRequest(
            access: FileShare.Read,
            title: "Folder Link");

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Create file link with editing access
        var fileLink = new FileLinkRequest(
            access: FileShare.Editing);

        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Act - Access file through file link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccessViaFileLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Act - Access file through folder link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccessViaFolderLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        // When accessed via file's own link, should have file's permissions (editing)
        fileAccessViaFileLink.Should().NotBeNull();
        fileAccessViaFileLink.Security.Edit.Should().BeTrue();
        fileAccessViaFileLink.Access.Should().Be(FileShare.Editing);

        // When accessed via folder link, should have folder's permissions (read-only)
        fileAccessViaFolderLink.Should().NotBeNull();
        fileAccessViaFolderLink.Security.Edit.Should().BeFalse();
        fileAccessViaFolderLink.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task RoomWithDenyDownload_InheritsToFolderAndFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room with deny download");
        var folder = await CreateFolder("folder without link", room.Id);
        var file = await CreateFile("file without link.docx", folder.Id);

        // Create room link with editing access and deny download
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link with Deny Download",
            linkType: LinkType.External,
            denyDownload: true);

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);

        // Act - Access file as external user through room link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Download.Should().BeFalse(); // Download should be denied due to room setting
        fileAccess.Security.Edit.Should().BeTrue(); // Editing should still be allowed
    }

    [Fact]
    public async Task FolderWithDenyDownload_InheritsToFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder with deny download", Initializer.Owner);
        var file = await CreateFile("file without link.docx", folder.Id);

        // Create folder link with editing access and deny download
        var folderLink = new FolderLinkRequest(
            access: FileShare.Editing,
            title: "Folder Link with Deny Download",
            denyDownload: true);

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Act - Access file through folder link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Download.Should().BeFalse(); // Download should be denied due to folder setting
        fileAccess.Security.Edit.Should().BeTrue(); // Editing should still be allowed
    }

    [Fact]
    public async Task FileWithDenyDownload_OverridesFolderAndRoomSettings()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room without deny download");
        var folder = await CreateFolder("folder without deny download", room.Id);
        var file = await CreateFile("file with deny download.docx", folder.Id);

        // Create room link with editing access without deny download
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link",
            linkType: LinkType.External,
            denyDownload: false);

        await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken);

        // Create file link with deny download
        var fileLink = new FileLinkRequest(
            access: FileShare.Editing,
            denyDownload: true);

        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Act - Access file through file link
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Download.Should().BeFalse(); // Download should be denied due to file's own setting
        fileAccess.Security.Edit.Should().BeTrue(); // Editing should still be allowed
    }

    [Fact]
    public async Task RoomWithPassword_DoesNotInheritToFolderOrFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room with password");
        var folder = await CreateFolder("folder in room", room.Id);
        var file = await CreateFile("file in room.docx", folder.Id);

        // Create room link with password
        var password = "roompassword123";
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link with Password",
            linkType: LinkType.External,
            password: password);

        await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken);

        // Create folder link without password
        var folderLink = new FolderLinkRequest(
            access: FileShare.Editing,
            title: "Folder Link without Password");

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);
        
        // Create file link without password
        var fileLink = new FileLinkRequest(access: FileShare.Editing);
        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);
        
        // Act - Try to access folder via folder link (should work without password)
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var folderAccess = (await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        
        // Act - Try to access folder via file link (should work without password)
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        folderAccess.Should().NotBeNull();
        folderAccess.Current.Should().NotBeNull();
        folderAccess.Current.Title.Should().Be(folder.Title);
        
        // Assert
        fileAccess.Should().NotBeNull();
        fileAccess.Security.Edit.Should().BeTrue(); // Editing should still be allowed
    }

    [Fact]
    public async Task FileWithPassword_RequiresPasswordRegardlessOfParent()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room without password");
        var folder = await CreateFolder("folder in room", room.Id);
        var file = await CreateFile("file with password.docx", folder.Id);

        // Create room link without password
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Room Link without Password",
            linkType: LinkType.External);

        await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken);

        // Create file link with password
        var filePassword = "filepassword123";
        var fileLink = new FileLinkRequest(
            access: FileShare.Editing,
            password: filePassword);

        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Act - Try to access file without providing password
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);

        // Check that password is required
        var externalShareData = (await _filesSharingApi.GetExternalShareDataAsync(fileSharedTo.RequestToken, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Then provide password and verify access
        var externalShareDataWithHttpInfo = await _filesSharingApi.ApplyExternalSharePasswordWithHttpInfoAsync(
            fileSharedTo.RequestToken,
            new ExternalShareRequestParam { Password = filePassword },
            cancellationToken: TestContext.Current.CancellationToken);

        var setCookie = externalShareDataWithHttpInfo.Headers.ToDictionary()["Set-Cookie"];
        var anonymousSessionKey = setCookie.First();

        _filesClient.DefaultRequestHeaders.Add("Cookie", anonymousSessionKey);
        var fileAccess = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove("Cookie");
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        externalShareData.Status.Should().Be(Status.RequiredPassword);
        fileAccess.Should().NotBeNull();
        fileAccess.Title.Should().Be(file.Title);
    }

    [Fact]
    public async Task MultipleLinkLevels_UsesCorrectAccessBasedOnEntryPoint()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("multi-link room");
        var folder = await CreateFolder("multi-link folder", room.Id);
        var file = await CreateFile("multi-link file.docx", folder.Id);

        // Create links with different access levels at each level
        var roomLink = new RoomLinkRequest(
            access: FileShare.Read,
            title: "Read-only Room Link",
            linkType: LinkType.External);

        var folderLink = new FolderLinkRequest(
            access: FileShare.Comment,
            title: "Comment Folder Link");

        var fileLink = new FileLinkRequest(
            access: FileShare.Editing);

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;

        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Act - Access file through different entry points
        await _filesClient.Authenticate(null);

        // Via room link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        var fileAccessViaRoom = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Via folder link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccessViaFolder = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Via file link
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccessViaFile = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        // Each access should reflect the entry point's permissions

        // Room link access (read-only)
        fileAccessViaRoom.Should().NotBeNull();
        fileAccessViaRoom.Security.Edit.Should().BeFalse();
        fileAccessViaRoom.Security.Comment.Should().BeFalse();
        fileAccessViaRoom.Access.Should().Be(FileShare.Read);

        // Folder link access (comment)
        fileAccessViaFolder.Should().NotBeNull();
        fileAccessViaFolder.Security.Edit.Should().BeFalse();
        fileAccessViaFolder.Security.Comment.Should().BeTrue();
        fileAccessViaFolder.Access.Should().Be(FileShare.Comment);

        // File link access (editing)
        fileAccessViaFile.Should().NotBeNull();
        fileAccessViaFile.Security.Edit.Should().BeTrue();
        fileAccessViaFile.Security.Comment.Should().BeTrue();
        fileAccessViaFile.Access.Should().Be(FileShare.Editing);
    }
    
    [Fact]
    public async Task ExpiredRoomLink_FallsBackToFolderLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("room with expired link");
        var folder = await CreateFolder("folder with valid link", room.Id);
        var file = await CreateFile("file.docx", folder.Id);

        // Create room link that expires in 1 second
        var roomLink = new RoomLinkRequest(
            access: FileShare.Editing,
            title: "Expired Room Link",
            linkType: LinkType.External,
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddSeconds(1) });

        // Create folder link that doesn't expire
        var folderLink = new FolderLinkRequest(
            access: FileShare.Read,
            title: "Valid Folder Link");

        var roomLinkResponse = (await _roomsApi.SetRoomLinkAsync(room.Id, roomLink, TestContext.Current.CancellationToken)).Response;
        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;

        var roomSharedTo = DeserializeSharedToLink(roomLinkResponse);
        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);

        // Wait for room link to expire
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Act - Try to access via room link (should fail due to expiration)
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, roomSharedTo.RequestToken);
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Access via folder link (should work)
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        var fileAccessViaFolder = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccessViaFolder.Should().NotBeNull();
        fileAccessViaFolder.Security.Edit.Should().BeFalse(); // Should have folder link's read-only permission
        fileAccessViaFolder.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task ExpiredFolderLink_FallsBackToFileLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("folder with expired link", Initializer.Owner);
        var file = await CreateFile("file with valid link.docx", folder.Id);

        // Create folder link that expires in 1 second
        var folderLink = new FolderLinkRequest(
            access: FileShare.Editing,
            title: "Expired Folder Link",
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddSeconds(1) });

        // Create file link that doesn't expire
        var fileLink = new FileLinkRequest(
            access: FileShare.Read);

        var folderLinkResponse = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, folderLink, TestContext.Current.CancellationToken)).Response;
        var fileLinkResponse = (await _filesApi.CreateFilePrimaryExternalLinkAsync(file.Id, fileLink, TestContext.Current.CancellationToken)).Response;

        var folderSharedTo = DeserializeSharedToLink(folderLinkResponse);
        var fileSharedTo = DeserializeSharedToLink(fileLinkResponse);

        // Wait for folder link to expire
        await Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        // Act - Try to access via folder link (should fail due to expiration)
        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, folderSharedTo.RequestToken);
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Access via file link (should work)
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, fileSharedTo.RequestToken);
        var fileAccessViaFileLink = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);

        // Assert
        fileAccessViaFileLink.Should().NotBeNull();
        fileAccessViaFileLink.Security.Edit.Should().BeFalse(); // Should have file link's read-only permission
        fileAccessViaFileLink.Access.Should().Be(FileShare.Read);
    }
    
    [Fact]
    public async Task SetFileLinkAsync_FileWithNoneAccessPublicRoom_ReturnsNewLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = await CreatePublicRoom("Public Room Test");
        var file = await CreateFile("File in Public Room", publicRoom.Id);

        // Get the primary external link
        var primaryLink = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

        // Act - Set the link with FileShare.None
        var updateRequest = new FileLinkRequest(
            linkId: originalSharedTo.Id, 
            access: FileShare.None);

        var updatedLink = (await _filesApi.SetFileExternalLinkAsync(file.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedLink);

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();

        // Verify that a new link is returned
        updatedSharedTo.Id.Should().NotBe(originalSharedTo.Id);
        updatedSharedTo.RequestToken.Should().NotBe(originalSharedTo.RequestToken);

        // Get all links to make sure both exist
        var allLinks = (await _filesApi.GetFileLinksAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        allLinks.Should().HaveCount(1); // The original link should be replaced
    }
    
    [Fact]
    public async Task SetFileInRoomLinkAsync_PublicRoomWithFileExpiration_ReturnsNoExpiration()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("Public Room Test", roomType: RoomType.PublicRoom), TestContext.Current.CancellationToken)).Response;
        var file = await CreateFile("File in Public Room", publicRoom.Id);
        
        // Get the primary external link
        var primaryLink = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

        // Act - Set the link with FileShare.None
        var updateRequest = new FileLinkRequest(
            linkId: originalSharedTo.Id, 
            expirationDate: new ApiDateTime { UtcTime = DateTime.UtcNow.AddDays(1)});

        var updatedLink = (await _filesApi.SetFileExternalLinkAsync(file.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedLink);

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();
        updatedSharedTo.ExpirationDate.Should().BeNull();
    }
    
    [Fact]
    public async Task SetFileInRoomLinkAsync_PublicRoomWithFileInternal_ReturnsExternal()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto("Public Room Test", roomType: RoomType.PublicRoom), TestContext.Current.CancellationToken)).Response;
        var file = await CreateFile("File in Public Room", publicRoom.Id);
        
        // Get the primary external link
        var primaryLink = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

        // Act - Set the link with FileShare.None
        var updateRequest = new FileLinkRequest(
            linkId: originalSharedTo.Id, 
            varInternal: true);

        var updatedLink = (await _filesApi.SetFileExternalLinkAsync(file.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedLink);

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();
        updatedSharedTo.Internal.Should().BeFalse();
    }
    
    [Fact]
    public async Task SetFolderLinkAsync_FolderWithNoneAccessPublicRoom_ReturnsNewLink()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var publicRoom = await CreatePublicRoom("Public Room Test");
        var folder = await CreateFolder("Folder in Public Room", publicRoom.Id);

        // Get the primary external link
        var primaryLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var originalSharedTo = DeserializeSharedToLink(primaryLink);

        // Act - Set the link with FileShare.None
        var updateRequest = new FolderLinkRequest(
            linkId: originalSharedTo.Id, 
            access: FileShare.None);

        var updatedLink = (await _foldersApi.SetFolderPrimaryExternalLinkAsync(folder.Id, updateRequest, TestContext.Current.CancellationToken)).Response;
        var updatedSharedTo = DeserializeSharedToLink(updatedLink);

        // Assert
        updatedLink.Should().NotBeNull();
        updatedSharedTo.Should().NotBeNull();

        // Verify that a new link is returned
        updatedSharedTo.Id.Should().NotBe(originalSharedTo.Id);
        updatedSharedTo.RequestToken.Should().NotBe(originalSharedTo.RequestToken);

        // Get all links to make sure both exist
        var allLinks = (await _foldersApi.GetFolderLinksAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        allLinks.Should().HaveCount(1); // The original link should be replaced
    }
}