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

namespace ASC.Files.Tests.Tests._01_Files.EditDiff;

/// <summary>GET /files/file/{fileId}/edit/diff - access control.</summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class GetEditDiffPermissionsTests(
    AspireAppFixture fixture)
    : EditDiffTestBase(fixture)
{
    [Fact]
    public async Task GetEditDiff_Owner_CanGetDiffUrlOfTheirFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Edit Diff Perm Owner", Owner);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_DocSpaceAdminWithRoomManagerRole_CanGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff DocSpaceAdmin Room");

        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await InviteToRoom(room.Id, admin, FileShare.RoomManager);

        var file = await CreateFile("Autotest Edit Diff DocSpaceAdmin File", room.Id);

        await _filesClient.Authenticate(admin);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_UserWithEditingRole_CanGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff User Editing Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        var file = await CreateFile("Autotest Edit Diff Editing File", room.Id);

        await _filesClient.Authenticate(user);

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_UserWithCommentRole_CannotGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff User Comment Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Comment);

        var file = await CreateFile("Autotest Edit Diff Comment File", room.Id);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetEditDiff_UserWithReadAccess_CannotGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff User Read Room");

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        var file = await CreateFile("Autotest Edit Diff Read File", room.Id);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetEditDiff_UserWithoutRoomAccess_CannotGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff No Access Room");

        var user = await InviteMember(EmployeeType.User);

        var file = await CreateFile("Autotest Edit Diff No Access File", room.Id);

        await _filesClient.Authenticate(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetEditDiff_Unauthenticated_CannotGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Edit Diff Anon", Owner);

        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetEditDiff_GuestWithReadAccess_CannotGetDiffUrl()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Edit Diff Guest Room");

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        var file = await CreateFile("Autotest Edit Diff Guest File", room.Id);

        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetEditDiff_DocSpaceAdmin_CanGetDiffUrlInOwnRoom()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var (_, file) = await CreateRoomWithFile("Autotest Edit Diff DocSpaceAdmin Own Room", "Autotest Edit Diff DocSpaceAdmin Own File");

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetEditDiff_RoomAdmin_CanGetDiffUrlInOwnRoom()
    {
        // Arrange
        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var (_, file) = await CreateRoomWithFile("Autotest Edit Diff RoomAdmin Own Room", "Autotest Edit Diff RoomAdmin Own File");

        // Act
        var diff = (await _filesApi.GetEditDiffUrlAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        diff.Key.Should().NotBeNullOrEmpty();
        diff.Url.Should().NotBeNullOrEmpty();
    }
}
