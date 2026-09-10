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
/// <c>POST /people/invite</c> — inviting a member by email. Unlike <see cref="AddMemberTests"/>
/// this endpoint never accepts <see cref="EmployeeType.Guest"/> as a target: guests are only
/// created through <c>POST /people/active</c> (<see cref="BaseTest.InviteGuest"/>).
/// </summary>
public class InviteUsersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    public static readonly TheoryData<EmployeeType?, EmployeeType> SuccessCases = new()
    {
        // inviter (null = Owner), invited type
        { null, EmployeeType.DocSpaceAdmin },
        { null, EmployeeType.RoomAdmin },
        { null, EmployeeType.User },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.User },
        { EmployeeType.RoomAdmin, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(SuccessCases))]
    public async Task InviteUsers_ByAllowedInviter_ShouldCreatePendingMember(EmployeeType? inviterType, EmployeeType invitedType)
    {
        if (inviterType is not null)
        {
            var inviter = await InviteContact(inviterType.Value);
            await _peopleClient.Authenticate(inviter);
        }

        var email = Initializer.Faker.Internet.Email();

        var invited = await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = invitedType, Email = email }]), TestContext.Current.CancellationToken);

        var invitedUser = invited.Response.Single(u => u.DisplayName == email);
        invitedUser.Id.Should().NotBe(Guid.Empty);
        invitedUser.HasAvatar.Should().BeFalse();
        invitedUser.IsAnonim.Should().BeFalse();
    }

    public static readonly TheoryData<EmployeeType?> InviteGuestForbiddenActors = new()
    {
        (EmployeeType?)null, EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest
    };

    [Theory]
    [MemberData(nameof(InviteGuestForbiddenActors))]
    public async Task InviteUsers_GuestType_ShouldThrowAccessDeniedForEveryone(EmployeeType? inviterType)
    {
        var inviter = inviterType switch
        {
            null => Owner,
            EmployeeType.Guest => await InviteGuest(),
            _ => await InviteContact(inviterType.Value)
        };
        await _peopleClient.Authenticate(inviter);

        var email = Initializer.Faker.Internet.Email();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.Guest, Email = email }]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    public static readonly TheoryData<EmployeeType, EmployeeType> SamePrivilegeForbiddenCases = new()
    {
        // inviter type, invited type of the same or higher privilege
        { EmployeeType.DocSpaceAdmin, EmployeeType.DocSpaceAdmin },
        { EmployeeType.RoomAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.User, EmployeeType.User },
    };

    [Theory]
    [MemberData(nameof(SamePrivilegeForbiddenCases))]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79500")]
    public async Task InviteUsers_SameOrHigherPrivilegeType_ShouldThrowAccessDenied(EmployeeType inviterType, EmployeeType invitedType)
    {
        var inviter = await InviteContact(inviterType);
        await _peopleClient.Authenticate(inviter);

        var email = Initializer.Faker.Internet.Email();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = invitedType, Email = email }]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79527")]
    public async Task InviteUsers_LongEmail_ShouldReturnValidationError()
    {
        var longEmail = new string('a', 260) + "@" + Initializer.Faker.Internet.DomainName();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = longEmail }]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("maximum length of '255'");
    }

    [Fact]
    public async Task InviteUsers_Anonymous_ShouldThrowUnauthorized()
    {
        await _peopleClient.Authenticate(null);
        var email = Initializer.Faker.Internet.Email();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
