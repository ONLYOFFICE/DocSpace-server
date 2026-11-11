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
/// The per-user-type role table (<c>FileSecurity.AvailableUserAccesses</c>): a DocSpace admin and a Room admin take
/// every role, a User and a Guest every role but Room manager. The roles are offered in a Custom room, Form Filler in
/// a VDR (the Custom room has none). And the room owner is the creator: no invitation makes another member the owner.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareUserTypeRolesTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private static readonly FileShare[] _customRoomRoles =
        [FileShare.RoomManager, FileShare.ContentCreator, FileShare.Editing, FileShare.Review, FileShare.Comment, FileShare.Read];

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetRoomSecurity_EveryRoleForUserType_AcceptedExactlyAsThePerTypeTable(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(Owner);
        var customRoom = await CreateCustomRoom($"Autotest Type Roles {employeeType}");
        var vdr = await CreateVDRRoom($"Autotest Type Roles VDR {employeeType}");

        var expected = new Dictionary<FileShare, bool>
        {
            [FileShare.RoomManager] = employeeType is EmployeeType.DocSpaceAdmin or EmployeeType.RoomAdmin,
            [FileShare.ContentCreator] = true,
            [FileShare.Editing] = true,
            [FileShare.Review] = true,
            [FileShare.Comment] = true,
            [FileShare.Read] = true,
            [FileShare.FillForms] = true
        };

        // Act
        var actual = new Dictionary<FileShare, bool>();

        foreach (var access in _customRoomRoles)
        {
            actual[access] = await TryInvite(customRoom.Id, member, access);
        }

        actual[FileShare.FillForms] = await TryInvite(vdr.Id, member, FileShare.FillForms);

        // Assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SetRoomSecurity_RoomManagerInvited_CreatorStaysTheOnlyOwner()
    {
        // Arrange
        var creator = await InviteMember(EmployeeType.RoomAdmin);
        var manager = await InviteMember(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(creator);
        var room = await CreateCustomRoom("Autotest Owner Is Creator");

        // Act
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        // Assert
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Where(s => s.IsOwner).Select(s => s.SharedToUser?.Id).Should().Equal(creator.Id);
        info.Single(s => s.SharedToUser?.Id == manager.Id).Access.Should().Be(FileShare.RoomManager);

        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomInfo.CreatedBy.Id.Should().Be(creator.Id);
    }

    /// <summary>Invites as the Owner; true when the role was stored, false on the 403 of a role the type may not hold.</summary>
    private async Task<bool> TryInvite(int roomId, User member, FileShare access)
    {
        try
        {
            await _roomsApi.SetRoomSecurityAsync(roomId, BuildInvitation(member, access), TestContext.Current.CancellationToken);
        }
        catch (ApiException e) when (e.ErrorCode == 403)
        {
            return false;
        }

        var info = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return info.Exists(s => s.SharedToUser?.Id == member.Id && s.Access == access);
    }
}
