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

namespace ASC.Files.Tests.Tests._06_Operations.Comment;

/// <summary>
/// <c>PUT /api/2.0/files/file/{fileId}/comment</c> — access control. Updating a comment needs
/// write access to the file: the file's own owner (or a DocSpaceAdmin/user acting on their own
/// file) succeeds, while <see cref="FileShare.Editing"/> and <see cref="FileShare.Read"/> in a
/// room someone else owns, and no access at all, are all rejected.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class CommentPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task Comment_Owner_OnOwnFile_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Perm Owner.docx", Owner);

        // Act & Assert
        await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Owner comment"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Comment_DocSpaceAdmin_OnOwnFile_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        var file = await CreateFileInMy("Autotest UpdateComment Perm Admin.docx", admin);

        // Act & Assert
        await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Admin comment"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Comment_User_OnOwnFile_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(user);
        var file = await CreateFileInMy("Autotest UpdateComment Perm User.docx", user);

        // Act & Assert
        await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "User comment"), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Comment_UserWithEditingAccess_OnAnotherUsersRoomFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest UpdateComment Perm Editor Room");
        var file = await CreateFile("Autotest UpdateComment Perm Editor File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Editor comment"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Comment_UserWithReadAccess_OnRoomFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest UpdateComment Perm Reader Room");
        var file = await CreateFile("Autotest UpdateComment Perm Reader File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Reader comment"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Comment_GuestWithReadAccess_OnRoomFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest UpdateComment Perm Guest Room");
        var file = await CreateFile("Autotest UpdateComment Perm Guest File.docx", room.Id);

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        // Act
        await _filesClient.Authenticate(guest);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Guest comment"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Comment_User_OnAnotherUsersFile_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Perm NoAccess.docx", Owner);

        var user = await InviteMember(EmployeeType.User);

        // Act
        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Unauthorized"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task Comment_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Perm Anon.docx", Owner);

        // Act
        await _filesClient.Authenticate(null);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Anon"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
