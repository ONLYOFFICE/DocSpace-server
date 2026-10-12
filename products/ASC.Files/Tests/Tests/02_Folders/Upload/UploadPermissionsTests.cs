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

namespace ASC.Files.Tests.Tests._02_Folders.Upload;

/// <summary>
/// <c>POST /api/2.0/files/{folderId}/upload</c> - access control. Uploading a file into a room
/// requires <c>ContentCreator</c> or <c>RoomManager</c>; <c>Editing</c> and <c>Read</c> are not
/// enough (see <c>FileSecurity.AvailableRoomAccesses</c>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Upload")]
public class UploadPermissionsTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Fact]
    public async Task UploadFile_Anonymous_Returns401()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Anon");
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-anon.txt"));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task UploadFile_UserWithoutRoomAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload User No Access");
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-no-access.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UploadFile_GuestWithoutRoomAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Guest No Access");
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-guest-no-access.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UploadFile_UserWithEditingAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload User Editing");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-editing.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UploadFile_UserWithReadAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload User Read");
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-read-only.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UploadFile_GuestWithReadAccess_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload Guest Read");
        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-guest-read.txt"));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task UploadFile_DocSpaceAdminWithRoomManagerAccess_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room Upload DocSpaceAdmin");
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);
        await _filesClient.Authenticate(admin);

        var uploaded = await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-admin.txt");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadFile_RoomAdminWithRoomManagerAccess_Returns200()
    {
        var room = await CreateCustomRoom("Autotest Room Upload RoomAdmin");
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        var uploaded = await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-roomadmin.txt");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadFile_RoomAdminNotMemberOfRoom_Returns403()
    {
        var room = await CreateCustomRoom("Autotest Room Upload RoomAdmin No Access");
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToFolderAsync(room.Id, "Autotest file content"u8.ToArray(), "autotest-roomadmin-noaccess.txt"));

        exception.ErrorCode.Should().Be(403);
    }
}
