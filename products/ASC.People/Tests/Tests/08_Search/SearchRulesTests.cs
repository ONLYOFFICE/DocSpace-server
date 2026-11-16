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
/// The rules behind the search endpoints that the functional suites do not pin down: the people
/// search reads the stored columns (no index to wait for), <c>GET /people/room/{id}</c> needs read
/// access while <c>GET /accounts/room/{id}/search</c> needs the right to edit access, an unknown
/// room answers 404, and the accounts search without a text returns nothing.
/// </summary>
public class SearchRulesTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    /// <summary>
    /// Read once, right after the invitation, with no poll: the people search is a plain query
    /// over the stored name columns, so there is no index lag to wait for.
    /// </summary>
    [Fact]
    public async Task SearchUsersByQuery_RightAfterInvitation_FindsTheStoredFirstName()
    {
        var member = await InviteContact(EmployeeType.User);
        var profile = (await _profilesApi.GetProfileByUserIdAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;

        var result = await _peopleSearchApi.SearchUsersByQueryAsync(profile.FirstName, TestContext.Current.CancellationToken);

        result.Response.Select(u => u.Id).Should().Contain(member.Id);
    }

    [Fact]
    public async Task GetUsersWithRoomShared_UnknownRoom_ReturnsNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleSearchApi.GetUsersWithRoomSharedAsync(int.MaxValue, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// One member with editing (not manager) access: enough to read the room, so the people search
    /// answers; not enough to edit its access, so the accounts search refuses.
    /// </summary>
    [Fact]
    public async Task RoomSearches_EditingMember_ReadsPeopleButIsRefusedAccounts()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, _) = await CreateRoomWithFileAsync("Autotest Search Rules", "Autotest Search File");
        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(member);
        var people = await _peopleSearchApi.GetUsersWithRoomSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(roomId, filterValue: "a", cancellationToken: TestContext.Current.CancellationToken));

        people.Response.Should().NotBeNull();
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_EmptyText_ReturnsNothingWithZeroTotal()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, _) = await CreateRoomWithFileAsync("Autotest Search Empty", "Autotest Search File");
        await InviteContact(EmployeeType.User);

        var result = await _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().BeEmpty();
        result.Count.Should().Be(0);
        result.Total.Should().Be(0);
    }
}
