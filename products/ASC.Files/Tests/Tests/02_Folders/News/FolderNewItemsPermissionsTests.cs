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

namespace ASC.Files.Tests.Tests._02_Folders.News;

/// <summary>
/// Access control for <c>GET /api/2.0/files/{folderId}/news</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Folders")]
public class FolderNewItemsPermissionsTests(
    AspireAppFixture fixture)
    : FolderNewItemsTestBase(fixture)
{
    [Fact]
    public async Task News_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Perm Anon");

        // Act
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task News_NoRoomAccess_Returns403(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Folder News Perm No Access {employeeType}");

        var member = await InviteMember(employeeType);

        // Act
        await _filesClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task News_ReadAccess_Returns200(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom($"Autotest Folder News Perm Read {employeeType}");

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, FileShare.Read);

        // Act
        await _filesClient.Authenticate(member);
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task News_DocSpaceAdmin_ReturnsForAnyRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Perm DocSpaceAdmin");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // Act
        await _filesClient.Authenticate(admin);
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task News_RoomAdmin_MemberOfRoom_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Perm RoomAdmin");

        // Only a RoomAdmin may be granted RoomManager.
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        // Act
        await _filesClient.Authenticate(roomAdmin);
        var news = (await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        news.Should().NotBeNull();
    }

    [Fact]
    public async Task News_RoomAdmin_NotMemberOfRoom_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Folder News Perm RoomAdmin No Access");

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);

        // Act
        await _filesClient.Authenticate(roomAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetNewFolderItemsAsync(room.Id, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
