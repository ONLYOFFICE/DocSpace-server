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
/// <c>PUT /files/folder/{folderId}</c>: renaming into and out of <c>.ai</c> in a room root moves the
/// folder in and out of <c>FolderType.Ai</c>.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AppsFolder")]
public class AppsFolderRenameTests(
    AspireAppFixture fixture)
    : AppsFolderTestBase(fixture)
{
    [Fact]
    public async Task RenameFolderToApps_WhenAppsFolderExists_Rejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Rename Conflict");
        await CreateAppsFolder(room.Id);
        var folder = await CreateFolder("Autotest Rename Me", room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(AppsTitle), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("already contains the .ai folder");
        (await GetFolderTitles(room.Id)).Should().Contain("Autotest Rename Me");
    }

    [Fact]
    public async Task RenameFolderToApps_InRoomRoot_BecomesAppsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Rename Promote");
        var folder = await CreateFolder("Autotest Rename Me", room.Id);

        // Act
        var renamed = (await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(AppsTitle), TestContext.Current.CancellationToken)).Response;

        // Assert
        renamed.Title.Should().Be(AppsTitle);
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task RenameAppsFolder_ToOtherTitle_BecomesRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Rename Demote");
        var apps = await CreateAppsFolder(room.Id);

        // Act
        await _foldersApi.RenameFolderAsync(apps.Id, new CreateFolder("Autotest Former Apps"), TestContext.Current.CancellationToken);

        // Assert
        await AssertAppsFolderAbsent(room.Id);
    }

    [Fact]
    public async Task RenameAppsFolder_AwayAndBack_BecomesAppsFolderAgain()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Rename Back");
        var apps = await CreateAppsFolder(room.Id);
        await _foldersApi.RenameFolderAsync(apps.Id, new CreateFolder("Autotest Former Apps"), TestContext.Current.CancellationToken);

        // Act
        await _foldersApi.RenameFolderAsync(apps.Id, new CreateFolder(AppsTitle), TestContext.Current.CancellationToken);

        // Assert
        await AssertAppsFolderPresent(room.Id);
    }

    [Fact]
    public async Task RenameFolderToApps_InRoomSubfolder_StaysRegularFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateSmartRoom("Autotest Apps Rename Nested");
        var subfolder = await CreateFolder("Autotest Subfolder", room.Id);
        var folder = await CreateFolder("Autotest Rename Me", subfolder.Id);

        // Act
        await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder(AppsTitle), TestContext.Current.CancellationToken);

        // Assert
        await AssertAppsFolderAbsent(subfolder.Id);
        await AssertAppsFolderAbsent(room.Id);
    }
}
