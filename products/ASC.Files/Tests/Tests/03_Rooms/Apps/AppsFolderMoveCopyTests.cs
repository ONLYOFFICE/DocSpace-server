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
/// <c>PUT /files/fileops/move</c> and <c>PUT /files/fileops/copy</c>: the type follows the folder's
/// new place — <c>Apps</c> in a room root, a regular folder anywhere else.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AppsFolder")]
public class AppsFolderMoveCopyTests(
    AspireAppFixture fixture)
    : AppsFolderTestBase(fixture)
{
    [Fact]
    public async Task MoveAppsFolder_IntoRoomSubfolder_BecomesRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Move Down");
        var apps = await CreateAppsFolder(room.Id);
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);

        // Act
        await MoveAndWait(apps.Id, subfolder.Id);

        // Assert
        (await GetFolderTitles(subfolder.Id)).Should().Contain(AppsTitle);
        await AssertAppsFolderAbsent(subfolder.Id);
        await AssertAppsFolderAbsent(room.Id);
    }

    [Fact]
    public async Task MoveFolderNamedApps_IntoRoomRoot_BecomesAppsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Move Up");
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);
        var apps = await CreateAppsFolder(subfolder.Id);

        // Act
        await MoveAndWait(apps.Id, room.Id);

        // Assert
        (await GetFolderTitles(room.Id)).Should().Contain(AppsTitle);
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task MoveAppsFolder_ToAnotherRoomRoot_StaysAppsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateSmartRoom("Autotest Apps Move Source");
        var target = await CreateSmartRoom("Autotest Apps Move Target", RoomType.PublicRoom);
        var apps = await CreateAppsFolder(source.Id);

        // Act
        await MoveAndWait(apps.Id, target.Id);

        // Assert
        await AssertAppsFolderPresent(target.Id);
        await AssertAppsFolderAbsent(source.Id);
    }

    [Fact]
    public async Task MoveFolderNamedApps_KeepingBothIntoRootWithAppsFolder_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Move Keep Both");
        var original = await CreateAppsFolder(room.Id);
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);
        var incoming = await CreateAppsFolder(subfolder.Id);

        // Act
        await MoveAndWait(incoming.Id, room.Id, FileConflictResolveType.Duplicate);

        // Assert
        (await GetFolderTitles(room.Id)).Count(t => t == AppsTitle).Should().Be(2);
        await AssertAppsFolderPresent(room.Id);

        await _foldersApi.RenameFolderAsync(original.Id, new CreateFolder("Autotest Former Apps"), TestContext.Current.CancellationToken);
        await AssertAppsFolderAbsent(room.Id);
    }

    [Fact]
    public async Task MoveAppsFolder_ToFillingFormsRoomRoot_BecomesRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateSmartRoom("Autotest Apps Move To Forms Source");
        var target = await CreateFillingFormsRoom("Autotest Apps Move To Forms Target");
        var apps = await CreateAppsFolder(source.Id);

        // Act
        await MoveAndWait(apps.Id, target.Id);

        // Assert
        (await GetFolderTitles(target.Id)).Should().Contain(AppsTitle);
        await AssertAppsFolderAbsent(target.Id);
    }

    [Fact]
    public async Task CopyAppsFolder_ToAnotherRoomRoot_CopyIsAppsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var source = await CreateSmartRoom("Autotest Apps Copy Source");
        var target = await CreateSmartRoom("Autotest Apps Copy Target", RoomType.EditingRoom);
        var apps = await CreateAppsFolder(source.Id);

        // Act
        await CopyAndWait(apps.Id, target.Id);

        // Assert
        await AssertAppsFolderPresent(target.Id);
        await AssertAppsFolderPresent(source.Id);
    }

    [Fact]
    public async Task DuplicateAppsFolder_InRoomRoot_DuplicateIsRenamedAndRegular()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Duplicate Folder");
        var apps = await CreateAppsFolder(room.Id);

        // Act
        await DuplicateAndWait(apps.Id);

        // Assert
        (await GetFolderTitles(room.Id)).Should().HaveCount(2).And.Contain(AppsTitle).And.Contain(t => t != AppsTitle && t.StartsWith(AppsTitle, StringComparison.Ordinal));
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task CopyAppsFolder_IntoRoomSubfolder_CopyIsRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Copy Down");
        var apps = await CreateAppsFolder(room.Id);
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);

        // Act
        await CopyAndWait(apps.Id, subfolder.Id);

        // Assert
        (await GetFolderTitles(subfolder.Id)).Should().Contain(AppsTitle);
        await AssertAppsFolderAbsent(subfolder.Id);
    }
}
