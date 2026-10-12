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
/// <c>PUT /people/invite</c> — resending the activation email to a pending member. Whoever may
/// invite a given employee type may also resend its activation email; the check is the same one
/// <see cref="InviteUsersTests"/> exercises on the invite itself.
/// </summary>
public class ResendActivationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?, EmployeeType> ResenderAndInvitedType = new()
    {
        { null, EmployeeType.DocSpaceAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.RoomAdmin, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(ResenderAndInvitedType))]
    public async Task ResendUserInvites_ByAllowedInviter_ShouldResendToPendingMember(EmployeeType? resenderType, EmployeeType invitedType)
    {
        var resender = resenderType is null ? Owner : await InviteContact(resenderType.Value);
        await _peopleClient.Authenticate(resender);

        var email = Initializer.Faker.Internet.Email();
        var invited = await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = invitedType, Email = email }]), TestContext.Current.CancellationToken);
        var invitedUserId = invited.Response.Single(u => u.DisplayName == email).Id;

        var resent = await _profilesApi.ResendUserInvitesAsync(
            new UpdateMembersRequestDto([invitedUserId]),
            TestContext.Current.CancellationToken);

        var resentUser = resent.Response.Single();
        resentUser.Id.Should().Be(invitedUserId);
        resentUser.Email.Should().Be(email);
        resentUser.HasAvatar.Should().BeFalse();
        resentUser.IsAnonim.Should().BeFalse();
        resentUser.Status.Should().Be(EmployeeStatus.Pending);
        resentUser.ActivationStatus.Should().Be(EmployeeActivationStatus.Pending);
    }

    [Fact]
    public async Task ResendUserInvites_ByGuest_ShouldThrowAccessDenied()
    {
        var email = Initializer.Faker.Internet.Email();
        var invited = await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.RoomAdmin, Email = email }]), TestContext.Current.CancellationToken);
        var invitedUserId = invited.Response.Single(u => u.DisplayName == email).Id;

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.ResendUserInvitesAsync(
                new UpdateMembersRequestDto([invitedUserId]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79545")]
    public async Task ResendUserInvites_ByUser_ShouldThrowAccessDenied()
    {
        var email = Initializer.Faker.Internet.Email();
        var invited = await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]), TestContext.Current.CancellationToken);
        var invitedUserId = invited.Response.Single(u => u.DisplayName == email).Id;

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.ResendUserInvitesAsync(
                new UpdateMembersRequestDto([invitedUserId]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task ResendUserInvites_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.ResendUserInvitesAsync(
                new UpdateMembersRequestDto([]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
