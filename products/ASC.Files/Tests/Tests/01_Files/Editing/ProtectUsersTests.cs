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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// <c>GET /files/file/{fileId}/protectusers</c> - the people offered when a document is protected: everybody the
/// file is shared with, the caller left out, ordered by display name; an unknown id is refused with 403.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class ProtectUsersTests(AspireAppFixture fixture) : EditingTestBase(fixture)
{
    [Fact]
    public async Task GetProtectedFileUsers_RoomFile_ListsTheMembersWithoutTheCaller()
    {
        // Arrange
        var (room, file) = await CreateRoomWithFile("Autotest Protect Room", "Autotest Protect File.docx");
        var editor = await InviteContact(EmployeeType.User);
        var reader = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, editor, FileShare.Editing);
        await InviteToRoom(room.Id, reader, FileShare.Read);

        // Act
        var users = (await _filesApi.GetProtectedFileUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        users.Select(u => u.Id).Should().Contain([editor.Id.ToString(), reader.Id.ToString()])
            .And.NotContain(Owner.Id.ToString())
            .And.OnlyHaveUniqueItems();
        users.Single(u => u.Id == editor.Id.ToString()).Email.Should().BeEquivalentTo(editor.Email);
        users.Should().OnlyContain(u => !string.IsNullOrEmpty(u.Name));
    }

    [Fact]
    public async Task GetProtectedFileUsers_AsMember_ListsTheOwnerButNotTheCaller()
    {
        // Arrange
        var (room, file) = await CreateRoomWithFile("Autotest Protect Member Room", "Autotest Protect Member File.docx");
        var editor = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, editor, FileShare.Editing);
        await _filesClient.Authenticate(editor);

        // Act
        var users = (await _filesApi.GetProtectedFileUsersAsync(file.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        users.Select(u => u.Id).Should().Contain(Owner.Id.ToString()).And.NotContain(editor.Id.ToString());
    }

    [Fact]
    public async Task GetProtectedFileUsers_UnknownFile_ReturnsForbidden()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetProtectedFileUsersAsync(999999999, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
