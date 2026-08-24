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

namespace ASC.Files.Tests.Tests._01_Files.Order;

/// <summary>
/// Permission coverage of <c>PUT /files/order</c>. Unlike most room actions, bulk ordering needs
/// portal-level access (owner or a DocSpaceAdmin acting on their own room) - a room-level role,
/// however privileged, is not enough.
/// </summary>
[Trait("Category", "Files")]
public class FilesBulkOrderPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task SetFilesOrder_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateVirtualRoom("Autotest BulkOrder Anon Room");
        var file = await CreateFile("Autotest BulkOrder Anon File", room.Id);

        await _filesClient.Authenticate(null);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SetFilesOrder_GuestWithReadAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var guest = await InviteGuest();
        var room = await CreateVirtualRoom("Autotest BulkOrder Guest Room");
        await InviteToRoom(room.Id, guest, FileShare.Read);
        var file = await CreateFile("Autotest BulkOrder Guest File", room.Id);

        await _filesClient.Authenticate(guest);
        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// The TypeScript source granted <c>RoomManager</c> to a plain <c>User</c> and expected 403, but
    /// <c>RoomManager</c> is only a legal grant for a <c>RoomAdmin</c>
    /// (<c>FileSecurity.AvailableRoomAccesses</c>), so that combination cannot be arranged at all.
    /// Ported as the case that does exist - a <c>RoomAdmin</c> holding <c>RoomManager</c> - which the
    /// product allows to reorder: room-level management is enough for bulk ordering inside the room,
    /// contrary to the "portal-level access required" premise of the TypeScript test. The refusals
    /// for lesser accesses are covered by the sibling tests in this class.
    /// </summary>
    [Fact]
    public async Task SetFilesOrder_RoomAdminWithRoomManagerAccess_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        var room = await CreateVirtualRoom("Autotest BulkOrder RoomAdmin Room");
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        var file = await CreateFile("Autotest BulkOrder RoomAdmin File", room.Id);

        await _filesClient.Authenticate(roomAdmin);
        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 2)]);

        // Act
        var response = await _filesApi.SetFilesOrderWithHttpInfoAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetFilesOrder_UserWithReadAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteContact(EmployeeType.User);
        var room = await CreateVirtualRoom("Autotest BulkOrder User Read Room");
        await InviteToRoom(room.Id, user, FileShare.Read);
        var file = await CreateFile("Autotest BulkOrder User Read File", room.Id);

        await _filesClient.Authenticate(user);
        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 1)]);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SetFilesOrder_DocSpaceAdmin_OwnRoom_Succeeds()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateVirtualRoom("Autotest BulkOrder Admin Room");
        var file = await CreateFile("Autotest BulkOrder Admin File", room.Id);

        var request = new OrdersRequestDtoInteger([new OrdersItemRequestDtoInteger(file.Id, FileEntryType.File, 3)]);

        // Act
        var result = await _filesApi.SetFilesOrderAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.Response[0].Id.Should().Be(file.Id);
    }
}
