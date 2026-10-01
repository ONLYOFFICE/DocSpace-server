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

namespace ASC.Files.Tests.Tests._03_Rooms.Apps;

/// <summary>
/// The <c>Apps</c> folder is handled like a regular room folder: room members who may delete or move
/// a regular folder may do the same with <c>.ai</c>.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AppsFolder")]
public class AppsFolderPermissionsTests(
    AspireAppFixture fixture)
    : AppsFolderTestBase(fixture)
{
    [Fact]
    public async Task DeleteAppsFolder_ContentCreatorOwnFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Perm Delete Own");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var apps = await CreateAppsFolder(room.Id);

        // Act
        await DeleteToTrashAndWait(apps.Id);

        // Assert
        (await GetFolderTitles(room.Id)).Should().NotContain(AppsTitle);
    }

    [Fact]
    public async Task DeleteAppsFolder_RoomManagerOthersFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Perm Delete Manager");
        var apps = await CreateAppsFolder(room.Id);
        var manager = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        await _filesClient.Authenticate(manager);

        // Act
        await DeleteToTrashAndWait(apps.Id);

        // Assert
        (await GetFolderTitles(room.Id)).Should().NotContain(AppsTitle);
    }

    [Fact]
    public async Task MoveAppsFolder_ContentCreatorOwnFolder_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Perm Move Own");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var apps = await CreateAppsFolder(room.Id);
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);

        // Act
        await MoveAndWait(apps.Id, subfolder.Id);

        // Assert
        (await GetFolderTitles(subfolder.Id)).Should().Contain(AppsTitle);
        (await GetFolderTitles(room.Id)).Should().NotContain(AppsTitle);
    }
}
