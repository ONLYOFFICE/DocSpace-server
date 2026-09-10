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
/// Permission checks on <c>PUT /people/type/:type</c> (synchronous) and <c>PUT /people/type</c>
/// (asynchronous). The two DocSpace-admin-only bugs the TypeScript suite marks with
/// <c>test.fail</c> for this endpoint (#80474, #80478) already have regression coverage in
/// <c>Tests/00_Regressions/UserTest.cs</c> and are not duplicated here.
/// </summary>
public class UserTypeUpdatePermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateUserType_RoomAdminUpgradesUser_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        var toRoomAdmin = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.RoomAdmin,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));
        toRoomAdmin.ErrorCode.Should().Be(403);
        toRoomAdmin.ErrorContent?.ToString().Should().Contain("Access denied");

        var toDocSpaceAdmin = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.DocSpaceAdmin,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));
        toDocSpaceAdmin.ErrorCode.Should().Be(403);
        toDocSpaceAdmin.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserType_UserChangesAnotherUsersType_ShouldThrowAccessDenied()
    {
        var user = await InviteContact(EmployeeType.User);
        var actor = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(actor);

        var toRoomAdmin = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.RoomAdmin,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));
        toRoomAdmin.ErrorCode.Should().Be(403);
        toRoomAdmin.ErrorContent?.ToString().Should().Contain("Access denied");

        var toDocSpaceAdmin = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.DocSpaceAdmin,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));
        toDocSpaceAdmin.ErrorCode.Should().Be(403);
        toDocSpaceAdmin.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserType_UserPromotesGuestThatDoesNotBelongToThem_ShouldThrowAccessDenied()
    {
        var guest = await InviteGuest();
        var actor = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.User,
                new UpdateMembersRequestDto([guest.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateUserType_AnonymousUser_ShouldThrowUnauthorized()
    {
        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.User,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task StartUserTypeUpdate_InvalidUserId_ShouldThrowBadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.StartUserTypeUpdateAsync(
                new StartUpdateUserTypeDto(EmployeeType.Guest, Guid.Empty),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Can not update type");
    }

    [Fact]
    public async Task StartUserTypeUpdate_RoomAdminDowngradesUser_ShouldFailWithPermissionError()
    {
        var user = await InviteContact(EmployeeType.User);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, user.Id),
            TestContext.Current.CancellationToken);

        // The room admin has no permission to check the progress of its own request (see
        // UserTypeProgressAndTerminatePermissionsTests), so the owner polls it instead.
        await _peopleClient.Authenticate(Owner);

        var progress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, user.Id);

        progress.IsCompleted.Should().BeTrue();
        progress.Error.Should().Be("You don't have enough permission to perform the operation");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80669")]
    public async Task StartUserTypeUpdate_RoomAdminPromotesOwnGuestToUser_ShouldSucceed()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var guest = await InviteGuest(roomAdmin);

        await _peopleClient.Authenticate(roomAdmin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, guest.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(Owner);

        var deadline = DateTime.UtcNow.AddSeconds(30);
        var isCollaborator = false;

        while (true)
        {
            isCollaborator = (await _profilesApi.GetProfileByUserIdAsync(guest.Id.ToString(), TestContext.Current.CancellationToken))
                .Response.IsCollaborator;

            if (isCollaborator || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }

        isCollaborator.Should().BeTrue();
    }
}
