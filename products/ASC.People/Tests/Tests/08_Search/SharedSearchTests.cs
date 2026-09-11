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
/// <c>GET /accounts/{file,folder,room}/{id}/search</c> and <c>GET /people/{file,folder,room}/{id}</c>
/// — searching for the accounts (users and groups) or the users a file/folder/room is shared with,
/// filtered by name. Every room here is a <see cref="RoomType.CustomRoom"/>, the only room type that
/// accepts <see cref="FileShare.Editing"/> for a <see cref="EmployeeType.User"/> subject
/// (<c>FileSecurity.AvailableRoomAccesses</c>). Granting <see cref="FileShare.RoomManager"/> is only
/// legal for a <see cref="EmployeeType.RoomAdmin"/> or <see cref="EmployeeType.DocSpaceAdmin"/>
/// subject (<c>FileSecurity.AvailableUserAccesses</c>), which is why the admin actors below are
/// always invited with that access instead of <c>Editing</c>.
/// </summary>
public class SharedSearchTests(AspireAppFixture fixture) : SearchTestBase(fixture)
{
    // ---- GET /accounts/file/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search File Shared", "Autotest Search File");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search Admin", "Autotest Search File");
        await InviteToRoom(roomId, admin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithFilesShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Search RoomAdmin", "Autotest Search File");
        await InviteToRoom(roomId, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    // ---- GET /people/file/:id ----

    [Fact]
    public async Task GetUsersWithFilesShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Users File Owner", "Autotest Users File");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFilesShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Users File Admin", "Autotest Users File");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFilesShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Users File RoomAdmin", "Autotest Users File");
        await InviteToRoom(roomId, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFilesShared_AsUser_FindsFellowInvitedMember()
    {
        var searchable = await InviteContact(EmployeeType.User);
        var searchableName = await DisplayNameOf(searchable);
        var caller = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateRoomWithFileAsync("Autotest Users File User", "Autotest Users File");
        await InviteToRoom(roomId, caller.Id, FileShare.Editing);
        await InviteToRoom(roomId, searchable.Id, FileShare.Editing);

        await _peopleClient.Authenticate(caller);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFilesSharedAsync(fileId, filterValue: searchableName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, searchable.Id, searchableName);
    }

    // ---- GET /accounts/folder/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder Shared", "Autotest Search Folder");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder Admin", "Autotest Search Folder");
        await InviteToRoom(roomId, admin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithFoldersShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Search Folder RoomAdmin", "Autotest Search Folder");
        await InviteToRoom(roomId, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    // ---- GET /people/folder/:id ----

    [Fact]
    public async Task GetUsersWithFoldersShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder Owner", "Autotest Users Folder");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFoldersShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder Admin", "Autotest Users Folder");
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFoldersShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder RoomAdmin", "Autotest Users Folder");
        await InviteToRoom(roomId, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(roomId, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithFoldersShared_AsUser_FindsFellowInvitedMember()
    {
        var searchable = await InviteContact(EmployeeType.User);
        var searchableName = await DisplayNameOf(searchable);
        var caller = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var (roomId, folderId) = await CreateRoomWithFolderAsync("Autotest Users Folder User", "Autotest Users Folder");
        await InviteToRoom(roomId, caller.Id, FileShare.Editing);
        await InviteToRoom(roomId, searchable.Id, FileShare.Editing);

        await _peopleClient.Authenticate(caller);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithFoldersSharedAsync(folderId, filterValue: searchableName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, searchable.Id, searchableName);
    }

    // ---- GET /accounts/room/:id/search ----

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room Shared");
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room Admin");
        await InviteToRoom(room.Id, admin.Id, FileShare.RoomManager);
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    [Fact]
    public async Task GetAccountsEntriesWithRoomsShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Search Room RoomAdmin");
        await InviteToRoom(room.Id, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetAccountsEntriesWithRoomsSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleAccountEntry(result, member.Id, memberName);
    }

    // ---- GET /people/room/:id ----

    [Fact]
    public async Task GetUsersWithRoomShared_AsOwner_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room Owner");
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(Owner);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithRoomShared_AsDocSpaceAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room Admin");
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(admin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithRoomShared_AsRoomAdmin_FindsInvitedMember()
    {
        var member = await InviteContact(EmployeeType.User);
        var memberName = await DisplayNameOf(member);
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room RoomAdmin");
        await InviteToRoom(room.Id, roomAdmin.Id, FileShare.RoomManager);
        await InviteToRoom(room.Id, member.Id, FileShare.Editing);

        await _peopleClient.Authenticate(roomAdmin);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, filterValue: memberName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, member.Id, memberName);
    }

    [Fact]
    public async Task GetUsersWithRoomShared_AsUser_FindsFellowInvitedMember()
    {
        var searchable = await InviteContact(EmployeeType.User);
        var searchableName = await DisplayNameOf(searchable);
        var caller = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Users Room User");
        await InviteToRoom(room.Id, caller.Id, FileShare.Editing);
        await InviteToRoom(room.Id, searchable.Id, FileShare.Editing);

        await _peopleClient.Authenticate(caller);
        var result = await PollUntilAsync(
            () => _peopleSearchApi.GetUsersWithRoomSharedAsync(room.Id, filterValue: searchableName, cancellationToken: TestContext.Current.CancellationToken),
            r => r.Response?.Count == 1);

        AssertSingleEmployee(result.Response, searchable.Id, searchableName);
    }

    private static void AssertSingleAccountEntry(IAccountEntryArrayWrapper result, Guid expectedId, string expectedName)
    {
        result.Response.Should().HaveCount(1);

        var entry = result.Response![0].ActualInstance as EmployeeFullDto;
        entry.Should().NotBeNull("the only matching account is the invited user, not a group");
        entry!.Id.Should().Be(expectedId);
        WebUtility.HtmlDecode(entry.DisplayName).Should().Be(expectedName);
    }

    private static void AssertSingleEmployee(List<EmployeeFullDto> result, Guid expectedId, string expectedName)
    {
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(expectedId);
        WebUtility.HtmlDecode(result[0].DisplayName).Should().Be(expectedName);
    }
}
