// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Files.Tests.Tests._05_Features;

[Collection("Test Collection")]
[Trait("Category", "Features")]
[Trait("Feature", "Favorites")]
public class FavoritesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AddToFavorites_FileFromMyDocuments_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var file = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        // Act
        await AddToFavorites(file.Id);

        // Assert
        var favorites = await GetFavoritesAsync(Initializer.Owner);
        favorites.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddToFavorites_FileFromSubFolderInMyDocuments_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var subFolder = await CreateFolderInMy("favorite_subfolder", Initializer.Owner);
        var file = await CreateFile("favorite_in_subfolder.docx", subFolder.Id);

        // Act
        await AddToFavorites(file.Id);

        // Assert - a file nested in a sub-folder of "My documents" is still part of the "My documents" favorites
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER]);
        favorites.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddToFavorites_FileFromRoom_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("favorite_custom_room");
        var file = await CreateFile("favorite_in_room.docx", room.Id);

        // Act
        await AddToFavorites(file.Id);

        // Assert
        var favorites = await GetFavoritesAsync(Initializer.Owner);
        favorites.Files.Should().ContainSingle(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetFavorites_WithoutFolderTypeFilter_ReturnsFilesFromMyDocumentsAndRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFile = await CreateFile("favorite_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("favorite_public_room");
        var publicRoomFile = await CreateFile("favorite_public.docx", publicRoom.Id);

        var vdrRoom = await CreateVDRRoom("favorite_vdr_room");
        var vdrRoomFile = await CreateFile("favorite_vdr.docx", vdrRoom.Id);

        await AddToFavorites(myFile.Id, customRoomFile.Id, publicRoomFile.Id, vdrRoomFile.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner);

        // Assert - without the filter every favorite file is returned regardless of its parent folder type
        favorites.Files.Should().Contain(f => f.Title == myFile.Title);
        favorites.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        favorites.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        favorites.Files.Should().Contain(f => f.Title == vdrRoomFile.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterByMyDocuments_ReturnsOnlyMyDocumentsFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFile = await CreateFile("favorite_custom.docx", customRoom.Id);

        await AddToFavorites(myFile.Id, customRoomFile.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER]);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == myFile.Title);
        favorites.Files.Should().NotContain(f => f.Title == customRoomFile.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterByRooms_ReturnsOnlyRoomFiles()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFile = await CreateFile("favorite_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("favorite_public_room");
        var publicRoomFile = await CreateFile("favorite_public.docx", publicRoom.Id);

        await AddToFavorites(myFile.Id, customRoomFile.Id, publicRoomFile.Id);

        // Act - VirtualRooms is the common ancestor of every room, so it selects all room files
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.VirtualRooms]);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        favorites.Files.Should().Contain(f => f.Title == publicRoomFile.Title);
        favorites.Files.Should().NotContain(f => f.Title == myFile.Title);
    }

    [Theory]
    [MemberData(nameof(RoomTypeFilterCases))]
    public async Task GetFavorites_FilterBySpecificRoomType_ReturnsOnlyThatRoomFiles(RoomType roomType, FolderType folderTypeFilter)
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        var targetRoom = await CreateRoom(roomType, "favorite_target_room");
        var targetRoomFile = await CreateFile("favorite_target.docx", targetRoom.Id);

        var otherRoom = await CreateCustomRoom("favorite_other_room");
        var otherRoomFile = await CreateFile("favorite_other.docx", otherRoom.Id);

        await AddToFavorites(myFile.Id, targetRoomFile.Id, otherRoomFile.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner, [folderTypeFilter]);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == targetRoomFile.Title);
        favorites.Files.Should().NotContain(f => f.Title == myFile.Title);

        if (roomType != RoomType.CustomRoom)
        {
            favorites.Files.Should().NotContain(f => f.Title == otherRoomFile.Title);
        }
    }

    [Fact]
    public async Task GetFavorites_FilterByMultipleFolderTypes_ReturnsFilesFromAllRequestedTypes()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFile = await CreateFile("favorite_custom.docx", customRoom.Id);

        var publicRoom = await CreatePublicRoom("favorite_public_room");
        var publicRoomFile = await CreateFile("favorite_public.docx", publicRoom.Id);

        await AddToFavorites(myFile.Id, customRoomFile.Id, publicRoomFile.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER, FolderType.CustomRoom]);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == myFile.Title);
        favorites.Files.Should().Contain(f => f.Title == customRoomFile.Title);
        favorites.Files.Should().NotContain(f => f.Title == publicRoomFile.Title);
    }

    [Fact]
    public async Task AddToFavorites_FolderFromMyDocuments_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var folder = await CreateFolderInMy("favorite_my_folder", Initializer.Owner);

        // Act
        await AddFoldersToFavorites(folder.Id);

        // Assert
        var favorites = await GetFavoritesAsync(Initializer.Owner);
        favorites.Folders.Should().ContainSingle(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task AddToFavorites_FolderFromSubFolderInMyDocuments_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var subFolder = await CreateFolderInMy("favorite_parent_folder", Initializer.Owner);
        var folder = await CreateFolder("favorite_nested_folder", subFolder.Id);

        // Act
        await AddFoldersToFavorites(folder.Id);

        // Assert - a folder nested in a sub-folder of "My documents" is still part of the "My documents" favorites
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER]);
        favorites.Folders.Should().ContainSingle(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task AddToFavorites_FolderFromRoom_AppearsInFavorites()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);
        var room = await CreateCustomRoom("favorite_custom_room");
        var folder = await CreateFolder("favorite_room_folder", room.Id);

        // Act
        await AddFoldersToFavorites(folder.Id);

        // Assert
        var favorites = await GetFavoritesAsync(Initializer.Owner);
        favorites.Folders.Should().ContainSingle(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterByMyDocuments_ReturnsOnlyMyDocumentsFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFolder = await CreateFolderInMy("favorite_my_folder", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFolder = await CreateFolder("favorite_custom_folder", customRoom.Id);

        await AddFoldersToFavorites(myFolder.Id, customRoomFolder.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER]);

        // Assert
        favorites.Folders.Should().Contain(f => f.Title == myFolder.Title);
        favorites.Folders.Should().NotContain(f => f.Title == customRoomFolder.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterByRooms_ReturnsOnlyRoomFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFolder = await CreateFolderInMy("favorite_my_folder", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFolder = await CreateFolder("favorite_custom_folder", customRoom.Id);

        var publicRoom = await CreatePublicRoom("favorite_public_room");
        var publicRoomFolder = await CreateFolder("favorite_public_folder", publicRoom.Id);

        await AddFoldersToFavorites(myFolder.Id, customRoomFolder.Id, publicRoomFolder.Id);

        // Act - VirtualRooms is the common ancestor of every room, so it selects all room folders
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.VirtualRooms]);

        // Assert
        favorites.Folders.Should().Contain(f => f.Title == customRoomFolder.Title);
        favorites.Folders.Should().Contain(f => f.Title == publicRoomFolder.Title);
        favorites.Folders.Should().NotContain(f => f.Title == myFolder.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterByFolderType_AppliesToBothFilesAndFolders()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var myFile = await CreateFileInMy("favorite_my.docx", Initializer.Owner);
        var myFolder = await CreateFolderInMy("favorite_my_folder", Initializer.Owner);

        var customRoom = await CreateCustomRoom("favorite_custom_room");
        var customRoomFile = await CreateFile("favorite_custom.docx", customRoom.Id);
        var customRoomFolder = await CreateFolder("favorite_custom_folder", customRoom.Id);

        await AddToFavorites(myFile.Id, customRoomFile.Id);
        await AddFoldersToFavorites(myFolder.Id, customRoomFolder.Id);

        // Act
        var favorites = await GetFavoritesAsync(Initializer.Owner, [FolderType.USER]);

        // Assert - the parent folder type filter narrows both files and folders to "My documents"
        favorites.Files.Should().Contain(f => f.Title == myFile.Title);
        favorites.Files.Should().NotContain(f => f.Title == customRoomFile.Title);
        favorites.Folders.Should().Contain(f => f.Title == myFolder.Title);
        favorites.Folders.Should().NotContain(f => f.Title == customRoomFolder.Title);
    }

    public static TheoryData<RoomType, FolderType> RoomTypeFilterCases =>
        new()
        {
            { RoomType.CustomRoom, FolderType.CustomRoom },
            { RoomType.PublicRoom, FolderType.PublicRoom },
            { RoomType.EditingRoom, FolderType.EditingRoom },
            { RoomType.VirtualDataRoom, FolderType.VirtualDataRoom }
        };

    private async Task<FolderContentDtoInteger> GetFavoritesAsync(User user, List<FolderType>? folderType = null)
    {
        var favoritesId = await GetFolderIdAsync(FolderType.Favorites, user);

        return (await _foldersApi.GetFolderByFolderIdAsync(favoritesId, folderType: folderType, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    private async Task AddToFavorites(params int[] fileIds)
    {
        var request = new BaseBatchRequestDto { FileIds = fileIds.Select(id => new BaseBatchRequestDtoAllOfFileIds(id)).ToList() };

        await _filesOperationsApi.AddFavoritesAsync(request, cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task AddFoldersToFavorites(params int[] folderIds)
    {
        var request = new BaseBatchRequestDto { FolderIds = folderIds.Select(id => new BaseBatchRequestDtoAllOfFolderIds(id)).ToList() };

        await _filesOperationsApi.AddFavoritesAsync(request, cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<FolderDtoInteger> CreateRoom(RoomType roomType, string title) => roomType switch
    {
        RoomType.CustomRoom => await CreateCustomRoom(title),
        RoomType.PublicRoom => await CreatePublicRoom(title),
        RoomType.EditingRoom => await CreateCollaborationRoom(title),
        RoomType.VirtualDataRoom => await CreateVDRRoom(title),
        RoomType.FillingFormsRoom => await CreateFillingFormsRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, "Unsupported room type")
    };
}
