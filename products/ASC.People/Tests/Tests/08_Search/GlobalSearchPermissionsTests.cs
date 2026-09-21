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
/// Permissions for the portal-wide search endpoints: <c>GET /accounts/search</c>,
/// <c>GET /people/search</c> and <c>GET /people/status/{status}/search</c>. Only the Owner and a
/// DocSpace admin may browse the portal's accounts this way — a room admin, a plain user and a
/// guest must all be refused regardless of who they search for, and an anonymous caller is
/// unauthorized outright.
/// </summary>
public class GlobalSearchPermissionsTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    // ---- GET /accounts/search ----

    [Fact]
    public async Task GetSearch_AsRoomAdmin_ThrowsAccessDenied()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSearchAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSearch_AsUser_ThrowsAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSearchAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSearch_AsGuest_ThrowsAccessDenied()
    {
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSearchAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetSearch_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetSearchAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /people/search ----

    [Fact]
    public async Task SearchUsersByQuery_AsRoomAdmin_ThrowsAccessDenied()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByQueryAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByQuery_AsUser_ThrowsAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByQueryAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByQuery_AsGuest_ThrowsAccessDenied()
    {
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByQueryAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByQuery_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByQueryAsync(Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /people/status/:status/search ----

    [Fact]
    public async Task SearchUsersByStatus_AsRoomAdmin_ThrowsAccessDeniedForActiveAndTerminated()
    {
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);
        var activeException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Active, query: user.Email, cancellationToken: TestContext.Current.CancellationToken));
        activeException.ErrorCode.Should().Be(403);
        activeException.ErrorContent?.ToString().Should().Contain("Access denied");

        await TerminateUser(user);

        await _peopleClient.Authenticate(roomAdmin);
        var terminatedException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Terminated, query: user.Email, cancellationToken: TestContext.Current.CancellationToken));
        terminatedException.ErrorCode.Should().Be(403);
        terminatedException.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByStatus_AsUser_ThrowsAccessDeniedForActiveAndTerminated()
    {
        var searchable = await InviteContact(EmployeeType.User);
        var caller = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(caller);
        var activeException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Active, query: searchable.Email, cancellationToken: TestContext.Current.CancellationToken));
        activeException.ErrorCode.Should().Be(403);
        activeException.ErrorContent?.ToString().Should().Contain("Access denied");

        await TerminateUser(searchable);

        await _peopleClient.Authenticate(caller);
        var terminatedException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Terminated, query: searchable.Email, cancellationToken: TestContext.Current.CancellationToken));
        terminatedException.ErrorCode.Should().Be(403);
        terminatedException.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByStatus_AsGuest_ThrowsAccessDeniedForActiveAndTerminated()
    {
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var activeException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Active, query: user.Email, cancellationToken: TestContext.Current.CancellationToken));
        activeException.ErrorCode.Should().Be(403);
        activeException.ErrorContent?.ToString().Should().Contain("Access denied");

        await TerminateUser(user);

        await _peopleClient.Authenticate(guest);
        var terminatedException = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Terminated, query: user.Email, cancellationToken: TestContext.Current.CancellationToken));
        terminatedException.ErrorCode.Should().Be(403);
        terminatedException.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task SearchUsersByStatus_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.SearchUsersByStatusAsync(EmployeeStatus.Active, query: Owner.Email, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
