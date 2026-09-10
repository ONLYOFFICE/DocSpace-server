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

namespace ASC.People.Tests.Tests._02_UserType;

/// <summary>
/// Permission checks on <c>GET /people/type/progress/{userid}</c> and
/// <c>PUT /people/type/terminate</c>: only an Owner or a DocSpace admin may inspect or cancel a
/// type-change task, regardless of who started it.
/// </summary>
public class UserTypeProgressAndTerminatePermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetUserTypeUpdateProgress_RoomAdmin_ShouldThrowAccessDenied()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, admin.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.GetUserTypeUpdateProgressAsync(admin.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUserTypeUpdateProgress_User_ShouldThrowAccessDenied()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, admin.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.GetUserTypeUpdateProgressAsync(admin.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUserTypeUpdateProgress_Guest_ShouldThrowAccessDenied()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest();

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, admin.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.GetUserTypeUpdateProgressAsync(admin.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_RoomAdminTerminatesUpdateStartedByDocSpaceAdmin_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(admin);
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(user.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_GuestTerminatesUpdateStartedByOwner_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(user.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_GuestTerminatesUpdateStartedByDocSpaceAdmin_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest();

        await _peopleClient.Authenticate(admin);
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(user.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_UserTerminatesUpdateStartedByOwner_ShouldThrowAccessDenied()
    {
        var member = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, member.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(member.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_UserTerminatesUpdateStartedByDocSpaceAdmin_ShouldThrowAccessDenied()
    {
        var member = await InviteContact(EmployeeType.RoomAdmin);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(admin);
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, member.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(member.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_RoomAdminTerminatesUpdateStartedByOwner_ShouldThrowAccessDenied()
    {
        var member = await InviteContact(EmployeeType.RoomAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, member.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.TerminateUserTypeUpdateAsync(
                new TerminateRequestDto(member.Id),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
