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
/// <c>POST /files/folder/{folderId}</c> with the title <c>.ai</c>: where the folder becomes the
/// room's single <c>Apps</c> folder and where it stays an ordinary folder.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AppsFolder")]
public class AppsFolderCreateTests(
    AspireAppFixture fixture)
    : AppsFolderTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(SmartRoomTypes))]
    public async Task CreateAppsFolder_SecondInRoomRoot_Rejected(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Second", roomType);
        await CreateAppsFolder(room.Id);

        // Act / Assert
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task CreateAppsFolder_InFillingFormsRoomRoot_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateFillingFormsRoom("Autotest Apps FillingForms");
        await CreateAppsFolder(room.Id);

        // Act / Assert
        await AssertAppsFolderAbsent(room.Id);
    }

    [Fact]
    public async Task CreateAppsFolder_InRoomSubfolder_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Subfolder");
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);
        await CreateAppsFolder(subfolder.Id);

        // Act / Assert
        await AssertAppsFolderAbsent(subfolder.Id);
        await AssertAppsFolderAbsent(room.Id);
    }

    [Fact]
    public async Task CreateAppsFolder_InMyDocuments_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        await CreateAppsFolder(myId);

        // Act / Assert
        await AssertAppsFolderAbsent(myId);
    }

    [Fact]
    public async Task CreateAppsFolder_InAnotherRoom_Allowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var first = await CreateSmartRoom("Autotest Apps First Room");
        var second = await CreateSmartRoom("Autotest Apps Second Room");
        await CreateAppsFolder(first.Id);

        // Act / Assert
        await AssertAppsFolderAbsent(second.Id);
    }

    [Fact]
    public async Task CreateFolder_TitleInDifferentCase_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Case");
        await CreateFolder(".Ai", room.Id);

        // Act / Assert
        await AssertAppsFolderAbsent(room.Id);
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task CreateFolder_TitleWithSurroundingSpaces_BecomesAppsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Spaces");
        var created = await CreateFolder("  .ai  ", room.Id);

        // Act / Assert
        created.Title.Should().Be(AppsTitle);
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task CreateFolder_OtherTitleNextToAppsFolder_Allowed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Neighbour");
        await CreateAppsFolder(room.Id);

        // Act
        var neighbour = await CreateFolder("Autotest Neighbour", room.Id);

        // Assert
        neighbour.Title.Should().Be("Autotest Neighbour");
        (await GetFolderTitles(room.Id)).Should().BeEquivalentTo([AppsTitle, "Autotest Neighbour"]);
    }
}
