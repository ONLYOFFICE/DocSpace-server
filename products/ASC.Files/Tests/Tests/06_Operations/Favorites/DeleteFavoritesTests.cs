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
/// DELETE /api/2.0/files/favorites - removing files and folders from the Favorites section.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Favorites")]
public class DeleteFavoritesTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    [Fact]
    public async Task DeleteFavorites_File_ReturnsTrueAndFileNoLongerAppears()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest DelFav File.docx", Owner);
        await AddFilesToFavorites(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        var response = await RemoveFilesFromFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file.Title));
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task DeleteFavorites_Folder_ReturnsTrueAndFolderNoLongerAppears()
    {
        // Arrange
        var folder = await CreateFolderInMy("Autotest DelFav Folder", Owner);
        await AddFoldersToFavorites(folder.Id);
        await PollFavorites(f => f.Folders.Any(x => x.Title == folder.Title));

        // Act
        var response = await RemoveFoldersFromFavorites(folder.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Folders.All(x => x.Title != folder.Title));
        favorites.Folders.Should().NotContain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task DeleteFavorites_MultipleFiles_AllRemoved()
    {
        // Arrange
        var file1 = await CreateFileInMy("Autotest DelFav Multi1.docx", Owner);
        var file2 = await CreateFileInMy("Autotest DelFav Multi2.docx", Owner);
        await AddFilesToFavorites(file1.Id, file2.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file1.Title) && f.Files.Any(x => x.Title == file2.Title));

        // Act
        var response = await RemoveFilesFromFavorites(file1.Id, file2.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file1.Title) && f.Files.All(x => x.Title != file2.Title));
        favorites.Files.Should().NotContain(f => f.Title == file1.Title);
        favorites.Files.Should().NotContain(f => f.Title == file2.Title);
    }

    [Fact]
    public async Task DeleteFavorites_MultipleFolders_AllRemoved()
    {
        // Arrange
        var folder1 = await CreateFolderInMy("Autotest DelFav MultiFolderA", Owner);
        var folder2 = await CreateFolderInMy("Autotest DelFav MultiFolderB", Owner);
        await AddFoldersToFavorites(folder1.Id, folder2.Id);
        await PollFavorites(f => f.Folders.Any(x => x.Title == folder1.Title) && f.Folders.Any(x => x.Title == folder2.Title));

        // Act
        var response = await RemoveFoldersFromFavorites(folder1.Id, folder2.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Folders.All(x => x.Title != folder1.Title) && f.Folders.All(x => x.Title != folder2.Title));
        favorites.Folders.Should().NotContain(f => f.Title == folder1.Title);
        favorites.Folders.Should().NotContain(f => f.Title == folder2.Title);
    }

    [Fact]
    public async Task DeleteFavorites_FileAndFolderTogether_BothRemoved()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest DelFav Mixed File.docx", Owner);
        var folder = await CreateFolderInMy("Autotest DelFav Mixed Folder", Owner);
        await AddFilesToFavorites(file.Id);
        await AddFoldersToFavorites(folder.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title) && f.Folders.Any(x => x.Title == folder.Title));

        var request = new BaseBatchRequestDto
        {
            FileIds = [new BaseBatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)]
        };

        // Act
        var response = await RemoveFavorites(request);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file.Title) && f.Folders.All(x => x.Title != folder.Title));
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
        favorites.Folders.Should().NotContain(f => f.Title == folder.Title);
    }

    [Fact]
    public async Task DeleteFavorites_SourceFile_StillAccessibleAfterRemoval()
    {
        // Arrange
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest DelFav Source File.docx", myFolderId);
        await AddFilesToFavorites(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        await RemoveFilesFromFavorites(file.Id);

        // Assert
        var content = (await _foldersApi.GetFolderByFolderIdAsync(myFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        content.Files.Should().Contain(f => f.Title == file.Title);
    }

    [Fact]
    public async Task DeleteFavorites_FileNotInFavorites_IsIdempotentAndReturnsTrue()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest DelFav Idempotent File.docx", Owner);

        // Act
        var response = await RemoveFilesFromFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFavorites_EmptyBody_ReturnsTrue()
    {
        // Act
        var response = await RemoveFavorites(null);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFavorites_EmptyIdArrays_ReturnsTrue()
    {
        // Act
        var response = await RemoveFavorites(new BaseBatchRequestDto { FileIds = [], FolderIds = [] });

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFavorites_NonExistentFileId_ReturnsTrue()
    {
        // Act
        var response = await RemoveFilesFromFavorites(999999999);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFavorites_NonExistentFolderId_ReturnsTrue()
    {
        // Act
        var response = await RemoveFoldersFromFavorites(999999999);

        // Assert
        response.Should().BeTrue();
    }
}
