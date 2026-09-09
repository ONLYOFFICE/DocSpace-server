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
/// Core, happy-path coverage of <c>POST /files/rooms/{id}/resend</c>: resending to one or many
/// pending invitees, batches mixing members and non-members, idempotency, and membership staying
/// unchanged across room types. Validation and error-path coverage lives in
/// <see cref="RoomInvitationTests"/>; permission coverage lives in
/// <see cref="Permissions.RoomResendPermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInvitationResendTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Invites <paramref name="count"/> new, still-pending users into a fresh CustomRoom with the
    /// given access and returns the room together with the invited users.
    /// </summary>
    private async Task<(FolderDtoInteger Room, List<User> Users)> CreateRoomWithInvitedUsers(
        int count, FileShare access = FileShare.Editing)
    {
        var room = await CreateCustomRoom("Autotest Resend Room");

        var users = new List<User>();
        for (var i = 0; i < count; i++)
        {
            var user = await InviteMember(EmployeeType.User);
            await InviteToRoom(room.Id, user, access);
            users.Add(user);
        }

        return (room, users);
    }

    /// <summary>
    /// Invites a member and authenticates as them once, which is what turns a pending invite into
    /// an active portal account - the same distinction the resend endpoint treats differently.
    /// </summary>
    private async Task<User> InviteActiveMember(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);
        await _filesClient.Authenticate(Owner);

        return member;
    }

    /// <summary>Snapshots the room's invite/member records as a stable, comparable list.</summary>
    private async Task<List<(Guid? Id, FileShare? Access)>> ReadRoomMembers(int roomId)
    {
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return shares
            .Select(s => (Id: s.SharedToUser?.Id, Access: s.Access))
            .OrderBy(s => s.Id)
            .ToList();
    }

    [Fact]
    public async Task ResendInvitations_ToInvitedUser_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Resend Room");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        // Act & Assert - the call completes without throwing
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [user.Id]), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_ToUserNotInRoom_SilentlySkippedAndSucceeds()
    {
        // Arrange - a batch operation: non-member ids are silently skipped by design
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Resend Room");
        var outsider = await InviteMember(EmployeeType.User);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [outsider.Id]), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_ResendAll_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, _) = await CreateRoomWithInvitedUsers(3);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_ResendAllWithUsersIds_UsersIdsIgnored()
    {
        // Arrange - resendAll drives a bulk resend; usersIds is ignored, not validated against
        await _filesClient.Authenticate(Owner);
        var (room, users) = await CreateRoomWithInvitedUsers(2);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [users[0].Id], resendAll: true), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_BatchWithMemberAndNonMember_Succeeds()
    {
        // Arrange - a real, existing user who is NOT a member of the room is silently skipped
        await _filesClient.Authenticate(Owner);
        var (room, users) = await CreateRoomWithInvitedUsers(1);
        var outsider = await InviteMember(EmployeeType.User);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [users[0].Id, outsider.Id]), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_Repeated_BothCallsSucceed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, users) = await CreateRoomWithInvitedUsers(1);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [users[0].Id]), TestContext.Current.CancellationToken);
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [users[0].Id]), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_ToAlreadyActiveMember_IsNoOp()
    {
        // Arrange - an authenticated member already has an active account, not a pending invite
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Resend Active Member");
        var member = await InviteActiveMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: [member.Id]), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_ToSeveralPendingUsers_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, users) = await CreateRoomWithInvitedUsers(3);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(usersIds: users.ConvertAll(u => u.Id)), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResendInvitations_DoesNotChangeRoomMembership()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, users) = await CreateRoomWithInvitedUsers(2);
        var before = await ReadRoomMembers(room.Id);

        // Act
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken);

        // Assert - same set of members, same access, no duplicate invite records
        var after = await ReadRoomMembers(room.Id);
        foreach (var user in users)
        {
            after.Count(m => m.Id == user.Id).Should().Be(1);
        }

        after.Should().Equal(before);
    }

    [Fact]
    public async Task ResendInvitations_ResendAll_DoesNotAffectAlreadyAcceptedMember()
    {
        // Arrange - accepted (active) member + pending (just-created) member. The pending member is
        // created first: authenticating a member shares its session on the request context, which
        // would make a later direct InviteMember call run as that low-privilege user and fail with 403.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Resend Accepted Mix");
        var pending = await InviteMember(EmployeeType.User);
        var accepted = await InviteActiveMember(EmployeeType.User);

        await InviteToRoom(room.Id, accepted, FileShare.Editing);
        await InviteToRoom(room.Id, pending, FileShare.Read);

        // Act
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken);

        // Assert - the accepted member is still present exactly once with unchanged access
        var after = await ReadRoomMembers(room.Id);
        var acceptedEntries = after.Where(m => m.Id == accepted.Id).ToList();
        acceptedEntries.Should().ContainSingle();
        acceptedEntries[0].Access.Should().Be(FileShare.Editing);
    }

    /// <summary>
    /// Access levels not tied to a specific room type: <c>ContentCreator</c> is the only level a
    /// regular user can be granted in every room type, which is what lets one theory exercise the
    /// endpoint across all of them (PublicRoom does not accept <c>Read</c> for a user subject - see
    /// <c>FileSecurity.AvailableRoomAccesses</c>).
    /// </summary>
    [Theory]
    [InlineData(RoomType.PublicRoom)]
    [InlineData(RoomType.FillingFormsRoom)]
    [InlineData(RoomType.EditingRoom)]
    [InlineData(RoomType.VirtualDataRoom)]
    public async Task ResendInvitations_AcrossRoomTypes_Succeeds(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto($"Autotest Resend {roomType}", roomType: roomType),
            TestContext.Current.CancellationToken)).Response;

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        // Act & Assert
        await _roomsApi.ResendEmailInvitationsAsync(
            room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken);
    }
}
