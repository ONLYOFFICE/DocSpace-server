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

namespace ASC.People.Tests.Tests._01_Profiles;

/// <summary>
/// <c>PUT /people/delete</c> — batch removal of already-deactivated members. Distinct from
/// <see cref="DeleteMemberTests"/> (single-member <c>DELETE /people/:userIds</c>): both share the
/// "target must be suspended first" rule and the "only Owner/DocSpace admin may call this" rule.
/// </summary>
public class RemoveUsersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?> BatchRemovers = new() { (EmployeeType?)null, EmployeeType.DocSpaceAdmin };

    [Theory]
    [MemberData(nameof(BatchRemovers))]
    public async Task RemoveUsers_ByOwnerOrDocSpaceAdmin_ShouldRemoveBothDeactivatedMembers(EmployeeType? removerType)
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var user = await InviteContact(EmployeeType.User);
        var request = new UpdateMembersRequestDto([roomAdmin.Id, user.Id], resendAll: false);

        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken);

        await _peopleClient.Authenticate(removerType is null ? Owner : await InviteContact(removerType.Value));

        var removed = await _profilesApi.RemoveUsersAsync(request, TestContext.Current.CancellationToken);

        removed.Response.Should().HaveCount(2);
        removed.Response[0].Id.Should().Be(roomAdmin.Id);
        removed.Response[0].Status.Should().Be(EmployeeStatus.Terminated);
        removed.Response[0].IsRoomAdmin.Should().BeTrue();
        removed.Response[1].Id.Should().Be(user.Id);
        removed.Response[1].Status.Should().Be(EmployeeStatus.Terminated);
        removed.Response[1].IsCollaborator.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79560")]
    public async Task RemoveUsers_OwnerRemovesNonDeactivatedUser_ShouldThrowNotSuspended()
    {
        var user = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.RemoveUsersAsync(
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("not suspended");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79876")]
    public async Task RemoveUsers_OwnerRemovesNonDeactivatedBatch_ShouldThrowNotSuspended()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.RemoveUsersAsync(
                new UpdateMembersRequestDto([docSpaceAdmin.Id, roomAdmin.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("not suspended");
    }

    public static readonly TheoryData<EmployeeType?> NonAdminRemovers = new() { EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest };

    [Theory]
    [MemberData(nameof(NonAdminRemovers))]
    public async Task RemoveUsers_ByNonAdmin_ShouldThrowAccessDenied(EmployeeType? removerType)
    {
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);
        var request = new UpdateMembersRequestDto([user1.Id, user2.Id], resendAll: false);

        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, request, TestContext.Current.CancellationToken);

        var remover = removerType == EmployeeType.Guest ? await InviteGuest() : await InviteContact(removerType!.Value);
        await _peopleClient.Authenticate(remover);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.RemoveUsersAsync(request, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task RemoveUsers_Anonymous_ShouldThrowUnauthorized()
    {
        var user = await InviteContact(EmployeeType.User);
        await TerminateUser(user);

        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.RemoveUsersAsync(
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// Goes over raw HTTP: <see cref="UpdateMembersRequestDto"/> can only ever serialize the
    /// correct <c>userIds</c> field name, so this malformed body (<c>userId</c> instead) cannot
    /// be produced through the typed SDK.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80048")]
    public async Task RemoveUsers_BodyWithWrongFieldName_ShouldReturnBadRequestNotServerError()
    {
        var user = await InviteContact(EmployeeType.User);
        await TerminateUser(user);

        var rawApi = new RawApiClient(_peopleClient);
        using var response = await rawApi.PutAsync(
            "api/2.0/people/delete",
            new { userId = new[] { user.Id } },
            TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().Be(400);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Value cannot be null. (Parameter 'inDto?.UserIds')");
    }
}
