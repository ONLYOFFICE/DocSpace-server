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

namespace ASC.Files.Tests.Tests._06_Operations.DeleteVersion;

/// <summary>PUT /api/2.0/files/fileops/deleteversion - access control.</summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class DeleteVersionPermissionsTests(
    AspireAppFixture fixture)
    : DeleteVersionTestBase(fixture)
{
    [Fact]
    public async Task DeleteVersion_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Anon File");

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DeleteVersion_Owner_DeletesVersionOfOwnFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Owner File");

        // Act
        await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }

    [Fact]
    public async Task DeleteVersion_User_DeletesVersionOfOwnFile()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);
        var file = await CreateFileWithSecondVersion("Autotest DelVer User File", user);

        // Act
        await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }

    [Fact]
    public async Task DeleteVersion_UserCannotDeleteVersionsOfAnotherUsersFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer Other User File");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteVersion_RoomAdmin_CanDeleteFileVersionsInTheirRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        var room = await CreateCustomRoom("Autotest DelVer RoomAdmin Room");
        var file = await CreateFile("Autotest DelVer RoomAdmin File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        await DeleteVersionsAndWait(file.Id, [1]);

        // Assert
        var versionNumbers = await GetVersionNumbers(file.Id);
        versionNumbers.Should().NotContain(1);
        versionNumbers.Should().Contain(2);
    }

    [Fact]
    public async Task DeleteVersion_DocSpaceAdmin_CannotDeleteVersionsOfFileInAnotherUsersMyDocuments_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileWithSecondVersion("Autotest DelVer DSAdmin File");

        var docSpaceAdmin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(docSpaceAdmin);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteVersion_GuestWithReadAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var guest = await InviteMember(EmployeeType.Guest);

        var room = await CreateCustomRoom("Autotest DelVer Guest Room");
        var file = await CreateFile("Autotest DelVer Guest File", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile { LastVersion = 2 }, TestContext.Current.CancellationToken);

        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DeleteFileVersionsAsync(
                new DeleteVersionBatchRequestDto(fileId: file.Id, versions: [1]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
