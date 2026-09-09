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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class EditingStartPermissionsTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    /// <summary>
    /// The endpoint should reject an unauthenticated caller with 401, not 403. Asserts the
    /// correct behaviour so the test stays red while the bug is open.
    /// </summary>
    [Trait("Bug", "81168")]
    [Fact]
    public async Task StartEditFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest StartEdit Unauth Room", "Autotest StartEdit Unauth File");

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task StartEditFile_GuestWithReadAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest StartEdit Guest Room", "Autotest StartEdit Guest File");

        var guest = await InviteGuest();
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You do not have enough permissions to edit the file");
    }

    [Fact]
    public async Task StartEditFile_UserWithReadOnlyAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest StartEdit Read Room", "Autotest StartEdit Read File");

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEdit(editingAlone: false), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("You do not have enough permissions to edit the file");
    }

    // NOTE: The TS suite also has "User with RoomManager access gets 403", which invites a
    // User-type member with FileShare.RoomManager access. That invitation is illegal on its own:
    // FileSecurity.AvailableUserAccesses only allows RoomManager for DocSpaceAdmin and RoomAdmin,
    // so SetRoomSecurityAsync throws before the assertion is ever reached (see the same reasoning
    // documented in FileVersionInfoPermissionsTests). The test cannot pass by construction and was
    // dropped rather than ported.
}
