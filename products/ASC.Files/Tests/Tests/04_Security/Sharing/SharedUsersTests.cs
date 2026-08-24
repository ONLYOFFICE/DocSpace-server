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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// <c>GET /api/2.0/files/file/{fileId}/sharedusers</c> (<c>GetSharedUsers</c>): the list of members
/// mentionable/sharable on a file, plus who is allowed to call the endpoint at all.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class SharedUsersTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Fact]
    public async Task GetSharedUsers_UnsharedFile_ReturnsEmptyList()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSharedUsers_SharedFile_ReturnsNonEmptyList()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSharedUsers_Owner_NotIncludedInList()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Select(u => u.Id).Should().NotContain(Owner.Id.ToString());
    }

    [Fact]
    public async Task GetSharedUsers_MemberWithoutAccess_NotInList()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Select(u => u.Id).Should().NotContain(user.Id.ToString());
    }

    [Fact]
    public async Task GetSharedUsers_Entries_HaveIdNameEmailHasAccessFields()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeEmpty();
        users.Should().AllSatisfy(u =>
        {
            u.Id.Should().NotBeNullOrEmpty();
            u.Name.Should().NotBeNull();
            u.Email.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task GetSharedUsers_SharedUserEntry_HasNonEmptyNameAndMatchingEmail()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        var userEntry = users.FirstOrDefault(u => u.Id == user.Id.ToString());
        userEntry.Should().NotBeNull();
        userEntry!.Name.Should().NotBeNullOrEmpty();
        userEntry.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetSharedUsers_MultipleSharedUsers_AllAppear()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user1.Id, Access = FileShare.Read }, new() { ShareTo = user2.Id, Access = FileShare.Comment }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        var userIds = users.Select(u => u.Id).ToList();
        userIds.Should().Contain(user1.Id.ToString());
        userIds.Should().Contain(user2.Id.ToString());
    }

    [Fact]
    [Trait("Bug", "81109")]
    public async Task GetSharedUsers_Guest_SeesOwnerButNotUserEntry()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);
        var guest = await InviteGuest();

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user.Id, Access = FileShare.Read }, new() { ShareTo = guest.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);
        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        var userIds = users.Select(u => u.Id).ToList();
        userIds.Should().Contain(Owner.Id.ToString());
        userIds.Should().NotContain(user.Id.ToString());
    }

    [Fact]
    public async Task GetSharedUsers_Unauthenticated_Returns401()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSharedUsers_UserWithFileAccess_Returns200()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        await _filesClient.Authenticate(user);
        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSharedUsers_UserWithoutFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSharedUsers_GuestWithFileAccess_Returns200()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSharedUsers_GuestWithoutFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Shared Users File.docx", Owner);
        var guest = await InviteGuest();

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSharedUsers_DocSpaceAdmin_ForRoomFile_Returns200()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var room = await CreateCollaborationRoom("Autotest Shared Users Room");
        var file = await CreateFile("Autotest Shared Users File.docx", room.Id);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = docSpaceAdmin.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(docSpaceAdmin);
        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSharedUsers_RoomAdmin_ForRoomFile_Returns200()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var room = await CreateCollaborationRoom("Autotest Shared Users Room");
        var file = await CreateFile("Autotest Shared Users File.docx", room.Id);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var users = (await _sharingApi.GetSharedUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        users.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "83105")]
    public async Task GetSharedUsers_NonExistentFileId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetSharedUsersAsync(999999999, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }
}
