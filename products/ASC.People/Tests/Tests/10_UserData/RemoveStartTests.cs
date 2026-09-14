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

namespace ASC.People.Tests.Tests._10_UserData;

/// <summary>
/// <c>POST /people/remove/start</c> - only the Owner and a DocSpaceAdmin may start removing a
/// deactivated member's data, and doing so hands any room the member owned over to the Owner.
/// The hand-over used to be invisible: the room's own <c>CreateBy</c> was reassigned, but the API
/// reported <c>OwnedBy</c> from the creator's virtualrooms root, which reassignment never touches.
/// </summary>
public class RemoveStartTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    public static TheoryData<EmployeeType> RoomOwnerRoles => new() { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin };

    [Theory]
    [MemberData(nameof(RoomOwnerRoles))]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80714")]
    public async Task StartRemove_ForDeactivatedRoomOwner_TransfersRoomOwnershipToOwner(EmployeeType type)
    {
        var roomTitle = $"Autotest Remove Owner Room {type}";
        var (actor, _) = await CreateActorWithRoomAndFileAsync(type, roomTitle, "Autotest Remove Owner File");
        var actorProfile = await GetProfileAsync(actor.Id);
        var ownerProfile = await GetProfileAsync(Owner.Id);

        await TerminateUser(actor);

        await _peopleClient.Authenticate(Owner);
        var beforeRemoval = await FindRoomByTitleAsync(roomTitle);
        beforeRemoval.OwnedBy!.Id.Should().Be(actor.Id);
        beforeRemoval.OwnedBy!.DisplayName.Should().Be(actorProfile.DisplayName);

        var start = await _userDataApi.StartRemoveAsync(new TerminateRequestDto(actor.Id), TestContext.Current.CancellationToken);
        start.Response.Status.Should().Be(DistributedTaskStatus.Running);

        await PollUntilAsync(
            async () => (await _userDataApi.GetRemoveProgressAsync(actor.Id, TestContext.Current.CancellationToken)).Response,
            p => p.IsCompleted);

        var afterRemoval = await FindRoomByTitleAsync(roomTitle);
        afterRemoval.OwnedBy!.Id.Should().Be(Owner.Id, "ownership of a deactivated member's room should transfer to the Owner once their data is removed");
        afterRemoval.OwnedBy!.DisplayName.Should().Be(ownerProfile.DisplayName);
    }

    [Fact]
    public async Task StartRemove_AsOwner_ForDeactivatedRoomAdmin_StartsRunning()
    {
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(Owner);
        var result = await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        result.Response.IsCompleted.Should().BeFalse();
        result.Response.Percentage.Should().BeGreaterThanOrEqualTo(0);
        result.Response.Error.Should().BeEmpty();
        result.Response.Status.Should().Be(DistributedTaskStatus.Running);
    }

    [Fact]
    public async Task StartRemove_AsDocSpaceAdmin_ForDeactivatedRoomAdmin_StartsRunning()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(admin);
        var result = await _userDataApi.StartRemoveAsync(new TerminateRequestDto(roomAdmin.Id), TestContext.Current.CancellationToken);

        result.Response.IsCompleted.Should().BeFalse();
        result.Response.Percentage.Should().BeGreaterThanOrEqualTo(0);
        result.Response.Error.Should().BeEmpty();
        result.Response.Status.Should().Be(DistributedTaskStatus.Running);
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task StartRemove_AsNonAdminRole_ThrowsAccessDenied(EmployeeType type)
    {
        var (target, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Remove Room", "Autotest Remove File");
        await TerminateUser(target);

        var actor = await InviteMember(type);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartRemoveAsync(new TerminateRequestDto(target.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task StartRemove_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartRemoveAsync(new TerminateRequestDto(Guid.Empty), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
