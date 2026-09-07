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

namespace ASC.People.Tests.Tests._14_GroupSearch;

/// <summary>
/// Permissions for <c>GET /api/2.0/group/{file|folder|room}/{id}</c>: only someone with edit
/// access to the target entity may see who it is shared with.
/// </summary>
public class GroupSearchPermissionsTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsWithFilesShared_DocSpaceAdmin_CannotGetGroupsSharedWithOwnersFile()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_FileOwnerUser_GetsGroupsSharedWithOwnFile()
    {
        var group = await CreateGroupAsync();

        var user = await InviteContact(EmployeeType.User);
        var fileId = await CreateFileInMyDocumentsAsync(user);
        await _filesClient.Authenticate(user);
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        await _peopleClient.Authenticate(user);
        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_UserWithoutAccess_CannotGetGroupsInfo()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GuestWithoutAccess_CannotGetGroupsInfo()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_Unauthenticated_IsRejectedWith401()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_DocSpaceAdmin_CannotGetGroupsSharedWithOwnersFolder()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_FolderOwnerUser_GetsGroupsSharedWithOwnFolder()
    {
        var group = await CreateGroupAsync();

        var user = await InviteContact(EmployeeType.User);
        var folderId = await CreateFolderInMyDocumentsAsync(user);
        await _filesClient.Authenticate(user);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        await _peopleClient.Authenticate(user);
        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_UserWithoutAccess_CannotGetGroupsInfo()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GuestWithoutAccess_CannotGetGroupsInfo()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_Unauthenticated_IsRejectedWith401()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_DocSpaceAdmin_CannotGetGroupsSharedWithOwnersRoom()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    // TS names this "Room owner (User)" but authenticates as RoomAdmin: a plain User cannot
    // create a room, so the actual actor under test is the RoomAdmin who created it.
    [Fact]
    public async Task GetGroupsWithRoomsShared_RoomOwner_GetsGroupsSharedWithOwnRoom()
    {
        var group = await CreateGroupAsync();

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var roomId = await CreateCustomRoomAsync(roomAdmin);
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        await _peopleClient.Authenticate(roomAdmin);
        var result = (await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_UserWithoutAccess_CannotGetGroupsInfo()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var user = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_GuestWithoutAccess_CannotGetGroupsInfo()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var guest = await InviteGuest();
        await _peopleClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_UserInvitedAsRead_CannotGetGroupsInfo()
    {
        var roomId = await CreateCustomRoomAsync();
        var group = await CreateGroupAsync();
        await InviteToRoom(roomId, group.Id, FileShare.Read);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, user.Id, FileShare.Read);

        await _peopleClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetGroupsWithRoomsShared_Unauthenticated_IsRejectedWith401()
    {
        var roomId = await CreateCustomRoomAsync();
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithRoomsSharedAsync(roomId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
