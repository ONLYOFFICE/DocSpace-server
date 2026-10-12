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

namespace ASC.Files.Tests.Tests._02_Folders.Crud;

[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class FolderRenamePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // FileSecurity.Rename (products/ASC.Files/Core/Core/Security/FileSecurity.cs) only grants
    // renaming a folder inside a room to FileShare.RoomManager, or to FileShare.ContentCreator when
    // the caller is also the folder's creator. Every access level below is granted to someone who
    // did not create the folder, so all four are expected to be refused.
    public static TheoryData<EmployeeType, FileShare> InsufficientRoomAccess =>
        new()
        {
            { EmployeeType.User, FileShare.Read },
            { EmployeeType.User, FileShare.Editing },
            { EmployeeType.DocSpaceAdmin, FileShare.ContentCreator },
            { EmployeeType.RoomAdmin, FileShare.ContentCreator },
        };

    [Fact]
    public async Task RenameFolder_Anonymous_Returns401()
    {
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolder("Autotest Folder Anon Rename", FolderType.USER, Owner);

        _filesClient.DefaultRequestHeaders.Authorization = null;

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder("Autotest Folder Anon Renamed"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task RenameFolder_Guest_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolder("Autotest Folder Guest Rename", FolderType.USER, Owner);

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder("Autotest Folder Guest Renamed"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RenameFolder_UserOnAnotherUsersMyDocumentsSubfolder_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolder("Autotest Owner Folder User Cannot Rename", FolderType.USER, Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder("Autotest Owner Folder Renamed By User"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RenameFolder_RoomAdminNotMemberOfRoom_Returns403()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room Folder Rename No RoomAdmin");
        var folder = await CreateFolder("Autotest Folder RoomAdmin Not Member Rename", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder("Autotest Folder Renamed By Non-Member RoomAdmin"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [MemberData(nameof(InsufficientRoomAccess))]
    public async Task RenameFolder_WithInsufficientRoomAccess_Returns403(EmployeeType employeeType, FileShare access)
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Room Folder Rename {employeeType} {access}");
        var folder = await CreateFolder($"Autotest Folder {employeeType} {access} Rename", room.Id);

        var member = await InviteContact(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.RenameFolderAsync(folder.Id, new CreateFolder($"Autotest Folder Renamed By {employeeType}"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

}
