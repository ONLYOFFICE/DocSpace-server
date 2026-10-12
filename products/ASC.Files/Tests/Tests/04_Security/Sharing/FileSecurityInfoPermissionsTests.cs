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
/// Access control for <c>GET /api/2.0/files/file/{id}/share</c> (<c>GetFileSecurityInfo</c>): who
/// may look up the sharing rights of a file - role checks, room-level access and IDOR cases.
/// Functional coverage lives in <see cref="FileSecurityInfoReadTests"/> and
/// <see cref="FileSecurityInfoLifecycleTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class FileSecurityInfoPermissionsTests(
    AspireAppFixture fixture)
    : FileSecurityInfoTestBase(fixture)
{
    [Fact]
    public async Task GetFileSecurityInfo_Anonymous_Returns401()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm Anon.docx", Owner);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetFileSecurityInfo_Owner_OnOwnFile_Succeeds()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm Owner.docx", Owner);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_DocSpaceAdmin_OnRoomFile_Succeeds()
    {
        var docSpaceAdmin = await InviteContact(EmployeeType.DocSpaceAdmin);

        var room = await CreateCollaborationRoom("Autotest Security Info Perm Room");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = docSpaceAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Perm File.docx", room.Id);

        await _filesClient.Authenticate(docSpaceAdmin);
        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_RoomAdmin_WithRoomManagerAccess_Succeeds()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var room = await CreateCollaborationRoom("Autotest Security Info Perm Room RoomAdmin");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Perm File RoomAdmin.docx", room.Id);

        await _filesClient.Authenticate(roomAdmin);
        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_RoomAdmin_WithEditingAccessNotRoomManager_Succeeds()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);

        var room = await CreateCollaborationRoom("Autotest Security Info Perm RoomAdmin Editing");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = roomAdmin.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Perm File Editing.docx", room.Id);

        await _filesClient.Authenticate(roomAdmin);
        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserWithEditingAccessInRoom_Succeeds()
    {
        var user = await InviteContact(EmployeeType.User);

        var room = await CreateCollaborationRoom("Autotest Security Info Perm User Editing Room");
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Perm File User Editing.docx", room.Id);

        await _filesClient.Authenticate(user);
        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserWithFileAccess_Succeeds()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm User Access.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);

        await _filesClient.Authenticate(user);
        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserWithoutFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm User No Access.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileSecurityInfo_GuestWithoutAccessToRoomFile_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Security Info Perm Guest No Room Access");
        var file = await CreateFile("Autotest Security Info Perm Room File Guest.docx", room.Id);
        var guest = await InviteGuest();

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// A guest shared directly on a personal file (outside any room) is refused here, unlike the
    /// batch <c>POST /api/2.0/files/share</c> endpoint, which lets a directly-shared guest read the
    /// same information (see <c>GetSecurityInfoPermissionsTests.GetSecurityInfo_GuestWithFileAccess_Succeeds</c>).
    /// Ported as-is: the TS source asserts 403 here with no bug marker.
    /// </summary>
    [Fact]
    public async Task GetFileSecurityInfo_GuestWithFileAccess_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm Guest Access.docx", Owner);
        var guest = await InviteGuest();

        await ShareFile(file.Id, guest.Id, FileShare.Read);

        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserCannotReadSecurityInfoOfAnotherUsersPrivateFile_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info IDOR Private.docx", Owner);
        var attacker = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(attacker);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserLosesAccessAfterRevoke_Returns403()
    {
        var file = await CreateFileInMy("Autotest Security Info Revoked Access.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await ShareFile(file.Id, user.Id, FileShare.None);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserRemovedFromRoom_CannotReadRoomFileSecurityInfo_Returns403()
    {
        var room = await CreateCollaborationRoom("Autotest Security Info Room Remove");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Room File.docx", room.Id);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.None }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileSecurityInfo_UserInRoomA_CannotReadFileSecurityInfoInRoomB_Returns403()
    {
        var roomA = await CreateCollaborationRoom("Autotest Security Info Room A");
        var roomB = await CreateCollaborationRoom("Autotest Security Info Room B");
        var user = await InviteContact(EmployeeType.User);

        await _roomsApi.SetRoomSecurityAsync(roomA.Id, new RoomInvitationRequest
        {
            Invitations = [new() { Id = user.Id, Access = FileShare.Editing }]
        }, TestContext.Current.CancellationToken);

        var file = await CreateFile("Autotest Security Info Room B File.docx", roomB.Id);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
