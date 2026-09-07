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
/// GET /api/2.0/files/@favorites - pagination (<c>count</c>, <c>startIndex</c>), sorting and the
/// <c>filterValue</c> search term. <c>filterValue</c> is served from the search index, which is
/// written asynchronously after the favorite-toggling request returns, so every read polls.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "Favorites")]
public class FavoritesQueryTests(
    AspireAppFixture fixture)
    : FavoritesTestBase(fixture)
{
    [Fact]
    public async Task GetFavorites_CountOne_ReturnsExactlyOneFileWithCorrectStartIndex()
    {
        // Arrange
        var myFolderId = await GetUserFolderIdAsync(Owner);
        foreach (var title in new[] { "Autotest Favorites Page File A.docx", "Autotest Favorites Page File B.docx" })
        {
            var file = await CreateFile(title, myFolderId);
            await ToggleFavorite(file.Id);
        }

        await PollFavorites(f => f.Files.Count >= 2);

        // Act
        var favorites = await GetFavorites(count: 1, startIndex: 0);

        // Assert
        favorites.Files.Should().HaveCount(1);
        favorites.Count.Should().Be(1);
        favorites.StartIndex.Should().Be(0);
    }

    [Fact]
    public async Task GetFavorites_SortOrderDescending_ReturnsFilesInReverseAlphabeticalOrder()
    {
        // Arrange
        var myFolderId = await GetUserFolderIdAsync(Owner);
        string[] titles = ["Autotest Favorites Sort AAA.docx", "Autotest Favorites Sort MMM.docx", "Autotest Favorites Sort ZZZ.docx"];
        foreach (var title in titles)
        {
            var file = await CreateFile(title, myFolderId);
            await ToggleFavorite(file.Id);
        }

        // Act
        var favorites = await PollFavorites(f => titles.All(t => f.Files.Any(x => x.Title == t)));
        var favoritesSorted = await GetFavorites(sortBy: "AZ", sortOrder: SortOrder.Descending);

        // Assert
        var actualTitles = favoritesSorted.Files.Select(f => f.Title).ToList();
        var zzzIndex = actualTitles.IndexOf(titles[2]);
        var mmmIndex = actualTitles.IndexOf(titles[1]);
        var aaaIndex = actualTitles.IndexOf(titles[0]);

        zzzIndex.Should().BeGreaterThanOrEqualTo(0);
        mmmIndex.Should().BeGreaterThanOrEqualTo(0);
        aaaIndex.Should().BeGreaterThanOrEqualTo(0);
        zzzIndex.Should().BeLessThan(mmmIndex);
        mmmIndex.Should().BeLessThan(aaaIndex);

        favorites.Files.Should().NotBeEmpty(); // arrange-poll observed the write completed
    }

    [Fact]
    public async Task GetFavorites_FilterValue_ReturnsOnlyMatchingFiles()
    {
        // Arrange
        var matchFile = await CreateFileInMy("Autotest Favorites FilterVal UNIQUE.docx", Owner);
        await ToggleFavorite(matchFile.Id);

        var otherFile = await CreateFileInMy("Autotest Favorites FilterVal Other.docx", Owner);
        await ToggleFavorite(otherFile.Id);

        // Act
        var favorites = await PollFavorites(f => f.Files.Any(x => x.Title == matchFile.Title));
        // A filterValue search is served from the index, which is written asynchronously.
        var filtered = await PollFavorites(f => f.Files.Any(x => x.Title == matchFile.Title), filterValue: "UNIQUE");

        // Assert
        favorites.Files.Should().NotBeEmpty(); // arrange-poll observed the write completed
        filtered.Files.Should().Contain(f => f.Title == matchFile.Title);
        filtered.Files.Should().NotContain(f => f.Title == otherFile.Title);
    }

    [Fact]
    public async Task GetFavorites_FilterValueWithNoMatch_ReturnsEmptyFilesArray()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites FilterVal NoMatch.docx", Owner);
        await ToggleFavorite(file.Id);

        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        var filtered = await GetFavorites(filterValue: "XQZNONEXISTENTXQZ");

        // Assert
        filtered.Files.Should().BeEmpty();
        filtered.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetFavorites_CountZero_ReturnsBadRequest()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await GetFavorites(count: 0));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetFavorites_StartIndexBeyondTotal_ReturnsEmptyFilesAndFoldersArrays()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Favorites StartIndex Beyond.docx", Owner);
        await ToggleFavorite(file.Id);

        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        var favorites = await GetFavorites(startIndex: 999999);

        // Assert
        favorites.Files.Should().BeEmpty();
        favorites.Folders.Should().BeEmpty();
        favorites.Count.Should().Be(0);
    }
}
