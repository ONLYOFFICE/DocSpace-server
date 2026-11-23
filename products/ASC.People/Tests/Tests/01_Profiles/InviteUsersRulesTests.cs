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
/// The rules of <c>POST /people/invite</c> and <c>PUT /people/invite</c> that depend on who is asking
/// and on the state of the target account. Whether a letter went out is not visible here.
/// </summary>
public class InviteUsersRulesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private static InviteUsersRequestDto Invite(string email, EmployeeType type) =>
        new([new UserInvitationRequestDto { Type = type, Email = email }]);

    [Fact]
    public async Task InviteUsers_AddressOfDisabledAccount_ReturnsBadRequest()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.InviteUsersAsync(Invite(user.Email, EmployeeType.User), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task InviteUsers_AddressOfExistingUser_WithHigherType_RaisesTypeToRoomAdmin()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);

        await _profilesApi.InviteUsersAsync(Invite(user.Email, EmployeeType.RoomAdmin), TestContext.Current.CancellationToken);

        var profile = await _profilesApi.GetProfileByUserIdAsync(user.Id.ToString(), TestContext.Current.CancellationToken);
        profile.Response.IsRoomAdmin.Should().BeTrue();
    }

    [Fact]
    public async Task InviteUsers_Answer_ListsEarlierPendingProfilesToo()
    {
        var first = Initializer.Faker.Internet.Email();
        var second = Initializer.Faker.Internet.Email();
        await _peopleClient.Authenticate(Owner);

        await _profilesApi.InviteUsersAsync(Invite(first, EmployeeType.User), TestContext.Current.CancellationToken);
        var answer = await _profilesApi.InviteUsersAsync(Invite(second, EmployeeType.User), TestContext.Current.CancellationToken);

        answer.Response.Select(u => u.DisplayName).Should().Contain([first, second]);
    }

    [Fact]
    public async Task InviteUsers_MembersInvitationsDisabled_OwnerGetsForbidden()
    {
        await _webApiClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        await _commonSettingsApi.UpdateInvitationSettingsAsync(new TenantUserInvitationSettingsRequestDto(allowInvitingMembers: false, allowInvitingGuests: true), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.InviteUsersAsync(Invite(Initializer.Faker.Internet.Email(), EmployeeType.User), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ResendUserInvites_ResendAll_ByUser_ReturnsForbidden()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.ResendUserInvitesAsync(new UpdateMembersRequestDto([], true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ResendUserInvites_ResendAll_ByGuest_ReturnsForbidden()
    {
        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _profilesApi.ResendUserInvitesAsync(new UpdateMembersRequestDto([], true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ResendUserInvites_ResendAll_ByRoomAdmin_IsAllowed()
    {
        var email = Initializer.Faker.Internet.Email();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.InviteUsersAsync(Invite(email, EmployeeType.User), TestContext.Current.CancellationToken);
        await _peopleClient.Authenticate(roomAdmin);

        var answer = await _profilesApi.ResendUserInvitesAsync(new UpdateMembersRequestDto([], true), TestContext.Current.CancellationToken);

        answer.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task ResendUserInvites_OwnIdByActiveUser_LeavesAccountActive()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var answer = await _profilesApi.ResendUserInvitesAsync(new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);

        answer.Response.Single(u => u.Id == user.Id).Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public async Task ResendUserInvites_DisabledAccount_LeavesItTerminated()
    {
        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);

        var answer = await _profilesApi.ResendUserInvitesAsync(new UpdateMembersRequestDto([user.Id], false), TestContext.Current.CancellationToken);

        answer.Response.Single(u => u.Id == user.Id).Status.Should().Be(EmployeeStatus.Terminated);
    }
}
