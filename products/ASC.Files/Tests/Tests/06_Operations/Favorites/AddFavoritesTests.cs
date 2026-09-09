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

namespace ASC.Files.Tests.Tests._06_Operations.Favorites;

/// <summary>
/// POST /api/2.0/files/favorites - adding files and folders to the Favorites section.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Favorites")]
public class AddFavoritesTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    [Fact]
    public async Task AddFavorites_File_ReturnsTrueAndAppearsInFavorites()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav File.docx", Owner);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFavorites_Folder_ReturnsTrueAndAppearsInFavorites()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest AddFav Folder", Owner);

        // Act
        var response = await AddFoldersToFavorites(folder.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Folders.Any(x => x.Title == folder.Title));
        favorites.Folders.Should().Contain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task AddFavorites_MultipleFiles_AllAppearInFavorites()
    {
        // Arrange
        var file1 = await CreateFileInMy("Autotest AddFav Multi1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest AddFav Multi2.docx", Owner);

        // Act
        var response = await AddFilesToFavorites(file1.Id, file2.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file1.Title) && f.Files.Any(x => x.Title == file2.Title));
        favorites.Files.Should().Contain(f => f.Title == file1.Title);
        favorites.Files.Should().Contain(f => f.Title == file2.Title);
    }

    [Fact]
    public async Task AddFavorites_MultipleFolders_AllAppearInFavorites()
    {
        // Arrange
        var folder1 = await CreateFolderInMy("Autotest AddFav MultiFolderA", Owner);
        var folder2 = await CreateFolderInMy("Autotest AddFav MultiFolderB", Owner);

        // Act
        var response = await AddFoldersToFavorites(folder1.Id, folder2.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Folders.Any(x => x.Title == folder1.Title) && f.Folders.Any(x => x.Title == folder2.Title));
        favorites.Folders.Should().Contain(f => f.Title == folder1.Title);
        favorites.Folders.Should().Contain(f => f.Title == folder2.Title);
    }

    [Fact]
    public async Task AddFavorites_FileAndFolderTogether_BothAppearInFavorites()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav Mixed File.docx", Owner);
        var folder = await CreateFolderInMy("Autotest AddFav Mixed Folder", Owner);

        var request = new BaseBatchRequestDto
        {
            FileIds = [new BaseBatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)]
        };

        // Act
        var response = await AddFavorites(request);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title) && f.Folders.Any(x => x.Title == folder.Title));
        favorites.Files.Should().Contain(f => f.Title == file.Title);
        favorites.Folders.Should().Contain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task AddFavorites_AlreadyFavoritedFile_IsIdempotent()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav Idempotent.docx", Owner);
        await AddFilesToFavorites(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Files.Count(f => f.Title == file.Title).Should().Be(1);
    }

    [Fact]
    public async Task AddFavorites_EmptyBody_ReturnsTrue()
    {
        // Act - sent raw: the generated client drops the Content-Type header together with the body,
        // so a bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PostAsync("api/2.0/files/favorites", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddFavorites_EmptyIdArrays_ReturnsTrueAndNothingAdded()
    {
        // Act
        var response = await AddFavorites(new BaseBatchRequestDto { FileIds = [], FolderIds = [] });

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Total.Should().Be(0);
    }

    [Fact]
    public async Task AddFavorites_FileIdZero_ReturnsTrueAndNothingAdded()
    {
        // Act
        var response = await AddFilesToFavorites(0);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Total.Should().Be(0);
    }

    [Fact]
    public async Task AddFavorites_NonExistentFileId_ReturnsTrueAndNothingAdded()
    {
        // Act
        var response = await AddFilesToFavorites(999999999);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Total.Should().Be(0);
    }

    [Fact]
    public async Task AddFavorites_FolderIdZero_ReturnsTrueAndNothingAdded()
    {
        // Act
        var response = await AddFoldersToFavorites(0);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Total.Should().Be(0);
    }

    [Fact]
    public async Task AddFavorites_NonExistentFolderId_ReturnsTrueAndNothingAdded()
    {
        // Act
        var response = await AddFoldersToFavorites(999999999);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Total.Should().Be(0);
    }

    [Fact]
    public async Task AddFavorites_FileMovedToTrash_ReturnsTrueButDoesNotAppearInFavorites()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav Trash File.docx", Owner);
        await DeleteFileToTrash(file.Id);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await GetFavorites();
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFavorites_FileFromRecentSection_CanBeAddedToFavorites()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav Recent File.docx", Owner);
        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Theory]
    [MemberData(nameof(RoomTypesForFavorites))]
    public async Task AddFavorites_FileFromRoom_CanBeAddedToFavorites(RoomType roomType)
    {
        // Arrange - a FillingFormsRoom refuses non-form uploads, and a .pdf created from the
        // built-in blank template is a genuine ONLYOFFICE form, so it seeds every room type.
        var room = await CreateRoom(roomType, $"Autotest AddFav {roomType} Room");
        var extension = roomType == RoomType.FillingFormsRoom ? "pdf" : "docx";
        var file = await CreateFile($"Autotest AddFav {roomType} File.{extension}", room.Id);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task AddFavorites_FileFromArchivedRoom_CanBeAddedToFavorites()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest AddFav Archived Room");
        var file = await CreateFile("Autotest AddFav Archived File.docx", room.Id);
        await ArchiveRoom(room.Id);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }
}
