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
/// Room management of a Virtual Data Room by role: only a Room manager edits the room and invites, re-roles and
/// removes members; a Content creator, an Editor, a Form Filler and a Viewer get 403 for each of those and the
/// room stays as it was. A Room manager is a Room admin, the other roles are held by Users.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrRoomManagementRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>VDR role of the acting member and whether it manages the room.</summary>
    public static TheoryData<FileShare, bool> ManageByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, false },
        { FileShare.Editing, false },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(ManageByRole))]
    public async Task UpdateRoom_VdrMemberRole_OnlyRoomManagerRenamesRoom(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Update {access}");
        var newTitle = $"Autotest VDR Updated by {access}";

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var updated = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(newTitle), TestContext.Current.CancellationToken)).Response;

            // Assert
            updated.Title.Should().Be(newTitle);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(newTitle), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
            await _filesClient.Authenticate(Owner);
            var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
            info.Title.Should().Be(room.Title);
        }
    }

    /// <summary>
    /// Inviting a new user, changing the role of a member and removing a member all go through
    /// <c>PUT rooms/{id}/share</c>: a Room manager does all three, every lower VDR role is refused each time.
    /// </summary>
    [Theory]
    [MemberData(nameof(ManageByRole))]
    public async Task SetRoomSecurity_VdrMemberRole_OnlyRoomManagerInvitesChangesRoleAndRemoves(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Share {access}");
        var newcomer = await InviteMember(EmployeeType.User);
        var existing = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, existing, FileShare.Read);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(newcomer, FileShare.FillForms), cancellationToken: TestContext.Current.CancellationToken);
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.Editing), cancellationToken: TestContext.Current.CancellationToken);
            var afterChange = await GetMemberAccesses(room.Id);
            await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.None), cancellationToken: TestContext.Current.CancellationToken);
            var afterRemove = await GetMemberAccesses(room.Id);

            // Assert
            afterChange.Should().Contain(newcomer.Id, FileShare.FillForms);
            afterChange.Should().Contain(existing.Id, FileShare.Editing);
            afterRemove.Should().NotContainKey(existing.Id);
        }
        else
        {
            var invite = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(newcomer, FileShare.Read), cancellationToken: TestContext.Current.CancellationToken));
            var change = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.Editing), cancellationToken: TestContext.Current.CancellationToken));
            var remove = await Assert.ThrowsAsync<ApiException>(
                async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(existing, FileShare.None), cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            invite.ErrorCode.Should().Be(403);
            change.ErrorCode.Should().Be(403);
            remove.ErrorCode.Should().Be(403);

            var accesses = await GetMemberAccesses(room.Id);
            accesses.Should().NotContainKey(newcomer.Id);
            accesses.Should().Contain(existing.Id, FileShare.Read);
        }
    }

    private async Task<Dictionary<Guid, FileShare?>> GetMemberAccesses(int roomId)
    {
        await _filesClient.Authenticate(Owner);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares
            .Where(s => s.SharedToUser is not null)
            .ToDictionary(s => s.SharedToUser.Id, s => s.Access);
    }

    private async Task<(FolderDto Room, User Member)> CreateVdrWithMember(FileShare access, string title)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom(title);
        await InviteToRoom(room.Id, member, access);

        return (room, member);
    }
}
