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
public class RootFoldersPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetRootFolders_Unauthenticated_Returns401()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetRootFolders_Owner_ReturnsOwnSections()
    {
        await _filesClient.Authenticate(Owner);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.Select(s => s.Current.Title).ToList();
        titles.Should().Contain(["Files", "Rooms", "Trash"]);
    }

    [Fact]
    public async Task GetRootFolders_RegularUser_ReturnsOwnSections()
    {
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.Select(s => s.Current.Title).ToList();
        titles.Should().Contain(["Files", "Rooms", "Trash"]);
    }

    [Fact]
    public async Task GetRootFolders_Guest_DoesNotSeeFilesSection()
    {
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.Select(s => s.Current.Title).ToList();
        titles.Should().NotContain("Files");
        titles.Should().Contain(["Rooms", "Trash"]);
    }

    [Fact]
    public async Task GetRootFolders_DocSpaceAdmin_ReturnsOwnSections()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.Select(s => s.Current.Title).ToList();
        titles.Should().Contain(["Files", "Rooms", "Trash"]);
    }

    [Fact]
    public async Task GetRootFolders_RoomMember_SeesTheirRoomInSections()
    {
        var roomTitle = "Autotest Room For Root Member Access";
        var room = await CreateCustomRoom(roomTitle);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        var result = await _foldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        var titles = result.Response.SelectMany(s => s.Folders ?? []).Select(f => f.Title).ToList();
        titles.Should().Contain(roomTitle);
    }
}
