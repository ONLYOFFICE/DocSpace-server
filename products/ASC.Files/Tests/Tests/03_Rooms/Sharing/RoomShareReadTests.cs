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
/// Response contract and revocation behaviour of <c>GET /files/rooms/{id}/share</c>. Access
/// control on the endpoint itself (owner / RoomManager / invited user / not-invited user or
/// guest / anonymous) lives in <c>Permissions/RoomShareReadPermissionsTests</c> - only the
/// DocSpaceAdmin case is new here, since that role is not covered there.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareReadTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task GetRoomSecurityInfo_ResponseItem_HasExpectedShape()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Shape");
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entry = info.Find(s => s.SharedToUser?.Id == user.Id);
        entry.Should().NotBeNull();
        entry!.SharedToUser.DisplayName.Should().NotBeNullOrEmpty();
        entry.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_NewRoom_ContainsOnlyOwner()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share OwnerOnly");

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomSecurityInfo_InvitedUser_AppearsWithTheAssignedAccess()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Invited Access");
        await InviteToRoom(room.Id, user, FileShare.Editing);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entry = info.Find(s => s.SharedToUser?.Id == user.Id);
        entry.Should().NotBeNull();
        entry!.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_MultipleInvitedUsers_AreAllReturned()
    {
        // Arrange
        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        var user3 = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Multiple Invited");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user1.Id, Access = FileShare.Read },
                new RoomInvitation { Id = user2.Id, Access = FileShare.Editing },
                new RoomInvitation { Id = user3.Id, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(4);
        var ids = info.ConvertAll(s => s.SharedToUser?.Id);
        ids.Should().Contain(user1.Id);
        ids.Should().Contain(user2.Id);
        ids.Should().Contain(user3.Id);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_RoomManagerAccess_IsReturned()
    {
        // Arrange
        var manager = await InviteMember(EmployeeType.RoomAdmin);
        var room = await CreateCustomRoom("Autotest Share Manager Access");
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entry = info.Find(s => s.SharedToUser?.Id == manager.Id);
        entry.Should().NotBeNull();
        entry!.Access.Should().Be(FileShare.RoomManager);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_ChangingUserAccess_UpdatesEntryWithoutDuplicatingIt()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Change Access No Dup");
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entries = info.FindAll(s => s.SharedToUser?.Id == user.Id);
        entries.Should().HaveCount(1);
        entries[0].Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_RemovingOneUser_DoesNotAffectTheOthers()
    {
        // Arrange
        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Remove One Keep Other");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user1.Id, Access = FileShare.Read },
                new RoomInvitation { Id = user2.Id, Access = FileShare.Editing }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);
        await InviteToRoom(room.Id, user1, FileShare.None);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var ids = info.ConvertAll(s => s.SharedToUser?.Id);
        ids.Should().NotContain(user1.Id);
        ids.Should().Contain(user2.Id);
        info.Find(s => s.SharedToUser?.Id == user2.Id)!.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_RevokingAllInvitedUsers_OwnerStillPresent()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Owner Persists");
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.None);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoomSecurityInfo_DocSpaceAdmin_CanReadSecurityInfoOfAForeignRoom()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share Admin Access");
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().NotBeNull();
    }

    public static TheoryData<RoomType> RoomTypes =>
    [
        RoomType.CustomRoom, RoomType.EditingRoom, RoomType.PublicRoom, RoomType.FillingFormsRoom, RoomType.VirtualDataRoom
    ];

    [Theory]
    [MemberData(nameof(RoomTypes))]
    public async Task GetRoomSecurityInfo_EveryRoomType_ContainsTheOwner(RoomType roomType)
    {
        // Arrange
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto($"Autotest Share Type {roomType}", roomType: roomType), TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().Contain(s => s.IsOwner);
    }
}
