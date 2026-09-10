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

namespace ASC.Files.Tests.Tests._06_Operations.MarkAsRead;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/markasread</c> — functional coverage: a single file, several
/// files, a folder, a mix of both, an idempotent re-read, an empty/absent body and a non-existent
/// id. Access control lives in <see cref="MarkAsReadPermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class MarkAsReadTests(
    AspireAppFixture fixture)
    : MarkAsReadTestBase(fixture)
{
    [Fact]
    public async Task MarkAsRead_SingleFile_ClearsNewsBadge()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkAsRead Single File");

        await _filesClient.Authenticate(Owner);
        var file = await CreateFile("Autotest MarkAsRead File.docx", room.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkAsRead File.docx"));

        // Act
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            MarkAsReadFiles(file.Id), TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeEmpty();
        results[0].Operation.Should().Be(FileOperationType.MarkAsRead);

        var titles = await PollRoomNewsTitles(room.Id, t => !t.Contains("Autotest MarkAsRead File.docx"));
        titles.Should().NotContain("Autotest MarkAsRead File.docx");
    }

    [Fact]
    public async Task MarkAsRead_MultipleFiles_RemovesBothFromNewItems()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkAsRead Multi Files");

        await _filesClient.Authenticate(Owner);
        var file1 = await CreateFile("Autotest MarkAsRead File1.docx", room.Id);
        var file2 = await CreateFile("Autotest MarkAsRead File2.docx", room.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkAsRead File1.docx") && t.Contains("Autotest MarkAsRead File2.docx"));

        // Act
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            MarkAsReadFiles(file1.Id, file2.Id), TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeEmpty();
        results[0].Operation.Should().Be(FileOperationType.MarkAsRead);

        var titles = await PollRoomNewsTitles(room.Id, t => !t.Contains("Autotest MarkAsRead File1.docx") && !t.Contains("Autotest MarkAsRead File2.docx"));
        titles.Should().NotContain(["Autotest MarkAsRead File1.docx", "Autotest MarkAsRead File2.docx"]);
    }

    /// <remarks>
    /// Subfolders never appear in <c>GetNewRoomItems</c> - only files do - so this only checks the
    /// technical response: a folder id is accepted and reported as a MarkAsRead operation.
    /// </remarks>
    [Fact]
    public async Task MarkAsRead_Folder_ReturnsMarkAsReadOperation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MarkAsRead Folder");
        var subFolder = await CreateFolder("Autotest MarkAsRead Subfolder", room.Id);

        // Act
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            MarkAsReadFolders(subFolder.Id), TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeEmpty();
        results[0].Operation.Should().Be(FileOperationType.MarkAsRead);
    }

    [Fact]
    public async Task MarkAsRead_FileAndFolder_ReturnsMarkAsReadOperation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MarkAsRead Mix");
        var file = await CreateFile("Autotest MarkAsRead Mix File.docx", room.Id);
        var folder = await CreateFolder("Autotest MarkAsRead Mix Folder", room.Id);

        // Act
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            new BaseBatchRequestDto(
                folderIds: [new BaseBatchRequestDtoAllOfFolderIds(folder.Id)],
                fileIds: [new BaseBatchRequestDtoAllOfFileIds(file.Id)]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeEmpty();
        results[0].Operation.Should().Be(FileOperationType.MarkAsRead);
    }

    [Fact]
    public async Task MarkAsRead_NoBody_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert - sent raw: the generated client drops the Content-Type header together with
        // the body, so a bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _filesClient.PutAsync("api/2.0/files/fileops/markasread", content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsRead_EmptyFileIds_ReturnsArray()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            new BaseBatchRequestDto(fileIds: []), TestContext.Current.CancellationToken)).Response;

        // Assert
        results.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsRead_NonExistentFileId_Returns200()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert - a non-existent id is silently skipped, not rejected.
        await _filesOperationsApi.MarkAsReadAsync(
            MarkAsReadFiles(999999999), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MarkAsRead_AlreadyReadFile_IsIdempotent()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkAsRead Idempotent");

        await _filesClient.Authenticate(Owner);
        var file = await CreateFile("Autotest MarkAsRead Idempotent File.docx", room.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkAsRead Idempotent File.docx"));

        await _filesOperationsApi.MarkAsReadAsync(MarkAsReadFiles(file.Id), TestContext.Current.CancellationToken);
        await PollRoomNewsTitles(room.Id, t => !t.Contains("Autotest MarkAsRead Idempotent File.docx"));

        // Act & Assert - reading an already-read file again must still succeed.
        await _filesOperationsApi.MarkAsReadAsync(MarkAsReadFiles(file.Id), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MarkAsRead_Folder_RemovesItsFilesFromNewItems()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkAsRead Folder Business");

        await _filesClient.Authenticate(Owner);
        var subFolder = await CreateFolder("Autotest MarkAsRead Folder Business Sub", room.Id);
        await CreateFile("Autotest MarkAsRead Folder File.docx", subFolder.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkAsRead Folder File.docx"));

        // Act
        await _filesOperationsApi.MarkAsReadAsync(MarkAsReadFolders(subFolder.Id), TestContext.Current.CancellationToken);

        // Assert
        var titles = await PollRoomNewsTitles(room.Id, t => !t.Contains("Autotest MarkAsRead Folder File.docx"));
        titles.Should().NotContain("Autotest MarkAsRead Folder File.docx");
    }

    /// <remarks>
    /// BUG 83509: <c>markasread</c> has no root-type/section parameter, so it clears the unified
    /// "new" flag regardless of which section's root folder id was passed. Marking only the Files
    /// section root as read also clears the news of an unrelated room the same user owns, even
    /// though each root section is supposed to track new items independently (the same root cause
    /// as BUG 82588 for emptytrash).
    /// </remarks>
    [Fact]
    [Trait("Bug", "83509")]
    public async Task MarkAsRead_FilesSectionRoot_LeavesUnrelatedRoomNewsAlone()
    {
        // Arrange - a file in the owner's My Documents shared with an invited user, plus a room
        // file created by that user, so the owner has independent "new" items in both sections.
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        var member = await InviteMember(EmployeeType.User);

        var sharedFile = await CreateFile("Autotest MarkAsRead Cross Files " + Guid.NewGuid().ToString("N")[..8] + ".docx", myDocsFolderId);
        await _sharingApi.SetFileSecurityInfoAsync(
            sharedFile.Id,
            new SecurityInfoSimpleRequestDto { Share = [new() { ShareTo = member.Id, Access = FileShare.ReadWrite }], Notify = false },
            TestContext.Current.CancellationToken);

        var room = await CreateCustomRoom("Autotest MarkAsRead Cross Room " + Guid.NewGuid().ToString("N")[..6]);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        var filesTitle = "Autotest MarkAsRead Cross Files Edited " + Guid.NewGuid().ToString("N")[..8] + ".docx";
        await _filesApi.UpdateFileAsync(sharedFile.Id, new UpdateFile(filesTitle), TestContext.Current.CancellationToken);

        var roomsTitle = "Autotest MarkAsRead Cross Rooms " + Guid.NewGuid().ToString("N")[..8] + ".docx";
        await CreateFile(roomsTitle, room.Id);

        await _filesClient.Authenticate(Owner);

        // Precondition - both files are new for the owner, each in its own section.
        var filesNewsBefore = await PollFolderNewsTitles(myDocsFolderId, t => t.Contains(filesTitle));
        filesNewsBefore.Should().Contain(filesTitle);

        var roomsNewsBefore = await PollRoomNewsTitles(room.Id, t => t.Contains(roomsTitle));
        roomsNewsBefore.Should().Contain(roomsTitle);

        // Act - mark only the Files section root as read.
        await _filesOperationsApi.MarkAsReadAsync(MarkAsReadFolders(myDocsFolderId), TestContext.Current.CancellationToken);

        // Assert - the Files section news is cleared...
        var filesNewsAfter = await PollFolderNewsTitles(myDocsFolderId, t => !t.Contains(filesTitle));
        filesNewsAfter.Should().NotContain(filesTitle);

        // ...and the unrelated room's news is left untouched.
        var roomsNewsAfter = await PollRoomNewsTitles(room.Id, t => t.Contains(roomsTitle));
        roomsNewsAfter.Should().Contain(roomsTitle, "markasread clears only the section whose root was passed");
    }
}
