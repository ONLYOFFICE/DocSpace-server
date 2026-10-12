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
/// Permissions for <c>GET /people/simple/filter</c> and <c>GET /people/filter</c>. Neither is
/// reachable by a plain user, a guest, or an anonymous caller; a room admin may call both, but
/// only sees guests they invited themselves — not every guest on the portal.
/// </summary>
public class FilterSearchPermissionsTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    [Fact]
    public async Task GetSimpleByFilter_AsRoomAdmin_DoesNotSeeGuestInvitedByOwner()
    {
        var guest = await InviteGuest();
        var guestName = await DisplayNameOf(guest);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        // Confirm the guest is indexed before asserting it is absent from someone else's view -
        // otherwise a not-yet-indexed guest would be indistinguishable from a correctly filtered one.
        await _peopleClient.Authenticate(Owner);
        await PollUntilAsync(
            () => _peopleSearchApi.GetSimpleByFilterAsync(filterValue: guestName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == guest.Id) == true);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await _peopleSearchApi.GetSimpleByFilterAsync(filterValue: guestName, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().NotContain(u => u.Id == guest.Id);
    }

    [Fact]
    public async Task GetSimpleByFilter_AsUser_ThrowsAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSimpleByFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSimpleByFilter_AsGuest_ThrowsAccessDenied()
    {
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSimpleByFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSimpleByFilter_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSimpleByFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsRoomAdmin_DoesNotSeeGuestInvitedByOwner()
    {
        var guest = await InviteGuest();
        var guestName = await DisplayNameOf(guest);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(Owner);
        await PollUntilAsync(
            () => _peopleSearchApi.SearchUsersByExtendedFilterAsync(filterValue: guestName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Any(u => u.Id == guest.Id) == true);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await _peopleSearchApi.SearchUsersByExtendedFilterAsync(filterValue: guestName, cancellationToken: TestContext.Current.CancellationToken);

        result.Response.Should().NotContain(u => u.Id == guest.Id);
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsUser_ThrowsAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByExtendedFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_AsGuest_ThrowsAccessDenied()
    {
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByExtendedFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByExtendedFilter_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByExtendedFilterAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
