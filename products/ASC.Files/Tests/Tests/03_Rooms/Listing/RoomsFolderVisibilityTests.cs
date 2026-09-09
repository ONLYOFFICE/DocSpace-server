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

namespace ASC.Files.Tests.Tests._03_Rooms.Listing;

/// <summary>
/// GET /files/rooms - who can call it, and which rooms each role actually sees.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomsFolderVisibilityTests(
    AspireAppFixture fixture)
    : RoomsFolderTestBase(fixture)
{
    [Fact]
    public async Task GetRoomsFolder_DocSpaceAdmin_CanGetRoomsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        await CreateCustomRoom("Autotest Admin Own Room " + Guid.NewGuid().ToString()[..8]);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Folders.Should().Contain(f => f.Title.StartsWith("Autotest Admin Own Room"));
    }

    [Fact]
    public async Task GetRoomsFolder_RoomAdmin_SeesOnlyRoomsWhereInvited()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var visible = await CreateCustomRoom("Autotest Owner Visible " + Guid.NewGuid().ToString()[..8]);
        var hidden = await CreateCustomRoom("Autotest Owner Hidden " + Guid.NewGuid().ToString()[..8]);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(visible.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = result.Folders.Select(f => f.Title).ToList();
        titles.Should().Contain(visible.Title);
        titles.Should().NotContain(hidden.Title);
    }

    [Fact]
    public async Task GetRoomsFolder_User_SeesOnlyRoomsInvitedTo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var visible = await CreateCustomRoom("Autotest User Visible " + Guid.NewGuid().ToString()[..8]);
        var hidden = await CreateCustomRoom("Autotest User Hidden " + Guid.NewGuid().ToString()[..8]);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(visible.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = result.Folders.Select(f => f.Title).ToList();
        titles.Should().Contain(visible.Title);
        titles.Should().NotContain(hidden.Title);
    }

    [Fact]
    public async Task GetRoomsFolder_Guest_SeesOnlyRoomsInvitedTo()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var visible = await CreateCustomRoom("Autotest Guest Visible " + Guid.NewGuid().ToString()[..8]);
        var hidden = await CreateCustomRoom("Autotest Guest Hidden " + Guid.NewGuid().ToString()[..8]);

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(visible.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);

        // Act
        var result = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        var titles = result.Folders.Select(f => f.Title).ToList();
        titles.Should().Contain(visible.Title);
        titles.Should().NotContain(hidden.Title);
    }

    [Fact]
    public async Task GetRoomsFolder_AnonymousRequest_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetRoomsFolder_TerminatedUser_CannotGetRoomsFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);

        // Authenticate as the user first, so the rejection comes from invalidating an existing
        // session rather than from ever failing to establish one — matching
        // RoomPinPermissionsTests.PinRoom_TerminatedUser_Unauthorized.
        await _filesClient.Authenticate(user);
        await TerminateUser(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
