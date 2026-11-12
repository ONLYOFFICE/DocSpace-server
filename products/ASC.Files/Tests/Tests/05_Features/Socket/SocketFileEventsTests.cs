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

namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// File changes reach a connected client of the owner as <c>s:modify-folder</c>, in the folder room and in the rooms of
/// the virtual sections.
/// </summary>
[Trait("Category", "Socket")]
public class SocketFileEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    [Fact]
    public async Task CreateFile_ReachesTheFolderRoom_WithTheFileData()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        var file = await CreateFile("Socket Create.docx", myId);

        var modified = await WaitForModified(socket, "create", "file", file.Id);
        using var data = DataOf(modified);
        data.RootElement.GetProperty("id").GetInt32().Should().Be(file.Id);
        data.RootElement.GetProperty("title").GetString().Should().Be(file.Title);
    }

    [Fact]
    public async Task RenameFile_ReachesTheFolderRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Socket Rename.docx", myId);
        await using var socket = await ConnectAndWatch(Owner, myId);

        await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest { Title = "Socket Renamed.docx" }, TestContext.Current.CancellationToken);

        var modified = await WaitForModified(socket, "update", "file", file.Id);
        using var data = DataOf(modified);
        data.RootElement.GetProperty("title").GetString().Should().Be("Socket Renamed.docx");
    }

    [Fact]
    public async Task DeleteFile_ReachesTheFolderRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Socket Delete.docx", myId);
        await using var socket = await ConnectAndWatch(Owner, myId);

        await _filesApi.DeleteFileAsync(file.Id, new DeleteFileRequest { Immediately = true }, true, TestContext.Current.CancellationToken);

        await WaitForModified(socket, "delete", "file", file.Id);
    }

    [Fact]
    public async Task AddFileToFavorites_ReachesTheFavoritesRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var favoritesId = await GetFolderIdAsync(FolderType.Favorites, Owner);
        var file = await CreateFile("Socket Favorite.docx", myId);
        await using var socket = await ConnectAndWatch(Owner, favoritesId);

        await _filesApi.ToggleFileFavoriteAsync(file.Id, true, TestContext.Current.CancellationToken);

        await WaitForModified(socket, "create", "file", file.Id);
    }

    [Fact]
    public async Task AddFileToRecent_ReachesTheRecentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var recentId = await GetFolderIdAsync(FolderType.Recent, Owner);
        var file = await CreateFile("Socket Recent.docx", myId);
        await using var socket = await ConnectAndWatch(Owner, recentId);

        await _filesApi.AddFileToRecentAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken);

        await WaitForModified(socket, "create", "file", file.Id);
    }

    [Fact]
    public async Task DropSharedFileFromList_ReachesTheRecentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        var file = await CreateFile("Socket Shared File.docx", await GetUserFolderIdAsync(Owner));
        await _sharingApi.SetFileSecurityInfoAsync(
            file.Id,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = member.Id, Access = FileShare.Read }], Notify = false },
            TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(member);
        await using var socket = await ConnectAndWatch(member, await GetFolderIdAsync(FolderType.Recent, member));

        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FileIds = [new(file.Id)] }, TestContext.Current.CancellationToken);

        await WaitForModified(socket, "delete", "file", file.Id);
    }
}
