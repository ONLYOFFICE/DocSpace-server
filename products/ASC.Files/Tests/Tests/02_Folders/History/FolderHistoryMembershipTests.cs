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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - room membership actions: inviting, changing
/// the access level of and removing a user or a group, resending invitations and changing the
/// room owner.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryMembershipTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_UserAddedToRoom_ContainsRoomCreateUser()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History AddUser");
        var user = await InviteContact(EmployeeType.User);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomCreateUser);

        // Act
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomCreateUser);
    }

    [Fact]
    public async Task GetFolderHistory_UserRemovedFromRoom_ContainsRoomRemoveUser()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History RemoveUser");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomRemoveUser);

        // Act
        await InviteToRoom(room.Id, user, FileShare.None);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomRemoveUser);
    }

    [Fact]
    public async Task GetFolderHistory_UserRoleChanged_ContainsRoomUpdateAccessForUser()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History ChangeRole");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomUpdateAccessForUser);

        // Act
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomUpdateAccessForUser);
    }

    [Fact]
    public async Task GetFolderHistory_GroupAddedToRoom_ContainsRoomGroupAdded()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomGroupAdded");
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto(groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10]), TestContext.Current.CancellationToken)).Response;

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomGroupAdded);

        // Act
        await InviteToRoomAsGroup(room.Id, group.Id, FileShare.Read);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomGroupAdded, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_GroupRoleChanged_ContainsRoomUpdateAccessForGroup()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomUpdateAccessForGroup");
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto(groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10]), TestContext.Current.CancellationToken)).Response;
        await InviteToRoomAsGroup(room.Id, group.Id, FileShare.Read);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomUpdateAccessForGroup);

        // Act
        await InviteToRoomAsGroup(room.Id, group.Id, FileShare.Editing);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomUpdateAccessForGroup, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_GroupRemovedFromRoom_ContainsRoomGroupRemove()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomGroupRemove");
        var group = (await _groupApi.AddGroupAsync(new GroupRequestDto(groupManager: Owner.Id, groupName: Guid.NewGuid().ToString("N")[..10]), TestContext.Current.CancellationToken)).Response;
        await InviteToRoomAsGroup(room.Id, group.Id, FileShare.Read);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomGroupRemove);

        // Act
        await InviteToRoomAsGroup(room.Id, group.Id, FileShare.None);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomGroupRemove, displayName);
    }

    [Fact]
    public async Task GetFolderHistory_RoomOwnerChanged_ContainsRoomChangeOwner()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Folder History RoomChangeOwner");
        var newOwner = await InviteContact(EmployeeType.RoomAdmin);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomChangeOwner);

        // Act
        await _sharingApi.ChangeFileOwnerAsync(
            new ChangeOwnerRequestDto(folderIds: [new BatchRequestDtoAllOfFileIds(room.Id)], userId: newOwner.Id),
            TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomChangeOwner);
    }

    [Fact]
    public async Task GetFolderHistory_InvitationsResent_ContainsRoomInviteResend()
    {
        // Arrange
        var displayName = await GetDisplayNameAsync();
        var room = await CreateCustomRoom("Autotest Folder History RoomInviteResend");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var beforeIds = await GetHistoryActionIdsAsync(room.Id);
        beforeIds.Should().NotContain(MessageAction.RoomInviteResend);

        // Act
        await _roomsApi.ResendEmailInvitationsAsync(room.Id, new UserInvitation(resendAll: true), TestContext.Current.CancellationToken);

        // Assert
        await AssertHistoryContainsAsync(room.Id, MessageAction.RoomInviteResend, displayName);
    }

    /// <summary>Invites a People group into a room with the given access level.</summary>
    private async Task InviteToRoomAsGroup(int roomId, Guid groupId, FileShare access)
    {
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest
            {
                Invitations = [new RoomInvitation { Id = groupId, Access = access }],
                Notify = false
            },
            cancellationToken: TestContext.Current.CancellationToken);
    }
}
