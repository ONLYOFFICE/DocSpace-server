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

namespace ASC.Files.Tests.Tests._02_Folders.UsedSpace;

/// <summary>
/// <c>GET /api/2.0/files/filesusedspace</c> - access control. Storage statistics are portal-wide
/// administration data, so only the owner and a DocSpace admin may read them; a Room admin, a
/// regular user and a guest are all rejected, and an anonymous caller is unauthorized.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "UsedSpace")]
public class FilesUsedSpacePermissionsTests(
    AspireAppFixture fixture)
    : UsedSpaceTestBase(fixture)
{
    [Fact]
    public async Task GetUsedSpace_DocSpaceAdmin_ReturnsResponse()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await CreateFileInMy("used_space_admin_init.docx", admin);

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        sections.Should().NotBeNull();
        sections.MyDocumentsUsedSpace.Should().NotBeNull();
        sections.MyDocumentsUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetUsedSpace_RoomAdmin_Forbidden()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetUsedSpace_User_Forbidden()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetUsedSpace_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetUsedSpace_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
