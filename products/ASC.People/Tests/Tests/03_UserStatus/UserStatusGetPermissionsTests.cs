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

namespace ASC.People.Tests.Tests._03_UserStatus;

/// <summary>
/// <c>GET /people/status/{status}</c> — access control. A plain User and a Guest may not list
/// profiles by status at all, whichever status is requested. Functional coverage of the endpoint
/// itself lives in <see cref="UserStatusGetTests"/>.
/// </summary>
public class UserStatusGetPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetByStatus_UserFilterActive_ReturnsAccessDenied()
    {
        await _peopleClient.Authenticate(Owner);
        await InviteContact(EmployeeType.DocSpaceAdmin);
        await InviteContact(EmployeeType.RoomAdmin);
        await InviteGuest();

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.GetByStatusAsync(EmployeeStatus.Active, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetByStatus_UserFilterTerminated_ReturnsAccessDenied()
    {
        await _peopleClient.Authenticate(Owner);
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var terminatedUser = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([docSpaceAdmin.Id, roomAdmin.Id, terminatedUser.Id, guest.Id], false),
            TestContext.Current.CancellationToken);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.GetByStatusAsync(EmployeeStatus.Terminated, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetByStatus_GuestFilterTerminated_ReturnsAccessDenied()
    {
        await _peopleClient.Authenticate(Owner);
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var terminatedGuest = await InviteGuest();

        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([docSpaceAdmin.Id, roomAdmin.Id, user.Id, terminatedGuest.Id], false),
            TestContext.Current.CancellationToken);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.GetByStatusAsync(EmployeeStatus.Terminated, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
