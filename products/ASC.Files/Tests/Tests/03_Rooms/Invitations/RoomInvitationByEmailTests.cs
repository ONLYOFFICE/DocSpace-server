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

namespace ASC.Files.Tests.Tests._03_Rooms.Invitations;

/// <summary>
/// <c>PUT /files/rooms/{id}/share</c> with an email address instead of an account id. What the
/// server records is checked; whether a letter went out is not visible here.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInvitationByEmailTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private static RoomInvitationRequest ByEmail(string email) => new()
    {
        Invitations = [new RoomInvitation { Email = email, Access = FileShare.Read }],
        Notify = false
    };

    private async Task<FileShareDto?> MemberOfRoomAsync(int roomId, Guid userId)
    {
        var shares = await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);

        return shares.Response.FirstOrDefault(s => s.SharedToUser?.Id == userId);
    }

    [Fact]
    public async Task SetRoomSecurity_UnknownAddress_CreatesPendingGuestWithAccess()
    {
        await _filesClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");
        var email = Initializer.Faker.Internet.Email();

        await _roomsApi.SetRoomSecurityAsync(room.Id, ByEmail(email), cancellationToken: TestContext.Current.CancellationToken);

        var profile = (await _profilesApi.GetProfileByEmailAsync(email: email, cancellationToken: TestContext.Current.CancellationToken)).Response;
        profile.IsVisitor.Should().BeTrue();
        profile.Status.Should().Be(EmployeeStatus.Pending);
        (await MemberOfRoomAsync(room.Id, profile.Id))!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task SetRoomSecurity_AddressOfPendingAccount_GrantsAccessToThatAccount()
    {
        await _filesClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");
        var email = Initializer.Faker.Internet.Email();
        var invited = await _profilesApi.InviteUsersAsync(new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]), TestContext.Current.CancellationToken);
        var pendingId = invited.Response.Single(u => u.DisplayName == email).Id;

        await _roomsApi.SetRoomSecurityAsync(room.Id, ByEmail(email), cancellationToken: TestContext.Current.CancellationToken);

        (await MemberOfRoomAsync(room.Id, pendingId)).Should().NotBeNull();
    }

    [Fact]
    public async Task SetRoomSecurity_AddressOfActiveAccount_GrantsAccessAndKeepsItActive()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await _peopleClient.Authenticate(Owner);

        await _roomsApi.SetRoomSecurityAsync(room.Id, ByEmail(user.Email), cancellationToken: TestContext.Current.CancellationToken);

        (await MemberOfRoomAsync(room.Id, user.Id)).Should().NotBeNull();
        var profile = await _profilesApi.GetProfileByEmailAsync(email: user.Email, cancellationToken: TestContext.Current.CancellationToken);
        profile.Response.Status.Should().Be(EmployeeStatus.Active);
    }

    [Fact]
    public async Task SetRoomSecurity_GuestInvitationsDisabled_RefusesWholeRequestWith403()
    {
        await _filesClient.Authenticate(Owner);
        await _webApiClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await _commonSettingsApi.UpdateInvitationSettingsAsync(new TenantUserInvitationSettingsRequestDto(allowInvitingMembers: true, allowInvitingGuests: false), TestContext.Current.CancellationToken);
        var request = new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user.Id, Access = FileShare.Read },
                new RoomInvitation { Email = Initializer.Faker.Internet.Email(), Access = FileShare.Read }
            ],
            Notify = false
        };

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, request, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await MemberOfRoomAsync(room.Id, user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task SetRoomSecurity_MalformedAddress_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, ByEmail("not-an-address"), cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SetRoomSecurity_AddressOver255Characters_ReturnsBadRequest()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Email Invite Room");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.SetRoomSecurityAsync(room.Id, ByEmail(new string('a', 250) + "@example.com"), cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
