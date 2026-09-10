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

namespace ASC.Files.Tests.Tests._06_Operations.Duplicate;

/// <summary>
/// Who may call <c>PUT /api/2.0/files/fileops/duplicate</c>, across roles (Owner, DocSpaceAdmin,
/// RoomAdmin, User, Guest) and room-invitation levels
/// (<see cref="FileShare.ContentCreator"/>/<see cref="FileShare.Read"/>/<see cref="FileShare.Editing"/>/
/// <see cref="FileShare.RoomManager"/>).
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class DuplicatePermissionsTests(
    AspireAppFixture fixture)
    : DuplicateTestBase(fixture)
{
    [Fact]
    public async Task DuplicateFile_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest Dup Anon File.docx", myDocsId);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task DuplicateFile_Owner_CanDuplicateOwnFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileBase = "Autotest Dup Owner File";
        var sourceFile = await CreateFile($"{fileBase}.docx", myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(myDocsId)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_UserWithContentCreatorInRoom_CanDuplicate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup User ContentCreator Room");
        const string fileBase = "Autotest Dup User ContentCreator File";
        var sourceFile = await CreateFile($"{fileBase}.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        await _filesClient.Authenticate(Owner);
        CountTitlesContaining(FileTitles(await GetFolderContent(room.Id)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_UserWithoutAccess_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest Dup No Access File.docx", myDocsId);

        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateFile_RoomAdmin_CanDuplicateInOwnRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup RoomAdmin Room");
        const string fileBase = "Autotest Dup RoomAdmin File";
        var sourceFile = await CreateFile($"{fileBase}.docx", room.Id);

        var roomAdmin = await InviteMember(EmployeeType.RoomAdmin);
        await InviteToRoom(room.Id, roomAdmin, FileShare.RoomManager);

        await _filesClient.Authenticate(roomAdmin);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        await _filesClient.Authenticate(Owner);
        CountTitlesContaining(FileTitles(await GetFolderContent(room.Id)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DuplicateFile_GuestWithReadAccess_ReturnsForbidden()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup Guest Room");
        var sourceFile = await CreateFile("Autotest Dup Guest File.docx", room.Id);

        var guest = await InviteMember(EmployeeType.Guest);
        await InviteToRoom(room.Id, guest, FileShare.Read);

        await _filesClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateFile_UserWithEditingAccess_ReturnsForbidden()
    {
        // Arrange: Editing allows editing existing files but not adding new content.
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Dup Editing Room");
        var sourceFile = await CreateFile("Autotest Dup Editing File.docx", room.Id);

        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Editing);

        await _filesClient.Authenticate(user);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.DuplicateBatchItemsAsync(
                BuildDuplicateRequest(fileIds: [sourceFile.Id]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DuplicateFile_DocSpaceAdmin_CanDuplicateOwnFile()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var myDocsId = await GetUserFolderIdAsync(admin);
        const string fileBase = "Autotest Dup DSAdmin File";
        var sourceFile = await CreateFile($"{fileBase}.docx", myDocsId);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(fileIds: [sourceFile.Id]));

        // Assert
        CountTitlesContaining(FileTitles(await GetFolderContent(myDocsId)), fileBase).Should().BeGreaterThanOrEqualTo(2);
    }
}
