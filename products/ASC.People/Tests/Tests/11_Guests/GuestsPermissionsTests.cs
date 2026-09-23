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

namespace ASC.People.Tests.Tests._11_Guests;

/// <summary>
/// <c>DELETE /people/guests</c> — only the room admin who owns a guest may remove it from
/// their own guest list. Everyone else, including the room admin who invited someone else's
/// guest, is refused.
/// </summary>
public class GuestsPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task DeleteGuests_Owner_Forbidden()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteGuests_DocSpaceAdmin_Forbidden()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(docSpaceAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80628")]
    public async Task DeleteGuests_RoomAdmin_DeactivatedGuestFromAnotherUsersList_Forbidden()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80628")]
    public async Task DeleteGuests_RoomAdmin_GuestFromAnotherUsersList_Forbidden()
    {
        var guest = await InviteGuest();

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteGuests_User_Forbidden()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteGuests_Guest_Forbidden()
    {
        var guest = await InviteGuest();
        await TerminateUser(guest);

        var attacker = await InviteGuest();
        await _peopleClient.Authenticate(attacker);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([guest.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task DeleteGuests_Anonymous_Unauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([Guid.Empty]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// A member invited by email (<c>POST /people/invite</c>) is related to the room admin who invited
    /// them, and that relation is what decides whose guest list they land on once their type is changed
    /// to Guest: the inviter may remove them, another room admin is refused.
    /// </summary>
    [Fact]
    public async Task DeleteGuests_MemberInvitedByEmailThenMadeGuest_OnlyTheInviterOwnsIt()
    {
        var inviter = await InviteContact(EmployeeType.RoomAdmin);
        var anotherRoomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var email = Initializer.Faker.Internet.Email();

        await _peopleClient.Authenticate(inviter);
        var invited = (await _profilesApi.InviteUsersAsync(
            new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]),
            TestContext.Current.CancellationToken)).Response.Single(u => u.DisplayName == email);

        // The conversion is a background task that only the owner or a DocSpace admin may start.
        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(
            new StartUpdateUserTypeDto(EmployeeType.Guest, invited.Id),
            TestContext.Current.CancellationToken);

        var progress = await _02_UserType.UserTypeUpdatePolling.WaitForCompletionAsync(_userTypeApi, invited.Id);
        progress.IsCompleted.Should().BeTrue();
        progress.Error.Should().BeEmpty();

        await _peopleClient.Authenticate(anotherRoomAdmin);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([invited.Id]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");

        await _peopleClient.Authenticate(inviter);
        await _guestsApi.DeleteGuestsAsync(new UpdateMembersRequestDto([invited.Id]), TestContext.Current.CancellationToken);
    }
}
