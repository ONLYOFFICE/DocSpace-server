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

namespace ASC.Files.Tests.Tests._02_Folders.Subfolders;

[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class SubfoldersPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetSubfolders_Unauthenticated_Returns401()
    {
        var room = await CreateCustomRoom("Autotest Room For Subfolders Auth");
        await CreateFolder("Autotest Subfolder Anon Auth", room.Id);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetSubfolders_UserWithoutAccess_Returns403()
    {
        var folder = await CreateFolderInMy("Autotest Folder Subfolders No Access", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// Every access level a CustomRoom accepts for a User subject except RoomManager (only a
    /// RoomAdmin may be granted it — see <c>RoomAdminWithRoomManagerAccess_Returns200</c> below).
    /// Reused from <see cref="RoomAccessData.NonManagerAccesses"/> rather than a new copy of the
    /// same matrix.
    /// </summary>
    [Theory]
    [MemberData(nameof(RoomAccessData.NonManagerAccesses), MemberType = typeof(RoomAccessData))]
    public async Task GetSubfolders_UserWithAccess_Returns200(FileShare access)
    {
        var room = await CreateCustomRoom($"Autotest Room For {access} Subfolders");
        await CreateFolder($"Autotest Subfolder {access}", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = access }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubfolders_GuestWithoutAccess_Returns403()
    {
        var folder = await CreateFolderInMy("Autotest Folder Subfolders Guest No Access", Owner);

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubfolders_OwnerGetsAnotherUsersSubfolders_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room For Owner Gets Other User Subfolders");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var folder = await CreateFolder("Autotest Subfolder By User For Owner", room.Id);
        await CreateFolder("Autotest Nested Subfolder By User", folder.Id);

        await _filesClient.Authenticate(Owner);
        var result = await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetSubfolders_UserWithoutMembership_PublicRoom_Returns403()
    {
        var room = await CreatePublicRoom("Autotest Public Room For Non-member Subfolders");
        await CreateFolder("Autotest Subfolder In Public Room Non-member", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubfolders_GuestWithReadAccess_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room For Guest Read Subfolders");
        var folder = await CreateFolder("Autotest Subfolder Guest Read", room.Id);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);
        var result = await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubfolders_DocSpaceAdminWithRoomAccess_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room For DocSpaceAdmin Subfolders");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = admin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await CreateFolder("Autotest Subfolder For Admin", room.Id);

        await _filesClient.Authenticate(admin);
        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    [Trait("Bug", "81463")]
    public async Task GetSubfolders_RoomAdminWithRoomManagerAccess_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room For RoomManager Subfolders");
        await CreateFolder("Autotest Subfolder RoomManager", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);
        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSubfolders_UserWithFillFormsAccess_FillingFormsRoom_Returns200()
    {
        var room = await CreateFillingFormsRoom("Autotest Room For FillForms Subfolders");
        await CreateFolder("Autotest Subfolder FillForms", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.FillForms }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }

    /// <summary>
    /// The TypeScript source grants <c>FileShare.Restrict</c>, which the product refuses outright
    /// ("The role is not available for this user type") — it is not one of the accesses
    /// <c>FileSecurity.AvailableRoomAccesses</c> lists for a <c>User</c> in a CustomRoom, so that
    /// invitation never succeeds and the test cannot run as written. Ported as the constructible
    /// equivalent: the member is invited with <c>Read</c> and then revoked with <c>FileShare.None</c>,
    /// which is how access is actually taken away.
    /// </summary>
    [Fact]
    public async Task GetSubfolders_UserWithRevokedAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room For Restrict Subfolders");
        await CreateFolder("Autotest Subfolder Restrict", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.None }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubfolders_DocSpaceAdminWithoutRoomAccess_Returns403()
    {
        var folder = await CreateFolderInMy("Autotest Folder DocSpaceAdmin No Access", Owner);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFoldersAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetSubfolders_UserWithReadAccess_ArchivedRoom_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Archived Room For User Subfolders");
        await CreateFolder("Autotest Subfolder In Archived Room", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(deleteAfter: false), TestContext.Current.CancellationToken);
        await WaitLongOperation();

        await _filesClient.Authenticate(user);
        var result = await _foldersApi.GetFoldersAsync(room.Id, TestContext.Current.CancellationToken);

        result.Response.Should().NotBeNull();
    }
}
