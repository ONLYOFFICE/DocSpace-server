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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// Functional behaviour of <c>PUT /files/rooms/{id}/share</c>: what a single request actually does
/// to the room membership. Access control on the endpoint itself lives in
/// <c>Permissions/RoomSharePermissionsTests</c>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareSetTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task SetRoomSecurity_EditingAccess_MemberAddedAndVisibleInSecurityInfo()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(user, FileShare.Editing),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Members.Should().HaveCount(1);

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().HaveCount(2);
        info.Should().Contain(s => s.SharedToUser.Id == user.Id);
    }

    [Fact]
    public async Task SetRoomSecurity_AccessNone_RevokesPreviouslyGrantedAccess()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Room");
        await InviteToRoom(room.Id, user, FileShare.Editing);

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(user, FileShare.None),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().HaveCount(1);
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id);
    }

    [Fact]
    public async Task SetRoomSecurity_ContentCreatorAccess_GrantsContentCreator()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share ContentCreator");

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            BuildInvitation(user, FileShare.ContentCreator),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var entry = info.Find(s => s.SharedToUser?.Id == user.Id);
        entry.Should().NotBeNull();
        entry!.Access.Should().Be(FileShare.ContentCreator);
    }

    [Fact]
    public async Task SetRoomSecurity_MixedBatch_AddsUpdatesAndRemovesInOneRequest()
    {
        // Arrange
        var keep = await InviteMember(EmployeeType.User);
        var remove = await InviteMember(EmployeeType.User);
        var add = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Mixed Batch");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = keep.Id, Access = FileShare.Read },
                new RoomInvitation { Id = remove.Id, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act: update keep, remove remove, add add - all in a single request
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = keep.Id, Access = FileShare.Editing },
                new RoomInvitation { Id = remove.Id, Access = FileShare.None },
                new RoomInvitation { Id = add.Id, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.Find(s => s.SharedToUser?.Id == keep.Id)?.Access.Should().Be(FileShare.Editing);
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == remove.Id);
        info.Find(s => s.SharedToUser?.Id == add.Id)?.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task SetRoomSecurity_DuplicateUserInOneRequest_KeepsSingleEntryWithLastAccess()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Duplicate User");

        // Act
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user.Id, Access = FileShare.Read },
                new RoomInvitation { Id = user.Id, Access = FileShare.Editing }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var entries = info.FindAll(s => s.SharedToUser?.Id == user.Id);
        entries.Should().HaveCount(1);
        entries[0].Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task SetRoomSecurity_OwnerAccessNone_IsANoOpAndOwnerStaysInTheRoom()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share Owner Self Remove");

        // Act
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = Owner.Id, Access = FileShare.None }],
                Notify = false
            },
            TestContext.Current.CancellationToken);

        // Assert: the owner cannot remove themselves this way, self-removal is a no-op
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().Contain(s => s.IsOwner);
    }
}
