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

namespace ASC.Files.Tests.Tests._06_Operations.EmptyTrash;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/emptytrash</c> — access control. Every authenticated employee type
/// may empty their own Trash; the endpoint has no per-item permission check because it only ever
/// operates on the caller's own Trash. A Guest given <see cref="FileShare.ContentCreator"/> in a
/// room is no exception. Functional coverage of the endpoint itself lives in
/// <see cref="EmptyTrashTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "EmptyTrash")]
public class EmptyTrashPermissionsTests(
    AspireAppFixture fixture)
    : EmptyTrashTestBase(fixture)
{
    [Fact]
    public async Task EmptyTrash_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.EmptyTrashAsync(cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task EmptyTrash_Owner_EmptiesOwnTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest EmptyTrash Owner File.docx";
        var file = await CreateFileInMy(fileName, Owner);
        await DeleteFileToTrashAsync(file.Id);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trash = await GetTrashAsync();
        trash.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_User_EmptiesOwnTrash()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        const string fileName = "Autotest EmptyTrash User File.docx";
        var file = await CreateFileInMy(fileName, user);
        await DeleteFileToTrashAsync(file.Id);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trash = await GetTrashAsync();
        trash.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_DocSpaceAdmin_EmptiesOwnTrash()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        const string fileName = "Autotest EmptyTrash Admin File.docx";
        var file = await CreateFileInMy(fileName, admin);
        await DeleteFileToTrashAsync(file.Id);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trash = await GetTrashAsync();
        trash.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_RoomAdmin_EmptiesOwnTrash()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        const string fileName = "Autotest EmptyTrash RoomAdmin File.docx";
        var file = await CreateFileInMy(fileName, roomAdmin);
        await DeleteFileToTrashAsync(file.Id);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trash = await GetTrashAsync();
        trash.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_Guest_DoesNotTouchAnotherUsersTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest EmptyTrash Guest Owner File.docx";
        var file = await CreateFileInMy(fileName, Owner);
        await DeleteFileToTrashAsync(file.Id);

        var trashBefore = await GetTrashAsync();
        trashBefore.Files.Should().Contain(f => f.Title == fileName);

        var guest = await InviteGuest();

        // Act - a guest emptying their own (empty) trash must not touch the owner's
        await _filesClient.Authenticate(guest);
        await EmptyTrashAndWaitAsync();

        // Assert
        await _filesClient.Authenticate(Owner);
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().Contain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_Guest_EmptiesOwnTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest EmptyTrash Guest Room");

        var guest = await InviteGuest();
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = guest.Id, Access = FileShare.ContentCreator }], Notify = false },
            cancellationToken: TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(guest);
        const string fileName = "Autotest EmptyTrash Guest File.docx";
        var file = await CreateFile(fileName, room.Id);
        await DeleteFileToTrashAsync(file.Id);

        var trashBefore = await GetTrashAsync();
        trashBefore.Files.Should().Contain(f => f.Title == fileName);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().NotContain(f => f.Title == fileName);
    }
}
