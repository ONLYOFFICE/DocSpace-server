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
/// Room changes, and who gets them: joining a socket room is not access-checked, so what keeps an event from a user is
/// the list of recipients the notifier resolves, and the tenant in the room name.
/// </summary>
[Trait("Category", "Socket")]
public class SocketRoomEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    [Fact]
    public async Task CreateRoom_ReachesTheRoomsSection()
    {
        await _filesClient.Authenticate(Owner);
        var roomsId = await GetFolderIdAsync(FolderType.VirtualRooms, Owner);
        await using var socket = await ConnectAndWatch(Owner, roomsId);

        var room = await CreateRoom(new CreateRoomRequestDto("Socket Create Room", roomType: RoomType.CustomRoom));

        await WaitForModified(socket, "create", "folder", room.Id);
    }

    [Fact]
    public async Task RenameRoom_ReachesTheRoomsSection()
    {
        await _filesClient.Authenticate(Owner);
        var roomsId = await GetFolderIdAsync(FolderType.VirtualRooms, Owner);
        var room = await CreateRoom(new CreateRoomRequestDto("Socket Rename Room", roomType: RoomType.CustomRoom));
        await using var socket = await ConnectAndWatch(Owner, roomsId);

        await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(title: "Socket Renamed Room"), TestContext.Current.CancellationToken);

        await WaitForModified(socket, "update", "folder", room.Id);
    }

    [Fact]
    public async Task CreateFileInRoom_ReachesTheMember_AndNotAStrangerInTheSameRoomName()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto("Socket Members Room", roomType: RoomType.CustomRoom));
        var member = await InviteContact(EmployeeType.User);
        var stranger = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);
        await using var memberSocket = await ConnectAndWatch(member, room.Id);
        await using var strangerSocket = await ConnectAndWatch(stranger, room.Id);

        await _filesClient.Authenticate(Owner);
        var file = await CreateFile("Socket Room File.docx", room.Id);

        await WaitForModified(memberSocket, "create", "file", file.Id);
        (await strangerSocket.ArrivesWithinAsync(ModifyFolder, Modified("create", "file", file.Id), _noDeliveryWindow, TestContext.Current.CancellationToken))
            .Should().BeFalse("the stranger cannot read the room, even though nothing stops them from joining its socket room");
    }

    [Fact]
    public async Task CreateFile_DoesNotReachAnotherPortal_WatchingTheSameFolderId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _filesClient.Authenticate(Owner);
        var myId = await GetUserFolderIdAsync(Owner);
        await using var socket = await ConnectAndWatch(Owner, myId);

        using var otherPortal = await Fixture.CreatePortalAsync(cancellationToken);
        await using var otherSocket = await otherPortal.ConnectSocketAsync(otherPortal.Owner, cancellationToken);
        await otherSocket.SubscribeAsync(true, cancellationToken, Dir(myId));

        var file = await CreateFile("Socket Isolated.docx", myId);

        await WaitForModified(socket, "create", "file", file.Id);
        (await otherSocket.ArrivesWithinAsync(ModifyFolder, Modified("create", "file", file.Id), _noDeliveryWindow, cancellationToken))
            .Should().BeFalse("socket rooms are prefixed with the tenant");
    }
}
