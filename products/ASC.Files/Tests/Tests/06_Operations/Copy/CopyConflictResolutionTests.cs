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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// How <c>PUT /api/2.0/files/fileops/copy</c> resolves a name conflict at the destination
/// (<see cref="FileConflictResolveType.Skip"/>/<see cref="FileConflictResolveType.Overwrite"/>/
/// <see cref="FileConflictResolveType.Duplicate"/>), both directly under a room and three levels
/// deep.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CopyConflictResolutionTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    [Fact]
    public async Task CopyFile_SkipConflict_LeavesDestinationUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Skip Conflict.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Skip Room");

        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));
        var existingId = await FindEntryIdByTitle(destRoom.Id, sourceFile.Title, inFolders: false);

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Assert
        var content = await GetFolderContent(destRoom.Id);
        content.Files.Should().HaveCount(1);
        (await FindEntryIdByTitle(destRoom.Id, sourceFile.Title, inFolders: false)).Should().Be(existingId);
    }

    [Fact]
    public async Task CopyFile_OverwriteConflict_UpdatesVersionInPlace()
    {
        // Arrange: Overwrite = version update on the same file entry, no second file created.
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileTitle = "Autotest CopyBatch Overwrite Conflict.docx";
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Overwrite Room");

        var file1 = await CreateFile(fileTitle, myDocsId);
        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [file1.Id]));
        var originalId = await FindEntryIdByTitle(destRoom.Id, fileTitle, inFolders: false);

        var file2 = await CreateFile(fileTitle, myDocsId);

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, FileConflictResolveType.Overwrite, fileIds: [file2.Id]));

        // Assert
        var content = await GetFolderContent(destRoom.Id);
        content.Files.Should().HaveCount(1);
        (await FindEntryIdByTitle(destRoom.Id, fileTitle, inFolders: false)).Should().Be(originalId);
    }

    [Fact]
    public async Task CopyFile_DuplicateConflict_CreatesSecondCopy()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Duplicate Conflict.docx", myDocsId);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch Duplicate Room");

        await CopyAndWait(BuildCopyRequest(destRoom.Id, fileIds: [sourceFile.Id]));

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, FileConflictResolveType.Duplicate, fileIds: [sourceFile.Id]));

        // Assert
        (await GetFolderContent(destRoom.Id)).Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task CopyFile_ToSameRoomWithDuplicate_CreatesSecondCopyInSameLocation()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest CopyBatch SameRoom Dup");
        var sourceFile = await CreateFile("Autotest CopyBatch SameRoom File.docx", room.Id);

        // Act
        await CopyAndWait(BuildCopyRequest(room.Id, FileConflictResolveType.Duplicate, fileIds: [sourceFile.Id]));

        // Assert
        (await GetFolderContent(room.Id)).Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task CopyFile_SkipConflictAtLevel3_LeavesDestinationUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Skip L3 Conflict.docx", myDocsId);
        var folder3Id = await CreateLevel3Folder("Autotest CopyBatch Skip L3 Room", "Autotest Skip L3 L2", "Autotest Skip L3 L3");

        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [sourceFile.Id]));
        var existingId = await FindEntryIdByTitle(folder3Id, sourceFile.Title, inFolders: false);

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [sourceFile.Id]));

        // Assert
        var content = await GetFolderContent(folder3Id);
        content.Files.Should().HaveCount(1);
        (await FindEntryIdByTitle(folder3Id, sourceFile.Title, inFolders: false)).Should().Be(existingId);
    }

    [Fact]
    public async Task CopyFile_OverwriteConflictAtLevel3_UpdatesVersionInPlace()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string fileTitle = "Autotest CopyBatch Overwrite L3.docx";
        var folder3Id = await CreateLevel3Folder("Autotest CopyBatch Overwrite L3 Room", "Autotest Overwrite L3 L2", "Autotest Overwrite L3 L3");

        var file1 = await CreateFile(fileTitle, myDocsId);
        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [file1.Id]));
        var originalId = await FindEntryIdByTitle(folder3Id, fileTitle, inFolders: false);

        var file2 = await CreateFile(fileTitle, myDocsId);

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, FileConflictResolveType.Overwrite, fileIds: [file2.Id]));

        // Assert
        var content = await GetFolderContent(folder3Id);
        content.Files.Should().HaveCount(1);
        (await FindEntryIdByTitle(folder3Id, fileTitle, inFolders: false)).Should().Be(originalId);
    }

    [Fact]
    public async Task CopyFile_DuplicateConflictAtLevel3_CreatesSecondCopy()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Dup L3.docx", myDocsId);
        var folder3Id = await CreateLevel3Folder("Autotest CopyBatch Dup L3 Room", "Autotest Dup L3 L2", "Autotest Dup L3 L3");

        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [sourceFile.Id]));

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, FileConflictResolveType.Duplicate, fileIds: [sourceFile.Id]));

        // Assert
        (await GetFolderContent(folder3Id)).Files.Should().HaveCount(2);
    }

    private async Task<int> CreateLevel3Folder(string roomTitle, string level2Title, string level3Title)
    {
        var room = await CreateCustomRoom(roomTitle);
        var level2 = await CreateFolder(level2Title, room.Id);
        var level3 = await CreateFolder(level3Title, level2.Id);

        return level3.Id;
    }
}
