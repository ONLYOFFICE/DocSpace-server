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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

/// <summary>
/// An invited Room manager (a Room admin holding the Room manager role in the Owner's room) neither archives the
/// room, nor restores or deletes it once archived: each is refused with 403 and the room stays where it was.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomArchiveRoomManagerTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task ArchiveRoom_InvitedRoomManager_Returns403AndRoomStaysActive()
    {
        // Arrange
        var (room, manager) = await CreateRoomWithManager(archived: false);

        // Act
        await _filesClient.Authenticate(manager);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await AssertRoomRoot(room.Id, FolderType.VirtualRooms);
    }

    [Fact]
    public async Task UnarchiveRoom_InvitedRoomManager_Returns403AndRoomStaysArchived()
    {
        // Arrange
        var (room, manager) = await CreateRoomWithManager(archived: true);

        // Act
        await _filesClient.Authenticate(manager);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await AssertRoomRoot(room.Id, FolderType.Archive);
    }

    [Fact]
    public async Task DeleteRoom_ArchivedRoomByInvitedRoomManager_Returns403AndRoomStays()
    {
        // Arrange
        var (room, manager) = await CreateRoomWithManager(archived: true);

        // Act
        await _filesClient.Authenticate(manager);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await AssertRoomRoot(room.Id, FolderType.Archive);
    }

    private async Task<(FolderDto Room, User Manager)> CreateRoomWithManager(bool archived)
    {
        var manager = await InviteMember(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Archive By Manager {archived}");
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        if (archived)
        {
            await ArchiveRoom(room.Id);
        }

        return (room, manager);
    }

    private async Task AssertRoomRoot(int roomId, FolderType root)
    {
        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomInfoAsync(roomId, TestContext.Current.CancellationToken)).Response;
        info.RootFolderType.Should().Be(root);
    }
}
