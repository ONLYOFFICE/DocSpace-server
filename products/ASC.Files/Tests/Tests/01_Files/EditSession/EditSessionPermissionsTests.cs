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

namespace ASC.Files.Tests.Tests._01_Files.EditSession;

[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class EditSessionPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    #region POST /files/{fileId}/edit_session - access control

    [Fact]
    public async Task CreateEditSession_Owner_ReturnsSuccess()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Session Owner Room");
        var file = await CreateFile("Autotest Edit Session Owner File", room.Id);

        // Act
        var session = (await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken)).Response;

        // Assert
        session.Success.Should().BeTrue();
    }

    /// <summary>
    /// Every member invited with an access level that allows editing can create an edit session,
    /// whatever the role that access was granted to. <c>RoomManager</c> is only ever granted to a
    /// <c>RoomAdmin</c> (see <c>FileSecurity.AvailableRoomAccesses</c>).
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin, FileShare.Editing)]
    [InlineData(EmployeeType.RoomAdmin, FileShare.RoomManager)]
    [InlineData(EmployeeType.User, FileShare.Editing)]
    public async Task CreateEditSession_InvitedMemberWithEditAccess_ReturnsSuccess(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Edit Session {employeeType} Room");
        var file = await CreateFile($"Autotest Edit Session {employeeType} File", room.Id);

        var member = await InviteContact(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act
        var session = (await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken)).Response;

        // Assert
        session.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEditSession_InvitedMemberWithReadAccess_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Session Reader Room");
        var file = await CreateFile("Autotest Edit Session Reader File", room.Id);

        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Read);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateEditSession_Guest_Forbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Session Guest Room");
        var file = await CreateFile("Autotest Edit Session Guest File", room.Id);

        // The guest is created but never invited into the room, so it has no access to the file.
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateEditSession_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Session Anon Room");
        var file = await CreateFile("Autotest Edit Session Anon File", room.Id);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    #endregion

}
