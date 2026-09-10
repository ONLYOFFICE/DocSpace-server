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
/// <c>GET /people/simple/filter</c> and <c>GET /people/filter</c> — the two "browse all accounts"
/// filters used by the members list. Both are reachable by every non-guest, non-collaborator role.
/// </summary>
public class FilterSearchTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    [Fact]
    public async Task GetSimpleByFilter_AsOwner_FindsEveryRole()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);

        await AssertSimpleFilterFinds(admin);
        await AssertSimpleFilterFinds(roomAdmin);
        await AssertSimpleFilterFinds(user);
        await AssertSimpleFilterFinds(guest);
    }

    [Fact]
    public async Task GetSimpleByFilter_AsDocSpaceAdmin_FindsEveryRole()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(admin);

        await AssertSimpleFilterFinds(Owner);
        await AssertSimpleFilterFinds(roomAdmin);
        await AssertSimpleFilterFinds(user);
        await AssertSimpleFilterFinds(guest);
    }

    [Fact]
    public async Task GetSimpleByFilter_AsRoomAdmin_FindsOwnerAdminAndUser()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        await AssertSimpleFilterFinds(Owner);
        await AssertSimpleFilterFinds(admin);
        await AssertSimpleFilterFinds(user);
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsOwner_ReportsEachRoleFlagsCorrectly()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(Owner);

        await AssertExtendedFilterFlags(admin, isAdmin: true, isOwner: false, isRoomAdmin: false, isVisitor: false, isCollaborator: false);
        await AssertExtendedFilterFlags(roomAdmin, isAdmin: false, isOwner: false, isRoomAdmin: true, isVisitor: false, isCollaborator: false);
        await AssertExtendedFilterFlags(user, isAdmin: false, isOwner: false, isRoomAdmin: false, isVisitor: false, isCollaborator: true);
        await AssertExtendedFilterFlags(guest, isAdmin: false, isOwner: false, isRoomAdmin: false, isVisitor: true, isCollaborator: false);
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsDocSpaceAdmin_ReportsEachRoleFlagsCorrectly()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(admin);

        // The Owner is checked separately (Bug 80587): searching for them here would currently
        // report IsAdmin: true, which SearchUsersByExtendedFilter_Bug80587 asserts against.
        await AssertExtendedFilterFlags(roomAdmin, isAdmin: false, isOwner: false, isRoomAdmin: true, isVisitor: false, isCollaborator: false);
        await AssertExtendedFilterFlags(user, isAdmin: false, isOwner: false, isRoomAdmin: false, isVisitor: false, isCollaborator: true);
        await AssertExtendedFilterFlags(guest, isAdmin: false, isOwner: false, isRoomAdmin: false, isVisitor: true, isCollaborator: false);
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsRoomAdmin_ReportsAdminAndUserFlagsCorrectly()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        // The Owner is checked separately (Bug 80587), same as the DocSpace-admin case above.
        await AssertExtendedFilterFlags(admin, isAdmin: true, isOwner: false, isRoomAdmin: false, isVisitor: false, isCollaborator: false);
        await AssertExtendedFilterFlags(user, isAdmin: false, isOwner: false, isRoomAdmin: false, isVisitor: false, isCollaborator: true);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80587")]
    public async Task SearchUsersByExtendedFilter_SearchingForOwner_ReportsOwnerNotAdmin(EmployeeType actorType)
    {
        var ownerName = await DisplayNameOf(Owner);
        var actor = await InviteContact(actorType);

        await _peopleClient.Authenticate(actor);

        var owner = await PollAndFindByName(ownerName, Owner.Id);

        owner.IsOwner.Should().BeTrue();
        owner.IsAdmin.Should().BeFalse();
        owner.IsRoomAdmin.Should().BeFalse();
        owner.IsVisitor.Should().BeFalse();
        owner.IsCollaborator.Should().BeFalse();
    }

    private async Task AssertSimpleFilterFinds(User expected)
    {
        var name = await DisplayNameOf(expected);

        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetSimpleByFilterAsync(filterValue: name, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expected.Id) == true);

        result.Response.Should().Contain(u => u.Id == expected.Id && WebUtility.HtmlDecode(u.DisplayName) == name);
    }

    private async Task AssertExtendedFilterFlags(
        User expected, bool isAdmin, bool isOwner, bool isRoomAdmin, bool isVisitor, bool isCollaborator)
    {
        var name = await DisplayNameOf(expected);
        var entry = await PollAndFindByName(name, expected.Id);

        WebUtility.HtmlDecode(entry.DisplayName).Should().Be(name);
        entry.IsAdmin.Should().Be(isAdmin);
        entry.IsOwner.Should().Be(isOwner);
        entry.IsRoomAdmin.Should().Be(isRoomAdmin);
        entry.IsVisitor.Should().Be(isVisitor);
        entry.IsCollaborator.Should().Be(isCollaborator);
    }

    private async Task<EmployeeFullDto> PollAndFindByName(string name, Guid expectedId)
    {
        var result = await PollUntilAsync(
            () => _peopleSearchApi.SearchUsersByExtendedFilterAsync(filterValue: name, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == expectedId) == true);

        var entry = result.Response!.FirstOrDefault(u => u.Id == expectedId);
        entry.Should().NotBeNull();
        return entry!;
    }
}
