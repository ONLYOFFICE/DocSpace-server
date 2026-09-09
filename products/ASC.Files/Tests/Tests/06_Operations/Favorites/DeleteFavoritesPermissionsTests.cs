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
/// DELETE /api/2.0/files/favorites - access control.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Favorites")]
public class DeleteFavoritesPermissionsTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    [Fact]
    public async Task DeleteFavorites_Anonymous_Returns401()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest DelFav Anon File.docx", Owner);
        await AddFilesToFavorites(file.Id);
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await RemoveFilesFromFavorites(file.Id));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteFavorites_Owner_RemovesFile()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest DelFav Owner File.docx", Owner);
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
    public async Task DeleteFavorites_UserRemovesOwnFile_RemovesFile()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest DelFav User File.docx", user);
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
    public async Task DeleteFavorites_GuestRemovesRoomFile_RemovesFile()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest DelFav Guest Room");
        var file = await CreateFile("Autotest DelFav Guest File.docx", room.Id);

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);
        await AddFilesToFavorites(file.Id);
        await PollFavorites(f => f.Files.Any(x => x.Title == file.Title));

        // Act
        var response = await RemoveFilesFromFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
        var favorites = await PollFavorites(f => f.Files.All(x => x.Title != file.Title));
        favorites.Files.Should().NotContain(f => f.Title == file.Title);
    }
}
