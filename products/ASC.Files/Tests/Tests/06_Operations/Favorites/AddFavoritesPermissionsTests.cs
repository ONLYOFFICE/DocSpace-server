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
/// POST /api/2.0/files/favorites - access control. Favoriting a file only needs Read access to it;
/// it does not require any content-modifying permission on the parent room.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Favorites")]
public class AddFavoritesPermissionsTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    [Fact]
    public async Task AddFavorites_Owner_ReturnsTrue()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest Owner AddFav File.docx", Owner);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorites_UserAddsOwnFile_ReturnsTrue()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest User Own AddFav File.docx", user);

        // Act
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorites_UserWithReadAccessToRoomFile_ReturnsTrue()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest AddFav User Read Room");
        var file = await CreateFile("Autotest AddFav User Read File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        await _filesClient.Authenticate(user);
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorites_GuestWithReadAccessToRoomFile_ReturnsTrue()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest AddFav Guest Room");
        var file = await CreateFile("Autotest AddFav Guest File.docx", room.Id);

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        // Act
        await _filesClient.Authenticate(guest);
        var response = await AddFilesToFavorites(file.Id);

        // Assert
        response.Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorites_Anonymous_Returns401()
    {
        // Arrange
        var file = await CreateFileInMy("Autotest AddFav Anon File.docx", Owner);
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await AddFilesToFavorites(file.Id));

        exception.ErrorCode.Should().Be(401);
    }
}
