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

namespace ASC.People.Tests.Tests._08_Search;

/// <summary>
/// Permissions for the room-scoped search endpoints: <c>GET /accounts/{file,folder,room}/{id}/search</c>
/// and <c>GET /people/{file,folder,room}/{id}</c>. Only a member with access to the room may look up
/// who else it is shared with; a plain member of the room, a guest member of the room, a
/// DocSpace admin who is not a member of the room, and an anonymous caller must all be refused.
/// </summary>
public class SharedSearchPermissionsTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    // ---- GET /accounts/file/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search User Permissions", "Autotest Search File");

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsNonMemberDocSpaceAdmin_ThrowsAccessDenied()
    {
        var member = await InviteContact(EmployeeType.User);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search DocSpaceAdmin Permissions", "Autotest Search File");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search Guest Permissions", "Autotest Search File");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, guest.Id, FileShare.Editing);

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateRoomWithFileAsync("Autotest Search Unauthorized", "Autotest Search File");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /people/file/:id ----

    [Fact]
    public async Task GetUsersWithFilesShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateRoomWithFileAsync("Autotest Users File User Permissions", "Autotest Users File");

        var member = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithFilesShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateRoomWithFileAsync("Autotest Users File Guest Permissions", "Autotest Users File");

        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithFilesShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateRoomWithFileAsync("Autotest Users File Unauthorized", "Autotest Users File");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /accounts/folder/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder User Permissions", "Autotest Search Folder");

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder Guest Permissions", "Autotest Search Folder");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(roomId, guest.Id, FileShare.Editing);

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var (_, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder Unauthorized", "Autotest Search Folder");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /people/folder/:id ----

    [Fact]
    public async Task GetUsersWithFoldersShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (_, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder User Permissions", "Autotest Users Folder");

        var member = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithFoldersShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var (_, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder Guest Permissions", "Autotest Users Folder");

        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithFoldersShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var (_, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder Unauthorized", "Autotest Users Folder");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /accounts/room/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room User Permissions");

        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room Guest Permissions");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, guest.Id, FileShare.Editing);

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room Unauthorized");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    // ---- GET /people/room/:id ----

    [Fact]
    public async Task GetUsersWithRoomShared_AsRoomMemberUser_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room User Permissions");

        var member = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithRoomShared_AsRoomMemberGuest_ThrowsAccessDenied()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room Guest Permissions");

        var guest = await InviteGuest();

        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79218")]
    public async Task GetUsersWithRoomShared_AsGuestInvitedToTheRoom_ThrowsAccessDenied()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Guest Room Search");

        var guest = await InviteGuest(admin);
        await _filesClient.Authenticate(admin);
        await InviteToRoom(room.Id, guest.Id, FileShare.Read);

        // A guest invited to the room must not be able to retrieve the portal members list through
        // it, even for the room they belong to.
        await _peopleClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task GetUsersWithRoomShared_Anonymous_ThrowsUnauthorized()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room Unauthorized");

        await _peopleClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(() =>
            _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
