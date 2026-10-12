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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// <c>GET /files/rooms/{id}/share</c>: group entries, invitation/external links (must not leak
/// into the security list), id validation and stability of the returned list across repeated
/// calls and unrelated state changes.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareReadStateTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    private async Task<Guid> CreateGroup(Guid managerId, params Guid[] members)
    {
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([.. members], managerId, $"Autotest Share Group {Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        return group.Id;
    }

    [Fact]
    public async Task GetRoomSecurityInfo_SharedGroup_AppearsInSecurityInfo()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var groupId = await CreateGroup(user.Id, user.Id);
        var room = await CreateCustomRoom("Autotest Share Group Visible");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = groupId, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().Contain(s => s.SharedToGroup != null && s.SharedToGroup.Id == groupId);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_GroupAccessLevel_IsReturned()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var groupId = await CreateGroup(user.Id, user.Id);
        var room = await CreateCustomRoom("Autotest Share Group Access Level");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = groupId, Access = FileShare.Editing }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entry = info.Find(s => s.SharedToGroup?.Id == groupId);
        entry.Should().NotBeNull();
        entry!.Access.Should().Be(FileShare.Editing);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_RemovingGroupAccess_RemovesTheGroup()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var groupId = await CreateGroup(user.Id, user.Id);
        var room = await CreateCustomRoom("Autotest Share Group Removed");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = groupId, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = groupId, Access = FileShare.None }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().NotContain(s => s.SharedToGroup != null && s.SharedToGroup.Id == groupId);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_InvitationLinks_AreNotReturnedInSecurityInfo()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share No Invitation Link");

        var link = (await _roomsApi.SetRoomLinkAsync(room.Id, new RoomLinkRequest(
            access: FileShare.Read,
            linkType: LinkType.Invitation,
            title: "Autotest Invitation Link For Share Check",
            denyDownload: false), TestContext.Current.CancellationToken)).Response;

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
        info.Should().NotContain(s => s.SharedLink != null && s.SharedLink.Id == link.SharedLink.Id);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_PublicRoomAutoCreatedExternalLink_IsNotReturnedInSecurityInfo()
    {
        // Arrange
        var room = await CreatePublicRoom("Autotest Share No External Link");

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(1);
        info[0].IsOwner.Should().BeTrue();
        info[0].SharedLink.Should().BeNull();
    }

    [Fact]
    public async Task GetRoomSecurityInfo_NonExistingId_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_DeletedRoom_Returns404()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Share Deleted Room");
        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_IdMinusOne_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_IdZero_Returns404()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomSecurityInfoAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <summary>
    /// The typed SDK's <c>id</c> parameter is an <c>int</c>, so a non-numeric route segment cannot
    /// be produced through it; this goes over raw HTTP instead.
    /// </summary>
    private async Task<HttpResponseMessage> GetRoomSecurityInfoRaw(string id)
    {
        return await _filesClient.GetAsync($"api/2.0/files/rooms/{id}/share", TestContext.Current.CancellationToken);
    }

    /// <remarks>
    /// BUG 81790 confirmed by design: a room id is not always numeric — a thirdparty-storage room
    /// can have a string id — so the API accepts any string shape as a potential id and resolves it
    /// as "not found" -&gt; 404, not a 400 validation error. Same reasoning as bug 81703 on
    /// <c>DELETE /files/rooms/:id/tags</c> (<see cref="Tags.RoomTagDetachValidationTests.DeleteRoomTags_InvalidStringRoomId_NotFound"/>).
    /// </remarks>
    [Fact]
    [Trait("Bug", "81790")]
    public async Task GetRoomSecurityInfo_StringRoomId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetRoomSecurityInfoRaw("abc");

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }

    /// <summary>
    /// Other invalid id shapes not covered by the thirdparty-string reasoning above (a non-integer
    /// number, a special-character string) still resolve as "not found" -&gt; 404, same contract as
    /// the string case.
    /// </summary>
    [Theory]
    [InlineData("1.5")]
    [InlineData("!@#$%")]
    public async Task GetRoomSecurityInfo_OtherInvalidIdShape_Returns404(string badId)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetRoomSecurityInfoRaw(badId);

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_EmptyStringRoomId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await GetRoomSecurityInfoRaw(string.Empty);

        // Assert
        ((int)response.StatusCode).Should().Be(404);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_RepeatedGet_ReturnsTheSameSecurityList()
    {
        // Arrange
        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Repeated Stable");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user1.Id, Access = FileShare.Read },
                new RoomInvitation { Id = user2.Id, Access = FileShare.Editing }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var first = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        static List<string> Summary(List<FileShareDto> entries) => entries
            .ConvertAll(s => $"{s.SharedToUser?.Id ?? s.SharedToGroup?.Id}:{s.Access}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        Summary(first).Should().Equal(Summary(second));
    }

    [Fact]
    public async Task GetRoomSecurityInfo_ReflectsTheLatestSetRoomSecurityState()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Latest State");
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.Editing);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Find(s => s.SharedToUser?.Id == user.Id)!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_ReGrantingTheSameAccess_DoesNotDuplicateTheEntry()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Same Access No Dup");
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entries = info.FindAll(s => s.SharedToUser?.Id == user.Id);
        entries.Should().HaveCount(1);
        entries[0].Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_UserEntry_FoundBySharedToUserIdRegardlessOfOrder()
    {
        // Arrange
        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Order Independent");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user1.Id, Access = FileShare.Editing },
                new RoomInvitation { Id = user2.Id, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Find(s => s.SharedToUser?.Id == user1.Id)!.Access.Should().Be(FileShare.Editing);
        info.Find(s => s.SharedToUser?.Id == user2.Id)!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_TerminatedUser_KeepsTheirShareEntry()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Terminated User");
        await InviteToRoom(room.Id, user, FileShare.Read);

        await TerminateUser(user);
        await _filesClient.Authenticate(Owner);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var entry = info.Find(s => s.SharedToUser?.Id == user.Id);
        entry.Should().NotBeNull();
        entry!.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_TotalCount_EqualsOwnerPlusInvitedUsersPlusGroups()
    {
        // Arrange
        var user1 = await InviteMember(EmployeeType.User);
        var user2 = await InviteMember(EmployeeType.User);
        var groupMember = await InviteMember(EmployeeType.User);
        var groupId = await CreateGroup(groupMember.Id, groupMember.Id);
        var room = await CreateCustomRoom("Autotest Share Count Matches");

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations =
            [
                new RoomInvitation { Id = user1.Id, Access = FileShare.Read },
                new RoomInvitation { Id = user2.Id, Access = FileShare.Editing },
                new RoomInvitation { Id = groupId, Access = FileShare.Read }
            ],
            Notify = false
        }, TestContext.Current.CancellationToken);
        await InviteToRoom(room.Id, user1, FileShare.None);

        // Act
        var info = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Should().HaveCount(3);
        var ids = info.ConvertAll(s => s.SharedToUser?.Id ?? s.SharedToGroup?.Id);
        ids.Should().NotContain(user1.Id);
        ids.Should().Contain(user2.Id);
        ids.Should().Contain(groupId);
    }
}
