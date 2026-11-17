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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// The links of a private room and of its items managed by a room manager who is a room admin: the weakest role that
/// manages the room's links, where the other tests act as the portal owner.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkRoomManagerTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<(FolderDto Room, User Manager)> RoomWithAManager()
    {
        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        return (room, manager);
    }

    [Fact]
    public async Task RoomLink_SetListedAndRevokedByARoomManager()
    {
        var (room, manager) = await RoomWithAManager();
        await _filesClient.Authenticate(manager);

        Guid linkId;

        using (var stored = await PutRoomLink(room.Id, new LinkBody()))
        {
            ((int)stored.StatusCode).Should().Be(200);
            linkId = Guid.Parse((await SharedLinkOf(stored)).GetProperty("id").GetString()!);
        }

        (await RoomLinkCount(room.Id)).Should().Be(1);

        await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(linkId, FileShare.None, linkType: LinkType.External), TestContext.Current.CancellationToken);

        (await RoomLinkCount(room.Id)).Should().Be(0);
    }

    [Fact]
    public async Task FileLink_SetListedAndRevokedByARoomManager()
    {
        var (room, manager) = await RoomWithAManager();
        var fileId = await UploadEncryptedFile(room.Id, "managed.docx");
        await _filesClient.Authenticate(manager);

        var linkId = await StoreFileLink(fileId);

        (await FileLinkCount(fileId)).Should().Be(1);

        await RevokeFileLink(fileId, linkId);

        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task FolderLink_SetListedAndRevokedByARoomManager()
    {
        var (room, manager) = await RoomWithAManager();
        var folderId = await CreateFolderIn(room.Id);
        await _filesClient.Authenticate(manager);

        var linkId = await StoreFolderLink(folderId);

        (await FolderLinkCount(folderId)).Should().Be(1);

        await RevokeFolderLink(folderId, linkId);

        (await FolderLinkCount(folderId)).Should().Be(0);
    }
}
