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

namespace ASC.Files.Tests.Tests._01_Files.History;

/// <summary>GET /files/file/{fileId}/history - access control.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileVersionInfoPermissionsTests(
    AspireAppFixture fixture)
    : HistoryTestBase(fixture)
{
    [Fact]
    public async Task GetFileVersionInfo_Owner_CanGetVersionHistory()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Version History Owner", Owner);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions.Should().NotBeEmpty();
        versions[0].Version.Should().Be(1);
    }

    [Fact]
    public async Task GetFileVersionInfo_DocSpaceAdmin_CanGetVersionHistoryForOwnFile()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var file = await CreateFileInMy("Autotest Version History Admin", admin);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFileVersionInfo_RoomAdmin_CanGetVersionHistoryForOwnFile()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var file = await CreateFileInMy("Autotest Version History Room Admin", roomAdmin);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFileVersionInfo_User_CanGetVersionHistoryForOwnFile()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var file = await CreateFileInMy("Autotest Version History User", user);

        // Act
        var versions = (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        versions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFileVersionInfo_Unauthenticated_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Version History Anon", Owner);

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileVersionInfo_User_Returns403ForAnotherUsersPrivateFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Version History Other Private", Owner);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileVersionInfo_DocSpaceAdmin_Returns403ForFileCreatedByAnotherUserInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateRoomWithFile("Autotest Room Version History Admin", "Autotest Version History Admin Cross-User");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act & Assert
        // The admin was never invited into the room, so a portal role alone does not grant history
        // access to a file owned by someone else.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    // NOTE: The TS suite also has "Room manager gets 403 for a file created by another user in
    // their room", which invites a User-type member with FileShare.RoomManager access. That
    // invitation is illegal on its own - only a RoomAdmin can be granted RoomManager
    // (FileSecurity.GetTypeByShare) - so Arrange fails before the assertion is ever reached. It
    // is also inconsistent with FileSecurity.CanAsync's FilesSecurityActions.ReadHistory branch,
    // which explicitly allows FileShare.RoomManager for room-scoped files. The test cannot pass
    // by construction and was dropped rather than ported.
}
