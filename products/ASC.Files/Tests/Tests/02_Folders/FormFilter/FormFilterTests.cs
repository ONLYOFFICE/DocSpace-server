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

namespace ASC.Files.Tests.Tests._02_Folders.FormFilter;

/// <summary>
/// <c>GET /files/{folderId}/formfilter</c> - functional coverage: the endpoint always answers with
/// an array (empty when the folder carries no form fields, or does not exist, or was deleted), and
/// never throws for any folder the caller can otherwise reach.
/// </summary>
public class FormFilterTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetFolder_EmptyFolder_ReturnsEmptyArray()
    {
        var room = await CreateCustomRoom("Autotest Room For Form Filter Empty");
        var folder = await CreateFolder("Autotest Empty Folder For Filter", room.Id);

        var result = (await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolder_FolderWithRegularFiles_ReturnsEmptyArray()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder With Uploaded Files", myFolderId);

        await CreateFile("Autotest Uploaded Doc", folder.Id);
        await CreateFile("Autotest Uploaded Doc 2", folder.Id);

        var result = (await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolder_FolderWithManyFiles_ReturnsOk()
    {
        var room = await CreateCustomRoom("Autotest Room For Busy Folder Filter");
        var folder = await CreateFolder("Autotest Busy Folder", room.Id);

        for (var i = 1; i <= 10; i++)
        {
            await CreateFile($"Autotest File {i}", folder.Id);
        }

        var act = async () => await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFolder_NonExistentFolderId_ReturnsEmptyArray()
    {
        var result = (await _foldersApi.GetFolderAsync(999999999, TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolder_DeletedFolder_ReturnsEmptyArray()
    {
        var myFolderId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolder("Autotest Folder For Filter After Delete", myFolderId);

        await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolder(deleteAfter: true, immediately: true), TestContext.Current.CancellationToken);
        await WaitLongOperation();
        await WaitForFolderDeletedAsync(folder.Id);

        var result = (await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFolder_RecentFolder_ReturnsOk()
    {
        var recentFolderId = await GetFolderIdAsync(FolderType.Recent, Owner);

        var act = async () => await _foldersApi.GetFolderAsync(recentFolderId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFolder_FavoritesFolder_ReturnsOk()
    {
        var favoritesFolderId = await GetFolderIdAsync(FolderType.Favorites, Owner);

        var act = async () => await _foldersApi.GetFolderAsync(favoritesFolderId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Polls until the folder is actually gone (deletion is an asynchronous file operation), so the
    /// subsequent formfilter read observes a folder that genuinely no longer exists rather than
    /// racing the delete.
    /// </summary>
    private async Task WaitForFolderDeletedAsync(int folderId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ApiException? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await _foldersApi.GetFolderInfoAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);
            }
            catch (ApiException ex)
            {
                lastException = ex;
                return;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        lastException.Should().NotBeNull($"folder {folderId} should have been deleted within 30 seconds");
    }
}
