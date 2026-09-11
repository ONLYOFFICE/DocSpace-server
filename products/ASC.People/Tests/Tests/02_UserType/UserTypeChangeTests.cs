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
/// <c>PUT /people/type/:type</c> (synchronous), <c>PUT /people/type</c> +
/// <c>GET /people/type/progress/{userid}</c> (asynchronous) and <c>PUT /people/type/terminate</c> -
/// the happy-path type-change workflows an Owner or a DocSpace admin is allowed to drive.
/// </summary>
public class UserTypeChangeTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateUserType_OwnerPromotesGuestThroughAllTypes_ShouldSucceed()
    {
        var guest = await InviteGuest();

        // Guest -> User
        var toUser = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.User,
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
        toUser.Response.Should().ContainSingle();
        toUser.Response[0].IsCollaborator.Should().BeTrue();

        // User -> Room admin
        var toRoomAdmin = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.RoomAdmin,
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
        toRoomAdmin.Response[0].IsRoomAdmin.Should().BeTrue();

        // Room admin -> DocSpace admin
        var toAdmin = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.DocSpaceAdmin,
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
        toAdmin.Response[0].IsAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserType_DocSpaceAdminPromotesGuestToUserAndRoomAdmin_ShouldSucceed()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var guest = await InviteGuest(Owner);

        await _peopleClient.Authenticate(admin);

        // Guest -> User
        var toUser = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.User,
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
        toUser.Response[0].IsCollaborator.Should().BeTrue();

        // User -> Room admin
        var toRoomAdmin = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.RoomAdmin,
            new UpdateMembersRequestDto([guest.Id]),
            TestContext.Current.CancellationToken);
        toRoomAdmin.Response[0].IsRoomAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserType_OwnerDemotesDocSpaceAdminThroughAllTypes_ShouldSucceed()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // DocSpace admin -> Room admin (synchronous)
        var toRoomAdmin = await _userTypeApi.UpdateUserTypeAsync(
            EmployeeType.RoomAdmin,
            new UpdateMembersRequestDto([admin.Id]),
            TestContext.Current.CancellationToken);
        toRoomAdmin.Response[0].IsRoomAdmin.Should().BeTrue();

        // Room admin -> User (asynchronous)
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, admin.Id),
            TestContext.Current.CancellationToken);

        var toUserProgress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, admin.Id);
        toUserProgress.IsCompleted.Should().BeTrue();
        toUserProgress.Error.Should().BeEmpty();

        // User -> Guest (asynchronous)
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, admin.Id),
            TestContext.Current.CancellationToken);

        var toGuestProgress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, admin.Id);
        toGuestProgress.IsCompleted.Should().BeTrue();
        toGuestProgress.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task StartUserTypeUpdate_DocSpaceAdminDemotesRoomAdminToUserAndGuest_ShouldSucceed()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(admin);

        // Room admin -> User
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, roomAdmin.Id),
            TestContext.Current.CancellationToken);

        var toUserProgress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, roomAdmin.Id);
        toUserProgress.IsCompleted.Should().BeTrue();
        toUserProgress.Error.Should().BeEmpty();

        // User -> Guest
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, roomAdmin.Id),
            TestContext.Current.CancellationToken);

        var toGuestProgress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, roomAdmin.Id);
        toGuestProgress.IsCompleted.Should().BeTrue();
        toGuestProgress.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTypeUpdateProgress_OwnerDemotesDocSpaceAdminToUser_ShouldReportCompletion()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, admin.Id),
            TestContext.Current.CancellationToken);

        var progress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, admin.Id);

        progress.IsCompleted.Should().BeTrue();
        progress.Percentage.Should().Be(100);
        progress.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTypeUpdateProgress_DocSpaceAdminDemotesRoomAdminToUser_ShouldReportCompletion()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(admin);

        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, roomAdmin.Id),
            TestContext.Current.CancellationToken);

        var progress = await UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, roomAdmin.Id);

        progress.IsCompleted.Should().BeTrue();
        progress.Percentage.Should().Be(100);
        progress.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateUserTypeUpdate_DocSpaceAdminTerminatesUpdateStartedByOwner_LeavesTargetUnchanged()
    {
        var target = await InviteContact(EmployeeType.DocSpaceAdmin);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Owner starts the demotion (peopleClient is still authenticated as the owner, the
        // inviter of both members above).
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.User, target.Id),
            TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(admin);

        var terminated = await _userTypeApi.TerminateUserTypeUpdateAsync(
            new TerminateRequestDto(target.Id),
            TestContext.Current.CancellationToken);

        terminated.Response.IsCompleted.Should().BeTrue();
        terminated.Response.Error.Should().BeEmpty();
        terminated.Response.Status.Should().Be(DistributedTaskStatus.Canceled);

        await _peopleClient.Authenticate(Owner);
        var profile = await _profilesApi.GetProfileByUserIdAsync(target.Id.ToString(), TestContext.Current.CancellationToken);
        profile.Response.IsAdmin.Should().BeTrue();
    }
}
