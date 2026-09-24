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

namespace ASC.People.Tests.Tests._15_PeopleGroups;

/// <summary>
/// <c>POST /group/{id}/members</c> — replacing a group's member list.
/// </summary>
public class GroupMembersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79368")]
    public async Task SetMembersTo_DisabledUser_RejectsAndKeepsExistingMembers()
    {
        var member = await InviteContact(EmployeeType.User);
        var disabledUser = await InviteContact(EmployeeType.User);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(members: [member.Id], groupManager: admin.Id, groupName: "Autotest Group"),
            TestContext.Current.CancellationToken)).Response;

        await TerminateUser(disabledUser);
        await _peopleClient.Authenticate(admin);

        // Expected: replacing the members with a disabled user is rejected outright.
        // Previously the request succeeded and wiped out the existing active member instead.
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.SetMembersToAsync(group.Id, new MembersRequest([disabledUser.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        var groupAfter = (await _groupApi.GetGroupAsync(group.Id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        groupAfter.Members.Should().Contain(m => m.Id == member.Id);
    }
}
