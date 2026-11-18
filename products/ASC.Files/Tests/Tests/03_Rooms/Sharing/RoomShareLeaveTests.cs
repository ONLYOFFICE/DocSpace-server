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
/// Leaving a room: a member without the manage right sets access None on themselves and is gone; naming another
/// member alone is refused with 403 and changes nothing, and next to themselves only the caller leaves.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareLeaveTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.ContentCreator)]
    public async Task SetRoomSecurity_MemberSetsNoneOnSelf_LeavesTheRoom(FileShare access)
    {
        // Arrange
        var (room, member, _) = await CreateRoomWithTwoMembers(access);

        // Act
        await _filesClient.Authenticate(member);
        await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(member, FileShare.None), TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
    }

    [Fact]
    public async Task SetRoomSecurity_MemberRemovesAnotherMember_Returns403AndMemberStays()
    {
        // Arrange
        var (room, member, other) = await CreateRoomWithTwoMembers(FileShare.Read);

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(other, FileShare.None), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        await AssertBothStay(room.Id, member, other);
    }

    /// <summary>
    /// A plain User cannot view another plain User, so the controller drops the entry naming the other member before
    /// the access check; what is left is the caller's own None, which is a leave. The caller is gone, the other member
    /// stays: the request removes only the caller.
    /// </summary>
    [Fact]
    public async Task SetRoomSecurity_MemberRemovesSelfAndAnotherMember_OnlyTheCallerLeaves()
    {
        // Arrange
        var (room, member, other) = await CreateRoomWithTwoMembers(FileShare.Read);

        var request = new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = member.Id, Access = FileShare.None },
                new RoomInvitation { Id = other.Id, Access = FileShare.None }
            ],
            Notify = false
        };

        // Act
        await _filesClient.Authenticate(member);
        await _roomsApi.SetRoomSecurityAsync(room.Id, request, TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == other.Id && s.Access == FileShare.Read);
    }

    private async Task<(FolderDto Room, User Member, User Other)> CreateRoomWithTwoMembers(FileShare access)
    {
        var member = await InviteMember(EmployeeType.User);
        var other = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Leave Room {access}");
        await InviteToRoom(room.Id, member, access);
        await InviteToRoom(room.Id, other, FileShare.Read);

        return (room, member, other);
    }

    private async Task AssertBothStay(int roomId, User member, User other)
    {
        await _filesClient.Authenticate(Owner);
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id);
        info.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == other.Id);
    }
}
