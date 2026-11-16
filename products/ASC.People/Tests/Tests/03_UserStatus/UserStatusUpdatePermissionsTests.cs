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
/// <c>PUT /people/status/{status}</c> — access control. Only the owner and a DocSpace admin may
/// change another member's status at all, and a DocSpace admin may not touch the owner or a peer
/// DocSpace admin. Functional coverage of the endpoint itself lives in
/// <see cref="UserStatusUpdateTests"/>.
/// </summary>
public class UserStatusUpdatePermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpdateUserStatus_Unauthenticated_ReturnsUnauthorized()
    {
        await _peopleClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);

        var request = new UpdateMembersRequestDto([user.Id], false);

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // The product should never let a DocSpace admin deactivate a peer DocSpace admin; today it does,
    // which is the bug. This test asserts the correct behaviour, so it stays red until the bug is fixed.
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79900")]
    public async Task UpdateUserStatus_DocSpaceAdminDeactivatesPeerDocSpaceAdmin_ReturnsAccessDenied()
    {
        await _peopleClient.Authenticate(Owner);
        var target = await InviteContact(EmployeeType.DocSpaceAdmin);
        var actor = await InviteContact(EmployeeType.DocSpaceAdmin);

        var request = new UpdateMembersRequestDto([target.Id], false);

        await _peopleClient.Authenticate(actor);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    // Same bug as above: a DocSpace admin should never be able to deactivate the portal owner.
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79900")]
    public async Task UpdateUserStatus_DocSpaceAdminDeactivatesOwner_ReturnsAccessDenied()
    {
        await _peopleClient.Authenticate(Owner);
        var ownerProfile = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
        var actor = await InviteContact(EmployeeType.DocSpaceAdmin);

        var request = new UpdateMembersRequestDto([ownerProfile.Response.Id], false);

        await _peopleClient.Authenticate(actor);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// Every row is an actor without permission to call this endpoint at all: a RoomAdmin, a User
    /// and a Guest are each denied regardless of who the target is (a peer, an admin, the owner) or
    /// which direction the status change goes.
    /// </summary>
    public static TheoryData<EmployeeType, EmployeeType[], bool, EmployeeStatus> DeniedActorCases()
    {
        var data = new TheoryData<EmployeeType, EmployeeType[], bool, EmployeeStatus>
        {
            { EmployeeType.RoomAdmin, [EmployeeType.Guest, EmployeeType.User, EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.RoomAdmin, [EmployeeType.Guest, EmployeeType.User, EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Active },
            { EmployeeType.RoomAdmin, [EmployeeType.RoomAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.RoomAdmin, [EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.RoomAdmin, [], true, EmployeeStatus.Terminated },
            { EmployeeType.User, [], true, EmployeeStatus.Terminated },
            { EmployeeType.User, [EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.User, [EmployeeType.RoomAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.User, [EmployeeType.User], false, EmployeeStatus.Terminated },
            { EmployeeType.Guest, [], true, EmployeeStatus.Terminated },
            { EmployeeType.Guest, [EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.Guest, [EmployeeType.RoomAdmin], false, EmployeeStatus.Terminated },
            { EmployeeType.Guest, [EmployeeType.User], false, EmployeeStatus.Terminated },
            { EmployeeType.User, [EmployeeType.Guest, EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Active },
            { EmployeeType.Guest, [EmployeeType.User, EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin], false, EmployeeStatus.Active }
        };

        return data;
    }

    [Theory]
    [MemberData(nameof(DeniedActorCases))]
    public async Task UpdateUserStatus_ByNonPrivilegedActor_ReturnsAccessDenied(
        EmployeeType actorType, EmployeeType[] targetTypes, bool includeOwnerTarget, EmployeeStatus action)
    {
        await _peopleClient.Authenticate(Owner);

        var targetIds = new List<Guid>();
        foreach (var targetType in targetTypes)
        {
            var target = await InviteMember(targetType);
            targetIds.Add(target.Id);
        }

        if (includeOwnerTarget)
        {
            var ownerProfile = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
            targetIds.Add(ownerProfile.Response.Id);
        }

        var request = new UpdateMembersRequestDto(targetIds, false);

        if (action == EmployeeStatus.Active)
        {
            await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken);
        }

        var actor = await InviteMember(actorType);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _userStatusApi.UpdateUserStatusAsync(action, request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }
}
