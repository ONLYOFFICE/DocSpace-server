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
/// GET /api/2.0/files/@favorites filtered by <see cref="FilterType"/> - which item kinds a filter
/// selects, and how each file kind is classified.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "Favorites")]
public class FavoritesFilterTypeTests(
    AspireAppFixture fixture)
    : FavoritesTestBase(fixture)
{
    [Fact]
    public async Task GetFavorites_TextFileFavorited_AppearsInResponse()
    {
        // Arrange
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateTextFile("Autotest Favorites Text File.txt", myFolderId);
        await ToggleFavorite(file.Id);

        // Act
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Assert
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterType_DocumentsOnly_ReturnsDocxFile()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites Document.docx", Owner);
        await ToggleFavorite(file.Id);

        // Act
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title), FilterType.DocumentsOnly);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == file.Title);
        favorites.Folders.Should().BeEmpty();
    }

    [Trait("Bug", "81481")]
    [Fact]
    public async Task GetFavorites_HtmlFileFavorited_AppearsAfterConversionQueueFinishes()
    {
        // Arrange - previously the file was counted in response.total but missing from response.files
        // while still in the conversion queue; asserts the fixed behaviour, polled rather than delayed.
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateHtmlFile("Autotest Favorites HTML File.html", myFolderId);
        await ToggleFavorite(file.Id);

        // Act
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Assert
        favorites.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterType_FilesOnly_ReturnsAllFavoritedFileTypes()
    {
        // Arrange
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var txtFile = await CreateTextFile("Autotest Favorites Mixed Text.txt", myFolderId);
        await ToggleFavorite(txtFile.Id);

        var docxFile = await CreateFileInMy("Autotest Favorites Mixed Doc.docx", Owner);
        await ToggleFavorite(docxFile.Id);

        // Act
        var favorites = await PollFavorites(
            f => f.Files.Any(x => x.Title == txtFile.Title) && f.Files.Any(x => x.Title == docxFile.Title),
            FilterType.FilesOnly);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == txtFile.Title);
        favorites.Files.Should().Contain(f => f.Title == docxFile.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterType_DocumentsOnly_IncludesTxtFiles()
    {
        // Arrange - .txt opens in the Document Editor, so it is classified as a document
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var docxFile = await CreateFileInMy("Autotest Favorites DocOnly Doc.docx", Owner);
        await ToggleFavorite(docxFile.Id);

        var txtFile = await CreateTextFile("Autotest Favorites DocOnly Text.txt", myFolderId);
        await ToggleFavorite(txtFile.Id);

        // Act
        var favorites = await PollFavorites(
            f => f.Files.Any(x => x.Title == docxFile.Title) && f.Files.Any(x => x.Title == txtFile.Title),
            FilterType.DocumentsOnly);

        // Assert
        favorites.Files.Should().Contain(f => f.Title == docxFile.Title);
        favorites.Files.Should().Contain(f => f.Title == txtFile.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterType_FoldersOnly_ReturnsOnlyFolders()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites FolderOnly File.docx", Owner);
        await ToggleFavorite(file.Id);

        var folder = await CreateFolderInMy("Autotest Favorites FolderOnly Folder", Owner);
        await AddFoldersToFavorites(folder.Id);

        // Act
        var favorites = await PollFavorites(f => f.Folders.Any(x => x.Title == folder.Title), FilterType.FoldersOnly);

        // Assert
        favorites.Files.Should().BeEmpty();
        favorites.Folders.Should().Contain(f => f.Title == folder.Title);
    }
}
