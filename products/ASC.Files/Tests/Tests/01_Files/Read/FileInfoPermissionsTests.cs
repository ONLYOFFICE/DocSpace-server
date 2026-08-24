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

namespace ASC.Files.Tests.Tests._01_Files.Read;

[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class FileInfoPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetFileInfo_DocSpaceAdmin_ReturnsOk()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Room For Admin File Info");
        var file = await CreateFile("Autotest Admin Get File Info", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var info = await GetFile(file.Id);

        // Assert
        info.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task GetFileInfo_RoomAdminOfTheirRoom_ReturnsOk()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Room For File Info Permissions");
        var file = await CreateFile("Autotest Room Admin File Info", room.Id);

        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = roomAdmin.Id, Access = FileShare.RoomManager }]
        }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var info = await GetFile(file.Id);

        // Assert
        info.Id.Should().Be(file.Id);
        info.FolderId.Should().Be(room.Id);
    }

    [Fact]
    public async Task GetFileInfo_RegularUserWithAccessToRoom_ReturnsOk()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Room For User File Info");
        var file = await CreateFile("Autotest User File Info", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _roomsApi.SetRoomSecurityAsync(room.Id, new RoomInvitationRequest
        {
            Invitations = [new RoomInvitation { Id = user.Id, Access = FileShare.Read }]
        }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(user);
        var info = await GetFile(file.Id);

        // Assert
        info.Id.Should().Be(file.Id);
    }

    [Fact]
    public async Task GetFileInfo_UserWithoutRoomAccess_Returns403()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Room For Forbidden File Info");
        var file = await CreateFile("Autotest Forbidden File Info", room.Id);

        var user = await InviteContact(EmployeeType.User);

        // Act & Assert
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Bug", "80752")]
    public async Task GetFileInfo_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Room For Unauthenticated File Info");
        var file = await CreateFile("Autotest Unauthenticated File Info", room.Id);

        // Act & Assert — anonymous access must be rejected as unauthenticated (401), not
        // as forbidden (403), since no identity was presented at all.
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
