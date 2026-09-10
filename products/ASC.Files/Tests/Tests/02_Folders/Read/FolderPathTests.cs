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
public class FolderPathTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFolderPath_RoomSubfolder_ReturnsBreadcrumbEndingWithTargetFolder()
    {
        var room = await CreateCustomRoom("Autotest Room For Path Breadcrumb");
        var folder = await CreateFolder("Autotest Folder For Breadcrumb", room.Id);

        var path = (await _foldersApi.GetFolderPathAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        path.Should().NotBeEmpty();
        var titles = path.Select(e => e.Title).ToList();
        titles.Should().Contain("Autotest Folder For Breadcrumb");
        titles.Should().Contain("Autotest Room For Path Breadcrumb");
        path[^1].Title.Should().Be("Autotest Folder For Breadcrumb");
    }

    [Fact]
    public async Task GetFolderPath_NestedFolder_ElementsAreOrderedFromRootToTarget()
    {
        var room = await CreateCustomRoom("Autotest Room For Nested Path Order");
        var parent = await CreateFolder("Autotest Parent Folder Path Order", room.Id);
        var child = await CreateFolder("Autotest Child Folder Path Order", parent.Id);

        var path = (await _foldersApi.GetFolderPathAsync(child.Id, TestContext.Current.CancellationToken)).Response;

        path.Count.Should().BeGreaterThanOrEqualTo(2);
        var titles = path.Select(e => e.Title).ToList();
        var parentIndex = titles.IndexOf("Autotest Parent Folder Path Order");
        var childIndex = titles.IndexOf("Autotest Child Folder Path Order");
        parentIndex.Should().BeGreaterThanOrEqualTo(0);
        childIndex.Should().BeGreaterThanOrEqualTo(0);
        parentIndex.Should().BeLessThan(childIndex);
    }

    [Fact]
    public async Task GetFolderPath_AnyFolder_EachElementHasNonEmptyTitle()
    {
        var room = await CreateCustomRoom("Autotest Room For Path Titles");
        var folder = await CreateFolder("Autotest Folder Path Titles Check", room.Id);

        var path = (await _foldersApi.GetFolderPathAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        foreach (var element in path)
        {
            element.Title.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task GetFolderPath_Room_ReturnsPathContainingTheRoom()
    {
        var room = await CreateCustomRoom("Autotest Room As Path Target");

        var path = (await _foldersApi.GetFolderPathAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        var titles = path.Select(e => e.Title).ToList();
        titles.Should().Contain("Autotest Room As Path Target");
        path[^1].Title.Should().Be("Autotest Room As Path Target");
    }

    [Fact]
    public async Task GetFolderPath_FolderInMyDocuments_PathIncludesFilesSection()
    {
        var folder = await CreateFolderInMy("Autotest Folder MyDocs Path Check", Owner);

        var path = (await _foldersApi.GetFolderPathAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        var titles = path.Select(e => e.Title).ToList();
        titles.Should().Contain("Autotest Folder MyDocs Path Check");
        titles.Should().Contain("Files");
        titles.IndexOf("Files").Should().BeLessThan(titles.IndexOf("Autotest Folder MyDocs Path Check"));
    }

    [Fact]
    public async Task GetFolderPath_FolderInArchivedRoom_ReturnsOkWithPath()
    {
        var room = await CreateCustomRoom("Autotest Archived Room For Path");
        var folder = await CreateFolder("Autotest Folder In Archived Room Path", room.Id);

        var operation = await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation(operation.Response?.Id);

        var path = (await _foldersApi.GetFolderPathAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        var titles = path.Select(e => e.Title).ToList();
        titles.Should().Contain("Autotest Archived Room For Path");
        titles.Should().Contain("Autotest Folder In Archived Room Path");
        titles.IndexOf("Autotest Archived Room For Path").Should().BeLessThan(titles.IndexOf("Autotest Folder In Archived Room Path"));
    }

    [Fact]
    public async Task GetFolderPath_TrashVirtualFolder_ReturnsPathContainingTrash()
    {
        var trashFolder = (await _foldersApi.GetTrashFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var path = (await _foldersApi.GetFolderPathAsync(trashFolder.Id, TestContext.Current.CancellationToken)).Response;

        path.Should().NotBeEmpty();
        path.Select(e => e.Title).Should().Contain(trashFolder.Title);
    }

    [Fact]
    public async Task GetFolderPath_FavoritesVirtualFolder_ReturnsPathContainingFavorites()
    {
        var favoritesFolder = (await _foldersApi.GetFavoritesFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var path = (await _foldersApi.GetFolderPathAsync(favoritesFolder.Id, TestContext.Current.CancellationToken)).Response;

        path.Should().NotBeEmpty();
        path.Select(e => e.Title).Should().Contain(favoritesFolder.Title);
    }

    [Fact]
    public async Task GetFolderPath_RecentVirtualFolder_ReturnsPathContainingRecent()
    {
        var recentFolder = (await _foldersApi.GetRecentFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current;

        var path = (await _foldersApi.GetFolderPathAsync(recentFolder.Id, TestContext.Current.CancellationToken)).Response;

        path.Should().NotBeEmpty();
        path.Select(e => e.Title).Should().Contain(recentFolder.Title);
    }

    [Fact]
    [Trait("Bug", "81483")]
    public async Task GetFolderPath_NonExistentFolderId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderPathAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
