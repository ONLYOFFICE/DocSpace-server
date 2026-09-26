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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// BUG 83188: on new portals, a deleted user kept showing up in <c>GET /people</c> after
/// deletion. Covers both the plain case and the case where the user owned content that had to be
/// reassigned first.
/// </summary>
public class DeletedUserVisibilityTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "83188")]
    public async Task DeleteMember_PlainUser_ShouldNotAppearInContactsListAfterDeletion()
    {
        var user = await InviteContact(EmployeeType.User);
        await TerminateUser(user);
        await _profilesApi.DeleteMemberAsync(user.Id.ToString(), TestContext.Current.CancellationToken);

        var list = await _profilesApi.GetAllProfilesAsync(cancellationToken: TestContext.Current.CancellationToken);

        list.Response.Should().Contain(u => u.Id == Owner.Id);
        list.Response.Should().NotContain(u => u.Id == user.Id);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "83188")]
    public async Task DeleteMember_UserWithReassignedContent_ShouldNotAppearInContactsListAfterDeletion()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(roomAdmin);
        var room = await CreateCustomRoom("Autotest Delete User Room");
        await _filesApi.CreateFileAsync(room.Id, new CreateFileJsonElement("Autotest Delete User File"), TestContext.Current.CancellationToken);
        await _filesClient.Authenticate(Owner);

        await TerminateUser(roomAdmin);

        await _userDataApi.StartReassignAsync(
            new StartReassignRequestDto(roomAdmin.Id, Owner.Id),
            TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(60);
        var completed = false;

        while (true)
        {
            var progress = await _userDataApi.GetReassignProgressAsync(roomAdmin.Id, TestContext.Current.CancellationToken);
            completed = progress.Response.IsCompleted;

            if (completed || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }

        completed.Should().BeTrue("the reassignment should complete within the deadline");

        await _profilesApi.DeleteMemberAsync(roomAdmin.Id.ToString(), TestContext.Current.CancellationToken);

        var list = await _profilesApi.GetAllProfilesAsync(cancellationToken: TestContext.Current.CancellationToken);

        list.Response.Should().Contain(u => u.Id == Owner.Id);
        list.Response.Should().NotContain(u => u.Id == roomAdmin.Id);
    }
}
