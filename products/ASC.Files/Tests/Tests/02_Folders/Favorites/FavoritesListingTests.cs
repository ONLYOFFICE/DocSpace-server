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

namespace ASC.Files.Tests.Tests._02_Folders.Favorites;

/// <summary>
/// GET /api/2.0/files/@favorites - basic listing behaviour: metadata, the <c>isFavorite</c> flag,
/// adding/removing files and folders, and how deleted or archived content is reflected.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "Favorites")]
public class FavoritesListingTests(
    AspireAppFixture fixture)
    : FavoritesTestBase(fixture)
{
    [Fact]
    public async Task GetFavorites_Owner_ReturnsMetadataConsistentWithFilesAndFolders()
    {
        // Act
        var favorites = await GetFavorites();

        // Assert
        favorites.Current.Id.Should().NotBe(0);
        favorites.Current.Title.Should().NotBeNullOrEmpty();
        favorites.Count.Should().Be(favorites.Files.Count + favorites.Folders.Count);
    }

    [Fact]
    public async Task GetFavorites_Empty_ReturnsNoFilesNoFoldersAndTotalZero()
    {
        // Act
        var favorites = await GetFavorites();

        // Assert
        favorites.Files.Should().BeEmpty();
        favorites.Folders.Should().BeEmpty();
        favorites.Total.Should().Be(0);
        favorites.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFavorites_FileAdded_HasIsFavoriteTrue()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites isFavorite Check.docx", Owner);
        await ToggleFavorite(file.Id);

        // Act
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Assert
        var favoriteFile = favorites.Files.Should().ContainSingle(f => f.Title == file.Title).Subject;
        favoriteFile.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavorites_FolderAdded_AppearsInFoldersWithIsFavoriteTrue()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest Favorites Folder Target", Owner);
        await AddFoldersToFavorites(folder.Id);

        // Act
        var favorites = await PollFavorites(f => f.Folders.Any(x => x.Title == folder.Title));

        // Assert
        var favoriteFolder = favorites.Folders.Should().ContainSingle(f => f.Title == folder.Title).Subject;
        favoriteFolder.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavorites_FolderRemovedViaDeleteFavorites_DoesNotAppear()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest Favorites Remove Folder", Owner);
        await AddFoldersToFavorites(folder.Id);
        await PollFavorites(f => f.Folders.Any(x => x.Title == folder.Title));

        // Act
        await RemoveFoldersFromFavorites(folder.Id);
        var favorites = await PollFavorites(f => f.Folders.All(x => x.Title != folder.Title));

        // Assert
        favorites.Folders.Should().NotContain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task GetFavorites_FileRemovedFromFavorites_DoesNotAppear()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites Removed Doc.docx", Owner);
        await ToggleFavorite(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        await ToggleFavorite(file.Id, false);
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file.Title));

        // Assert
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetFavorites_FileDeletedToTrash_DoesNotAppear()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites Deleted File.docx", Owner);
        await ToggleFavorite(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        await DeleteFileToTrash(file.Id);
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file.Title));

        // Assert
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetFavorites_FileFromRoom_ShowsOriginRoomTitle()
    {
        // Arrange - FolderContentDtoInteger.Files is typed List<FileEntryBaseDto>, which does not
        // carry originRoomTitle, so this reads the raw response (see FavoritesTestBase).
        var room = await CreateCustomRoom("Autotest Favorites Origin Room");
        var file = await CreateFile("Autotest Favorites Room File.docx", room.Id);
        await ToggleFavorite(file.Id);

        // Act
        var deadline = DateTime.UtcNow.AddSeconds(10);
        List<RawFavoriteFile> rawFiles;
        while (true)
        {
            rawFiles = await GetFavoritesFilesRawAsync();
            if (rawFiles.Any(f => f.Title == file.Title) || DateTime.UtcNow >= deadline)
            {
                break;
            }
            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        // Assert
        var favoriteFile = rawFiles.Should().ContainSingle(f => f.Title == file.Title).Subject;
        favoriteFile.OriginRoomTitle.Should().Be("Autotest Favorites Origin Room");
        favoriteFile.IsFavorite.Should().BeTrue();
    }
}
