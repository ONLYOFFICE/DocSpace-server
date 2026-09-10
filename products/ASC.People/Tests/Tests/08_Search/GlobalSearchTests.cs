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

namespace ASC.People.Tests.Tests._08_Search;

/// <summary>
/// <c>GET /accounts/search</c>, <c>GET /people/search</c> and
/// <c>GET /people/status/{status}/search</c> — portal-wide search, not scoped to a room. Every
/// portal role is searchable by both its email and its display name, and by whichever
/// <see cref="EmployeeStatus"/> it currently has.
///
/// <c>EmployeeDto</c> (the model behind <c>GET /people/search</c>) does not carry an
/// <c>Email</c> property, so unlike the TypeScript suite these assertions match by the invited
/// user's <c>Id</c> rather than by re-reading the field that was searched for — the point of the
/// test, that the query resolves to the right account, is unaffected either way.
/// </summary>
public class GlobalSearchTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    [Fact]
    public async Task GetSearch_AsOwner_FindsEachRoleByEmailAndName()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);

        await AssertGetSearchFinds(admin);
        await AssertGetSearchFinds(roomAdmin);
        await AssertGetSearchFinds(user);
        await AssertGetSearchFinds(guest);
    }

    [Fact]
    public async Task GetSearch_AsDocSpaceAdmin_FindsEachRoleByEmailAndName()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(admin);

        await AssertGetSearchFinds(Owner);
        await AssertGetSearchFinds(roomAdmin);
        await AssertGetSearchFinds(user);
        await AssertGetSearchFinds(guest);
    }

    [Fact]
    public async Task SearchUsersByQuery_AsOwner_FindsEachRoleByEmailAndName()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);

        await AssertSearchUsersByQueryFinds(admin);
        await AssertSearchUsersByQueryFinds(roomAdmin);
        await AssertSearchUsersByQueryFinds(user);
        await AssertSearchUsersByQueryFinds(guest);
    }

    [Fact]
    public async Task SearchUsersByQuery_AsDocSpaceAdmin_FindsEachRoleByEmailAndName()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(admin);

        await AssertSearchUsersByQueryFinds(Owner);
        await AssertSearchUsersByQueryFinds(roomAdmin);
        await AssertSearchUsersByQueryFinds(user);
        await AssertSearchUsersByQueryFinds(guest);
    }

    [Fact]
    public async Task SearchUsersByStatus_AsOwner_FindsActiveThenTerminatedUser()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        await AssertFoundByStatus(EmployeeStatus.Active, user);

        await TerminateUser(user);

        await _peopleClient.Authenticate(Owner);
        await AssertFoundByStatus(EmployeeStatus.Terminated, user);
    }

    [Fact]
    public async Task SearchUsersByStatus_AsDocSpaceAdmin_FindsActiveThenTerminatedUser()
    {
        var user = await InviteContact(EmployeeType.User);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(admin);
        await AssertFoundByStatus(EmployeeStatus.Active, user);

        await TerminateUser(user);

        await _peopleClient.Authenticate(admin);
        await AssertFoundByStatus(EmployeeStatus.Terminated, user);
    }

    private async Task AssertGetSearchFinds(User expected)
    {
        var name = await DisplayNameOf(expected);

        var byEmail = await PollUntilAsync(
            () => _peopleSearchApi.GetSearchAsync(expected.Email, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);
        byEmail.Response.Should().Contain(u => u.Id == expected.Id);

        var byName = await PollUntilAsync(
            () => _peopleSearchApi.GetSearchAsync(name, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);
        byName.Response.Should().Contain(u => u.Id == expected.Id);
    }

    private async Task AssertSearchUsersByQueryFinds(User expected)
    {
        var name = await DisplayNameOf(expected);

        var byEmail = await PollUntilAsync(
            () => _peopleSearchApi.SearchUsersByQueryAsync(expected.Email, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);
        byEmail.Response.Should().Contain(u => u.Id == expected.Id);

        var byName = await PollUntilAsync(
            () => _peopleSearchApi.SearchUsersByQueryAsync(name, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);
        byName.Response.Should().Contain(u => u.Id == expected.Id);
    }

    private async Task AssertFoundByStatus(EmployeeStatus status, User expected)
    {
        var result = await PollUntilAsync(
            () => _peopleSearchApi.SearchUsersByStatusAsync(status, query: expected.Email, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);

        result.Response.Should().Contain(u => u.Id == expected.Id && u.Email == expected.Email);
    }
}
