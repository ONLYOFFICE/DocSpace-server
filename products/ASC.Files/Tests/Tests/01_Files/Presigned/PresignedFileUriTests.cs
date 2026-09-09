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

namespace ASC.Files.Tests.Tests._01_Files.Presigned;

/// <summary>
/// GET /files/file/{id}/presigned — a presigned download URL and file type for a file's raw
/// content, plus who is allowed to request it.
/// </summary>
[Trait("Category", "Files")]
public class PresignedFileUriTests(
    AspireAppFixture fixture)
    : PresignedTestBase(fixture)
{
    [Fact]
    public async Task GetPresignedFileUri_ValidFile_Returns200WithUrlAndFiletype()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri");

        // Act
        var link = (await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Url.Should().MatchRegex("^https?://");
        link.Filetype.Should().Be(".docx");
    }

    [Fact]
    public async Task GetPresignedFileUri_NonExistentFileId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetPresignedFileUriAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPresignedFileUri_Anonymous_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri Unauth");
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetPresignedFileUri_DocSpaceAdmin_CanGetPresignedUri()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri Admin");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var link = (await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPresignedFileUri_RoomAdmin_CanGetPresignedUri()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri RoomAdmin");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(roomId, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var link = (await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPresignedFileUri_UserWithReadAccess_CanGetPresignedUri()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (roomId, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri User");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var link = (await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPresignedFileUri_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri No Access");

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetPresignedFileUri_GuestWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, fileId) = await CreateFileInRoom("Autotest GetPresignedFileUri Guest");

        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetPresignedFileUriAsync(fileId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
