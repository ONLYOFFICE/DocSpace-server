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


namespace ASC.Files.Tests.Tests._03_Rooms.Archive;

/// <summary>
/// What each user type may do with an archived room: reading the member list, the history and the info
/// stays open, pinning is refused for everyone; a Room admin restores and deletes an archived room they
/// own; a DocSpace admin outside a room restores it, archives it again and deletes it.
/// </summary>
[Trait("Category", "Rooms")]
public class ArchivedRoomActionsByTypeTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// <paramref name="memberType"/> null is the Owner, who owns the room; a DocSpace admin acts as a
    /// non-member; the other types were invited with Read before the room was archived.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task ArchivedRoom_ByUserType_ReadsMembersHistoryInfoButCannotPin(EmployeeType? memberType)
    {
        // Arrange
        var actor = memberType is { } type ? await InviteMember(type) : Owner;

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Archived Read {memberType?.ToString() ?? "Owner"}");

        if (memberType is EmployeeType.RoomAdmin or EmployeeType.User or EmployeeType.Guest)
        {
            await InviteToRoom(room.Id, actor, FileShare.Read);
        }

        await ArchiveRoom(room.Id);

        await _filesClient.Authenticate(actor);

        // Act + Assert: reading stays open
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(room.Id);
        info.RootFolderType.Should().Be(FolderType.Archive);

        var members = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        members.Should().NotBeEmpty();

        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        history.Should().NotBeNull();

        // Act + Assert: pinning is refused
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.PinRoomAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RoomAdmin_OwnArchivedRoom_RestoresThenDeletes()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateCustomRoom("Autotest RoomAdmin Own Archived");
        await ArchiveRoom(room.Id);

        // Act: restore
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        var restore = await WaitLongOperation();

        // Assert
        restore.Should().OnlyContain(o => o.Finished && o.Error == "");
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.RootFolderType.Should().Be(FolderType.VirtualRooms);

        // Act: archive again and delete
        await ArchiveRoom(room.Id);
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var delete = await WaitLongOperation();

        // Assert
        delete.Should().OnlyContain(o => o.Finished && o.Error == "");
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task DocSpaceAdmin_ForeignArchivedRoom_RestoresArchivesAgainAndDeletes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Owner Archived For Admin Chain");
        await ArchiveRoom(room.Id);

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act + Assert: restore
        await _roomsApi.UnarchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.RootFolderType.Should().Be(FolderType.VirtualRooms);

        // Act + Assert: archive again
        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.RootFolderType.Should().Be(FolderType.Archive);

        // Act + Assert: delete
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        (await WaitLongOperation()).Should().OnlyContain(o => o.Finished && o.Error == "");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(404);
    }
}
