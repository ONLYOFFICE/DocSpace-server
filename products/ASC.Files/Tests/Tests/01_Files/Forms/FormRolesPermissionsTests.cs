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

namespace ASC.Files.Tests.Tests._01_Files.Forms;

/// <summary>
/// <c>GET /files/file/{fileId}/formroles</c> — access control.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Forms")]
public class FormRolesPermissionsTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    [Fact]
    public async Task GetAllFormRoles_Owner_CanGetFormRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Owner Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles Owner Form.pdf");

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_DocSpaceAdmin_CanGetFormRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Admin Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles Admin Form.pdf");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_UserWithReadAccess_CanGetFormRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Read Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles Read Form.pdf");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_Unauthenticated_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Unauth Room");
        var file = await CreateFile("Autotest GetAllFormRoles Unauth File", room.Id);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetAllFormRoles_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles No Access Room");
        var file = await CreateFile("Autotest GetAllFormRoles No Access File", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(file.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetAllFormRoles_RoomAdminWithRoomManagerAccess_CanGetFormRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles RoomAdmin Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles RoomAdmin Form.pdf");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);
        await _filesClient.Authenticate(roomAdmin);

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllFormRoles_ContentCreator_CanGetFormRoles()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles ContentCreator Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles ContentCreator Form.pdf");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);
        await _filesClient.Authenticate(user);

        // Act
        var response = await _filesApi.GetAllFormRolesWithHttpInfoAsync(fileId, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>Historically bug 81348; already fixed, this is the expected behaviour today.</summary>
    [Fact]
    public async Task GetAllFormRoles_GuestWithoutRoomAccess_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest GetAllFormRoles Guest Room");
        var fileId = await UploadOoFormAsync(room.Id, "Autotest GetAllFormRoles Guest Form.pdf");

        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetAllFormRolesAsync(fileId, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
