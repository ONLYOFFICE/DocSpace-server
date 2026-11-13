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

namespace ASC.People.Tests.Tests._08_Search.ShareCandidates;

/// <summary>
/// The users offered to a member of a room, through <c>GET /people/room/{id}</c>. Any member of a room sees its
/// access list, and that list keeps the Restrict records that take a member of a group out of the room, so the
/// list may make known only the accounts it gives access to.
/// </summary>
public class ShareCandidateRoomUsersTests(AspireAppFixture fixture) : ShareCandidatesTestBase(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task GetUsersWithRoomShared_AsUserMember_DoesNotOfferGroupMemberRestrictedFromTheRoom()
    {
        var caller = await InviteContact(EmployeeType.User);
        var groupMember = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [groupMember.Id]);
        var roomId = await CreateRoomWithMembersAsync(caller.Id, group.Id);

        // Offered through the group first, so the absence below comes from the restriction alone.
        (await UsersOfferedForRoomAsync(caller, roomId)).Select(u => u.Id).Should().Contain(groupMember.Id);

        await RestrictInRoomAsync(roomId, groupMember.Id, templateSubject: group.Id);
        var offered = await UsersOfferedForRoomAsync(caller, roomId);

        offered.Select(u => u.Id).Should().NotContain(groupMember.Id);
    }

    private async Task<List<EmployeeFullDto>> UsersOfferedForRoomAsync(User caller, int roomId)
    {
        await _peopleClient.Authenticate(caller);

        return (await _peopleSearchApi.GetUsersWithRoomSharedAsync(
            roomId,
            employeeStatus: EmployeeStatus.Active,
            area: AccountSearchArea.People,
            count: 100,
            cancellationToken: TestContext.Current.CancellationToken)).Response;
    }
}
