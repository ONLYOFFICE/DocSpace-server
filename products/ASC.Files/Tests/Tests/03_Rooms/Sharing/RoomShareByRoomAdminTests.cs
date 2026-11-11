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
/// <c>PUT /files/rooms/{id}/share</c> with a Room admin as the granting actor on their own room: every role
/// but Room manager goes to a User or a Guest (the check is on the target's type, not on the actor's); the
/// Room manager role is refused for both with "The role is not available for this user type".
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareByRoomAdminTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private async Task<(User RoomAdmin, User Target, FolderDto Room)> ArrangeAsync(EmployeeType targetType)
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var target = targetType == EmployeeType.Guest ? await InviteGuest(roomAdmin) : await InviteContact(targetType, roomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateCustomRoom($"Autotest RoomAdmin Grants {targetType}");

        return (roomAdmin, target, room);
    }

    [Theory]
    [MemberData(nameof(RoomAccessData.NonManagerAccessesForUserAndGuest), MemberType = typeof(RoomAccessData))]
    public async Task SetRoomSecurity_RoomAdminGrantsNonManagerRole_MemberHasIt(EmployeeType targetType, FileShare access)
    {
        var (_, target, room) = await ArrangeAsync(targetType);

        await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(target, access), cancellationToken: TestContext.Current.CancellationToken);

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shares.Should().ContainSingle(s => s.SharedToUser != null && s.SharedToUser.Id == target.Id)
            .Which.Access.Should().Be(access);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetRoomSecurity_RoomAdminGrantsRoomManagerToUserOrGuest_Forbidden(EmployeeType targetType)
    {
        var (_, target, room) = await ArrangeAsync(targetType);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, BuildInvitation(target, FileShare.RoomManager), cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent!.ToString().Should().Contain("The role is not available for this user type");

        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shares.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == target.Id);
    }
}
