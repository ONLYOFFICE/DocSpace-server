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
/// Validation and error-path coverage of <c>POST /files/rooms/{id}/resend</c>: no-op request
/// bodies, malformed user ids, and invalid/inaccessible room ids. Core, happy-path behavior lives
/// in <see cref="RoomInvitationResendTests"/>; permission coverage lives in
/// <see cref="Permissions.RoomResendPermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomInvitationTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Invites <paramref name="count"/> new, still-pending users into a fresh CustomRoom with
    /// Editing access and returns the room together with the invited users.
    /// </summary>
    private async Task<(FolderDtoInteger Room, List<User> Users)> CreateRoomWithInvitedUsers(int count)
    {
        var room = await CreateCustomRoom("Autotest Resend Room");

        var users = new List<User>();
        for (var i = 0; i < count; i++)
        {
            var user = await InviteMember(EmployeeType.User);
            await InviteToRoom(room.Id, user, FileShare.Editing);
            users.Add(user);
        }

        return (room, users);
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

    /// <summary>
    /// Sends a raw POST /api/2.0/files/rooms/{id}/resend with an arbitrary JSON body, bypassing the
    /// typed SDK: <see cref="UserInvitation.UsersIds"/> is a <c>List&lt;Guid&gt;</c>, which cannot
    /// carry a malformed or non-numeric id string.
    /// </summary>
    private async Task<HttpResponseMessage> SendRawResend(int roomId, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/2.0/files/rooms/{roomId}/resend")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Every one of these bodies is a no-op that must not touch the invite records: the TS suite's
    /// "empty body {}" and "usersIds: null" variants are dropped here because
    /// <see cref="UserInvitation.UsersIds"/> is serialised with <c>EmitDefaultValue = true</c> - a
    /// default-constructed request already puts <c>"usersIds": null</c> on the wire, so both
    /// collapse into the same typed call as "resendAll:false without usersIds".
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResendInvitations_NoOpBody_LeavesInvitesUntouched(bool emptyUsersIds)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, _) = await CreateRoomWithInvitedUsers(2);
        var before = await ReadRoomMembers(room.Id);

        var request = emptyUsersIds ? new UserInvitation(usersIds: []) : new UserInvitation();

        // Act
        await _roomsApi.ResendEmailInvitationsAsync(room.Id, request, TestContext.Current.CancellationToken);

        // Assert - a no-op must not touch the invite records: same members, same access levels,
        // same count (no new/duplicated/dropped pending invites)
        var after = await ReadRoomMembers(room.Id);
        after.Should().Equal(before);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("999999999")]
    public async Task ResendInvitations_MalformedUserId_ReturnsBadRequest(string badId)
    {
        // Arrange - a malformed or non-existent user id is rejected with 400. Contrast with a real,
        // existing user who is simply not a room member: that is silently skipped and returns 200
        // (see RoomInvitationResendTests.ResendInvitations_ToUserNotInRoom_SilentlySkippedAndSucceeds).
        await _filesClient.Authenticate(Owner);
        var (room, _) = await CreateRoomWithInvitedUsers(1);

        // Act
        using var response = await SendRawResend(room.Id, $"{{\"usersIds\":[\"{badId}\"]}}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Bug 81879: an id that resolves to no room reported 403 "You don't have enough permission to
    /// perform the operation". The room lookup did guard against a missing folder, but the guard was
    /// applied to the Task rather than to its result and never fired — fixed in
    /// <c>FileStorageService.ResendEmailInvitationsAsync</c>.
    ///
    /// Asserts 404 rather than the 400 the TypeScript suite asked for: 0 and -1 resolve to nothing
    /// exactly like an unknown id does, and the whole <c>rooms/{id}</c> family answers 404 for all of
    /// them (see <c>Pin.RoomPinValidationTests</c> and <c>Read.RoomInfoValidationTests</c>).
    /// </summary>
    [Trait("Bug", "81879")]
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ResendInvitations_IncorrectRoomId_ReturnsNotFound(int id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>A well-formed room id that does not exist should return 404, but the endpoint returns 403.</summary>
    [Trait("Bug", "81880")]
    [Fact]
    public async Task ResendInvitations_NonExistentRoomId_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                999999999, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>A deleted room no longer exists and should return 404, but the endpoint returns 403 (same masking as the non-existent case).</summary>
    [Trait("Bug", "81880")]
    [Fact]
    public async Task ResendInvitations_DeletedRoom_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, _) = await CreateRoomWithInvitedUsers(1);

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task ResendInvitations_ArchivedRoom_ReturnsForbidden()
    {
        // Arrange - archived rooms reject mutations: 403 is the intended response here (unlike the
        // bad-id/deleted cases above)
        await _filesClient.Authenticate(Owner);
        var (room, _) = await CreateRoomWithInvitedUsers(1);
        await ArchiveRoom(room.Id);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.ResendEmailInvitationsAsync(
                room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You don't have enough permission to perform the operation");
    }
}
