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

namespace ASC.Files.Tests.Tests._01_Files.Versions;

/// <summary>POST /files/file/{fileId}/restoreversion - access control.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class RestoreVersionPermissionsTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    [Fact]
    public async Task RestoreVersion_Owner_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Perm Owner");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreVersion_DocSpaceAdminOwnRoom_ReturnsSuccess()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var (_, file) = await CreateRoomFileWithSecondVersion("Autotest Restore DSA Own Room", "Autotest Restore DSA File");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreVersion_RoomAdminOwnRoom_ReturnsSuccess()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var (_, file) = await CreateRoomFileWithSecondVersion("Autotest Restore RoomAdmin Own Room", "Autotest Restore RoomAdmin File");

        // Act
        var response = (await _filesApi.RestoreFileVersionWithHttpInfoAsync(
            file.Id,
            version: 1,
            cancellationToken: TestContext.Current.CancellationToken)).Data.Response;

        // Assert
        response.Should().NotBeNull();
    }

    [Fact]
    public async Task RestoreVersion_UserWithEditingAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Restore User Editing Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var file = await CreateFile("Autotest Restore Editing File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestoreVersion_UserWithReadAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Restore User Read Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var file = await CreateFile("Autotest Restore Read File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestoreVersion_Guest_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Restore Guest Room");

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        var file = await CreateFile("Autotest Restore Guest File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task RestoreVersion_Unauthenticated_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest Restore Anon");

        await _filesClient.Authenticate(null);

        // Act & Assert
        // The TS suite asserts 403 here (not 401, unlike ChangeVersionHistory's anonymous case) -
        // ported as observed since it reflects how RestoreFileVersion's authorization check reports
        // an anonymous caller.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.RestoreFileVersionAsync(
                file.Id,
                version: 1,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
