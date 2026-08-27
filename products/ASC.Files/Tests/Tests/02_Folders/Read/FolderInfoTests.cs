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

namespace ASC.Files.Tests.Tests._02_Folders.Read;

[Trait("Category", "CRUD")]
[Trait("Feature", "Folders")]
public class FolderInfoTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFolderInfo_RoomSubfolder_ReturnsCorrectIdAndTitle()
    {
        var room = await CreateCustomRoom("Autotest Room For Folder Info");
        var folder = await CreateFolder("Autotest Folder For Info", room.Id);

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(folder.Id);
        info.Title.Should().Be("Autotest Folder For Info");
    }

    [Fact]
    public async Task GetFolderInfo_RoomSubfolder_ParentIdPointsToRoom()
    {
        var room = await CreateCustomRoom("Autotest Room For Parent Check");
        var folder = await CreateFolder("Autotest Folder Parent Check", room.Id);

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.ParentId.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetFolderInfo_FolderWithContent_FilesCountAndFoldersCountReflectContents()
    {
        var room = await CreateCustomRoom("Autotest Room For Count Check");
        var folder = await CreateFolder("Autotest Folder Count Check", room.Id);

        await CreateFile("Autotest File 1", folder.Id);
        await CreateFile("Autotest File 2", folder.Id);
        await CreateFolder("Autotest Subfolder", folder.Id);

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.FilesCount.Should().Be(2);
        info.FoldersCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFolderInfo_EmptyFolder_ReturnsZeroCounts()
    {
        var room = await CreateCustomRoom("Autotest Room For Empty Folder Info");
        var folder = await CreateFolder("Autotest Empty Folder Info", room.Id);

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.FilesCount.Should().Be(0);
        info.FoldersCount.Should().Be(0);
    }

    [Fact]
    public async Task GetFolderInfo_Room_ReturnsCorrectRoomType()
    {
        var room = await CreateCustomRoom("Autotest Room For Type Check");

        var info = (await _foldersApi.GetFolderInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        info.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    [Trait("Bug", "81459")]
    public async Task GetFolderInfo_NonExistentFolderId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    [Trait("Bug", "81459")]
    public async Task GetFolderInfo_DeletedFolder_Returns404()
    {
        var folder = await CreateFolderInMy("Autotest Folder For Info After Delete", Owner);

        var operation = (await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder(deleteAfter: true, immediately: true), TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(operation.FirstOrDefault()?.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFolderInfo_MyDocumentsVirtualFolder_ReturnsOk()
    {
        var myFolder = (await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var info = (await _foldersApi.GetFolderInfoAsync(myFolder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(myFolder.Id);
    }

    [Fact]
    public async Task GetFolderInfo_TrashVirtualFolder_ReturnsOk()
    {
        var trashFolder = (await _foldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var info = (await _foldersApi.GetFolderInfoAsync(trashFolder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(trashFolder.Id);
    }

    [Fact]
    public async Task GetFolderInfo_RecentVirtualFolder_ReturnsOk()
    {
        var recentFolder = (await _foldersApi.GetRecentFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var info = (await _foldersApi.GetFolderInfoAsync(recentFolder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(recentFolder.Id);
    }

    [Fact]
    public async Task GetFolderInfo_FavoritesVirtualFolder_ReturnsOk()
    {
        var favoritesFolder = (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var info = (await _foldersApi.GetFolderInfoAsync(favoritesFolder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(favoritesFolder.Id);
    }

    [Fact]
    public async Task GetFolderInfo_FolderWithManyFiles_ReturnsCorrectFilesCount()
    {
        var room = await CreateCustomRoom("Autotest Room For Many Files Info");
        var folder = await CreateFolder("Autotest Folder Many Files", room.Id);

        for (var i = 1; i <= 10; i++)
        {
            await CreateFile($"Autotest File {i}", folder.Id);
        }

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.FilesCount.Should().Be(10);
    }

    [Fact]
    public async Task GetFolderInfo_FolderWithUploadedFiles_ReturnsCorrectFields()
    {
        var folder = await CreateFolderInMy("Autotest Folder Uploaded Files Info", Owner);

        await CreateFile("Autotest Uploaded Doc 1", folder.Id);
        await CreateFile("Autotest Uploaded Doc 2", folder.Id);

        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.FilesCount.Should().Be(2);
        info.CreatedBy.Should().NotBeNull();
    }
}
