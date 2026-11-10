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
/// Folder changes reach a connected client as <c>s:modify-folder</c>, and only in the sections that can list a folder.
/// </summary>
[Trait("Category", "Socket")]
public class SocketFolderEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    [Fact]
    public async Task CreateFolder_ReachesTheParentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        var folder = await CreateFolderInMy("Socket Create Folder", Owner);

        var modified = await WaitForModified(socket, "create", "folder", folder.Id);
        using var data = DataOf(modified);
        data.RootElement.GetProperty("title").GetString().Should().Be("Socket Create Folder");
    }

    [Fact]
    public async Task RenameFolder_ReachesTheParentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolderInMy("Socket Rename Folder", Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder("Socket Renamed Folder"), TestContext.Current.CancellationToken);

        await WaitForModified(socket, "update", "folder", folder.Id);
    }

    [Fact]
    public async Task DeleteFolder_ReachesTheParentRoom()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolderInMy("Socket Delete Folder", Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        await _foldersApi.DeleteFolderAsync(folder.Id, new DeleteFolderRequest { Immediately = true }, TestContext.Current.CancellationToken);

        await WaitForModified(socket, "delete", "folder", folder.Id);
    }

    /// <summary>
    /// Dropping a shared folder from one's list clears it from Shared with me, and nothing reaches Recent, which lists
    /// files only. <c>RemoveAceAsync</c> also sends <c>delete-recent-folder</c> here, a method the socket service has no
    /// route for: it answers 404, which is why nothing reaches the client, and <c>SocketContractTests</c> reports it.
    /// </summary>
    [Fact]
    public async Task DropSharedFolderFromList_ReachesSharedWithMe_AndNotRecent()
    {
        await _filesClient.Authenticate(Owner);
        var member = await InviteContact(EmployeeType.User);
        var folder = await CreateFolderInMy("Socket Shared Folder", Owner);
        var link = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(member);
        await _sharingApi.GetExternalShareDataAsync(link.SharedLink.RequestToken, cancellationToken: TestContext.Current.CancellationToken);
        await using var sharedSocket = await ConnectAndWatch(member, await GetShareFolderIdAsync(member));
        await using var recentSocket = await ConnectAndWatch(member, await GetFolderIdAsync(FolderType.Recent, member));

        await _sharingApi.RemoveSecurityInfoAsync(new BaseBatchRequestDto { FolderIds = [new(folder.Id)] }, TestContext.Current.CancellationToken);

        await WaitForModified(sharedSocket, "delete", "folder", folder.Id);
        (await recentSocket.ArrivesWithinAsync(ModifyFolder, Modified("delete", "folder", folder.Id), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("Recent lists files only");
    }

    /// <summary>
    /// <c>DELETE files/recent</c> is documented to change nothing for a folder id, because Recent lists files only. But
    /// <c>DeleteFromRecentAsync</c> tells the caller's client that the folder was deleted from its real parent, which
    /// takes a folder that still exists off the open list. Red while it does: what to do about it is not decided yet.
    /// </summary>
    [Fact]
    public async Task DeleteRecent_WithAFolderId_DoesNotTakeTheFolderOffItsParentList()
    {
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        var folder = await CreateFolderInMy("Socket Not In Recent", Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        await _filesApi.DeleteRecentAsync(new BaseBatchRequestDto { FolderIds = [new(folder.Id)] }, TestContext.Current.CancellationToken);

        // Something that does reach the room, to know the notifications of this moment have been sent.
        var file = await CreateFile("Socket Sentinel.docx", myId);
        await WaitForModified(socket, "create", "file", file.Id);

        (await socket.ArrivesWithinAsync(ModifyFolder, Modified("delete", "folder", folder.Id), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("the folder was not deleted");
    }
}
