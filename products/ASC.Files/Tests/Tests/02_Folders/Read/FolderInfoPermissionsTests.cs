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

namespace ASC.Files.Tests.Tests._02_Folders.Read;

[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class FolderInfoPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    [Trait("Bug", "81460")]
    public async Task GetFolderInfo_Anonymous_ReturnsUnauthorized()
    {
        var room = await CreateCustomRoom("Autotest Room For Info Auth");
        var folder = await CreateFolder("Autotest Folder Info Anon", room.Id);

        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetFolderInfo_UserWithoutAccess_Returns403()
    {
        var folder = await CreateFolderInMy("Autotest Folder Info User No Access", Owner);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderInfo_UserWithReadAccess_ReturnsOkWithCorrectAccessField()
    {
        var room = await CreateCustomRoom("Autotest Room For Info Read Access");
        var folder = await CreateFolder("Autotest Folder Info Read", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.Access.Should().Be(FileShare.Read);
    }

    [Fact]
    public async Task GetFolderInfo_GuestWithoutAccess_Returns403()
    {
        var folder = await CreateFolderInMy("Autotest Folder Info Guest No Access", Owner);

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderInfo_Owner_ReturnsInfoForAnotherUsersFolder()
    {
        var room = await CreateCustomRoom("Autotest Room For Owner Info Other User");

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.ContentCreator }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var folder = await CreateFolder("Autotest Folder By User For Owner Info", room.Id);

        await _filesClient.Authenticate(Owner);
        var info = (await _foldersApi.GetFolderInfoAsync(folder.Id, TestContext.Current.CancellationToken)).Response;

        info.Id.Should().Be(folder.Id);
    }
}
