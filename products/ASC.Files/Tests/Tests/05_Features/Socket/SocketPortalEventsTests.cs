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

namespace ASC.Files.Tests.Tests._05_Features.Socket;

/// <summary>
/// The events that are not about an entry: users (<c>UserSocketManager</c>, room <c>users</c>) and quota
/// (<c>QuotaSocketManager</c>, room <c>quota</c>).
/// </summary>
[Trait("Category", "Socket")]
public class SocketPortalEventsTests(AspireAppFixture fixture) : SocketTestBase(fixture)
{
    [Fact]
    public async Task InviteUser_ReachesTheUsersRoom()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(false, cancellationToken, "users");

        var user = await InviteContact(EmployeeType.User);

        await socket.WaitForAsync(
            "s:add-user",
            payload => payload.GetProperty("id").GetGuid() == user.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// The events about an account carried its profile, built with the rights of whoever made the change - the account
    /// itself or an administrator - and delivered to everybody listening to the users of the portal. A user so got
    /// the name and the email of accounts it does not work with, and their contacts, location and notes, which the
    /// API does not show to it. The events now carry the id alone: the client re-reads the profile by it.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task AddAndUpdateUser_ReachAUserWithTheIdAlone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var listener = await InviteContact(EmployeeType.User);

        await using var socket = await ConnectSocket(listener);
        await socket.SubscribeAsync(false, cancellationToken, "users");

        var changed = await InviteContact(EmployeeType.User);

        var added = await socket.WaitForAsync(
            "s:add-user",
            payload => payload.GetProperty("id").GetGuid() == changed.Id,
            cancellationToken: cancellationToken);

        // Location, notes and contacts can only be set by the account itself, which sees its own profile in full.
        await _peopleClient.Authenticate(changed);
        var asItself = (await _profilesApi.UpdateMemberAsync(
            changed.Id.ToString(),
            new UpdateMemberRequestDto(location: ProfileLocation, comment: ProfileNotes, contacts: [new Contact("skype", "autotest.contact")]),
            cancellationToken)).Response;

        asItself.Location.Should().Be(ProfileLocation, "whoever made the change sees the profile the event must not carry");
        asItself.Email.Should().NotBeNullOrEmpty();

        var updated = await socket.WaitForAsync(
            "s:update-user",
            payload => payload.GetProperty("id").GetGuid() == changed.Id,
            cancellationToken: cancellationToken);

        PropertiesOf(added.Payload.GetProperty("data")).Should().Equal("id");
        PropertiesOf(updated.Payload.GetProperty("data")).Should().Equal("id");
        updated.Payload.GetProperty("data").GetProperty("id").GetGuid().Should().Be(changed.Id, "the client re-reads the profile by this id");
    }

    /// <summary>
    /// A change of an account refreshes the groups it is in, and that event carried the group with the full profiles
    /// of its manager and of every member, for everybody listening to the groups of the portal. It carries the id of
    /// the group alone now.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79163")]
    public async Task UpdateUserInAGroup_ReachesAUserWithTheGroupIdAlone()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var listener = await InviteContact(EmployeeType.User);
        var member = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto([member.Id], Owner.Id, "Autotest Group"), cancellationToken)).Response;

        await using var socket = await ConnectSocket(listener);
        await socket.SubscribeAsync(false, cancellationToken, "groups");

        await _peopleClient.Authenticate(member);
        var asItself = (await _profilesApi.UpdateMemberAsync(
            member.Id.ToString(),
            new UpdateMemberRequestDto(location: ProfileLocation, comment: ProfileNotes),
            cancellationToken)).Response;

        asItself.Location.Should().Be(ProfileLocation, "the member has a profile the event must not carry");

        var updated = await socket.WaitForAsync(
            "s:update-group",
            payload => payload.GetProperty("id").GetGuid() == group.Id,
            cancellationToken: cancellationToken);

        PropertiesOf(updated.Payload.GetProperty("data")).Should().Equal("id");
    }

    private const string ProfileLocation = "Autotest Location";
    private const string ProfileNotes = "Autotest Notes";

    private static List<string> PropertiesOf(JsonElement data)
    {
        return data.EnumerateObject().Select(p => p.Name).ToList();
    }

    [Fact]
    public async Task ChangeUserType_ReachesThatUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var user = await InviteContact(EmployeeType.User);
        await using var socket = await ConnectSocket(user);

        await _peopleClient.Authenticate(Owner);
        var userTypeApi = new UserTypeApi(_peopleClient, new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') });
        await userTypeApi.UpdateUserTypeAsync(EmployeeType.RoomAdmin, new UpdateMembersRequestDto([user.Id], resendAll: false), cancellationToken);

        await socket.WaitForAsync(
            "s:change-my-type",
            payload => payload.GetProperty("id").GetGuid() == user.Id,
            cancellationToken: cancellationToken);
    }

    [Fact]
    public async Task CreateRoom_ReachesTheQuotaRoom()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _filesClient.Authenticate(Owner);
        await using var socket = await ConnectSocket(Owner);
        await socket.SubscribeAsync(false, cancellationToken, "quota");

        await CreateRoom(new CreateRoomRequestDto("Socket Quota Room", roomType: RoomType.CustomRoom));

        await socket.WaitForAsync(
            "s:change-quota-used-value",
            payload => payload.GetProperty("featureId").GetString() == "room",
            cancellationToken: cancellationToken);
    }
}
