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
/// <c>POST /people/reassign/start</c> - only the Owner and a DocSpaceAdmin may start reassigning a
/// deactivated member's rooms/files to another member, and only from a deactivated source.
/// </summary>
public class ReassignStartTests(AspireAppFixture fixture) : UserDataTestBase(fixture)
{
    [Fact]
    public async Task StartReassign_AsOwner_FromDeactivatedRoomAdminToSelf_StartsRunning()
    {
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Reassign Room", "Autotest Reassign File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(Owner);
        var result = await _userDataApi.StartReassignAsync(
            new StartReassignRequestDto(roomAdmin.Id, Owner.Id),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Response.IsCompleted.Should().BeFalse();
        result.Response.Percentage.Should().BeGreaterThanOrEqualTo(0);
        result.Response.Error.Should().BeEmpty();
    }

    [Fact]
    public async Task StartReassign_AsDocSpaceAdmin_FromDeactivatedRoomAdminToSelf_StartsRunning()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var (roomAdmin, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Reassign Room", "Autotest Reassign File");
        await TerminateUser(roomAdmin);

        await _peopleClient.Authenticate(admin);
        var result = await _userDataApi.StartReassignAsync(
            new StartReassignRequestDto(roomAdmin.Id, admin.Id),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Response.IsCompleted.Should().BeFalse();
        result.Response.Percentage.Should().BeGreaterThanOrEqualTo(0);
        result.Response.Error.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task StartReassign_AsNonAdminRole_ThrowsAccessDenied(EmployeeType type)
    {
        var (target, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Reassign Room", "Autotest Reassign File");
        await TerminateUser(target);

        var actor = await InviteMember(type);
        await _peopleClient.Authenticate(actor);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(
                new StartReassignRequestDto(target.Id, actor.Id),
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task StartReassign_FromActiveUser_ThrowsBadRequest()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(
                new StartReassignRequestDto(roomAdmin.Id, Owner.Id),
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Can not reassign data");
    }

    /// <summary>
    /// The destination of a reassignment must be an active Room admin or DocSpace admin: a DocSpace admin
    /// reassigning a disabled Room admin's data to a User, a Guest or a disabled Room admin gets 400, an
    /// active Room admin is accepted. <paramref name="targetType"/> null is a disabled Room admin.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.User, false)]
    [InlineData(EmployeeType.Guest, false)]
    [InlineData(null, false)]
    [InlineData(EmployeeType.RoomAdmin, true)]
    public async Task StartReassign_AsDocSpaceAdmin_ByTargetType_OnlyActiveAdminAccepted(EmployeeType? targetType, bool accepted)
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var (source, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.RoomAdmin, "Autotest Reassign Room", "Autotest Reassign File");
        await TerminateUser(source);

        var target = await InviteMember(targetType ?? EmployeeType.RoomAdmin);

        if (targetType is null)
        {
            await TerminateUser(target);
        }

        await _peopleClient.Authenticate(admin);
        var request = new StartReassignRequestDto(source.Id, target.Id);

        if (accepted)
        {
            var result = await _userDataApi.StartReassignAsync(request, cancellationToken: TestContext.Current.CancellationToken);
            result.Response.Error.Should().BeEmpty();
            return;
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(request, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Can not reassign data to user");
    }

    /// <summary>
    /// Only the portal owner reassigns the data of a disabled DocSpace admin: a DocSpace admin gets 400
    /// ("Can not reassign data from user"), the owner is accepted.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartReassign_FromDisabledDocSpaceAdmin_OnlyOwnerAccepted(bool asOwner)
    {
        var caller = asOwner ? Owner : await InviteContact(EmployeeType.DocSpaceAdmin);
        var (source, _) = await CreateActorWithRoomAndFileAsync(EmployeeType.DocSpaceAdmin, "Autotest Reassign Admin Room", "Autotest Reassign Admin File");
        await TerminateUser(source);

        await _peopleClient.Authenticate(caller);
        var request = new StartReassignRequestDto(source.Id, caller.Id);

        if (asOwner)
        {
            var result = await _userDataApi.StartReassignAsync(request, cancellationToken: TestContext.Current.CancellationToken);
            result.Response.Error.Should().BeEmpty();
            return;
        }

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(request, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Can not reassign data from user");
    }

    [Fact]
    public async Task StartReassign_Anonymous_ThrowsUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userDataApi.StartReassignAsync(
                new StartReassignRequestDto(Guid.Empty, Guid.Empty),
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
