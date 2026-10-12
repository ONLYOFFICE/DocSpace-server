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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// <c>GET /api/2.0/files/folder/{folderId}/log</c> - access control for reading room history.
/// Functional coverage of the endpoint itself is split across
/// <see cref="FolderHistoryQueryTests"/>, <see cref="FolderHistoryFileActionsTests"/>,
/// <see cref="FolderHistoryMembershipTests"/> and <see cref="FolderHistoryRoomSettingsTests"/>.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "History")]
public class FolderHistoryPermissionsTests(
    AspireAppFixture fixture)
    : FolderHistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_Owner_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog Owner Perm");

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
        history[0].Initiator.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_DocSpaceAdmin_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog Admin Perm");

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_UserWithRoomManagerAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog RoomManager Perm");

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_UserWithReadAccess_Allowed()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog User Read Perm");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var history = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        history.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFolderHistory_UserWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog NoAccess Perm");

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderHistory_GuestWithoutRoomAccess_Forbidden()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog Guest Perm");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderHistory_Anonymous_Unauthorized()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest FolderLog Anon Perm");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
