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
/// <c>PUT /api/2.0/files/fileops/copy</c> against destinations nested several levels below a
/// room's root, and the <c>content=true</c> flag that copies a folder's contents in place of the
/// folder itself.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CopyNestedDestinationTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    [Fact]
    public async Task CopyFile_ToLevel2Subfolder_AppearsInSubfolder_NotInRoomRoot()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Level2 File.docx", myDocsId);
        var room = await CreateCustomRoom("Autotest CopyBatch Level2 Room");
        var subfolder = await CreateFolder("Autotest CopyBatch Level2 Subfolder", room.Id);

        // Act
        await CopyAndWait(BuildCopyRequest(subfolder.Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(subfolder.Id)).Should().Contain(sourceFile.Title);
        FileTitles(await GetFolderContent(room.Id)).Should().NotContain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyFile_ToLevel3Subfolder_AppearsAtCorrectLevel()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var sourceFile = await CreateFile("Autotest CopyBatch Level3 File.docx", myDocsId);
        var (room, folder3Id) = await CreateLevel3Folder("Autotest CopyBatch Level3 Room", "Autotest CopyBatch L3 Level2", "Autotest CopyBatch L3 Level3");

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [sourceFile.Id]));

        // Assert
        FileTitles(await GetFolderContent(folder3Id)).Should().Contain(sourceFile.Title);
        FileTitles(await GetFolderContent(room.Id)).Should().NotContain(sourceFile.Title);
    }

    [Fact]
    public async Task CopyMultipleFiles_ToLevel3Subfolder_AllAppearAtCorrectLevel()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        var file1 = await CreateFile("Autotest CopyBatch Multi L3 File1.docx", myDocsId);
        var file2 = await CreateFile("Autotest CopyBatch Multi L3 File2.docx", myDocsId);
        var (_, folder3Id) = await CreateLevel3Folder("Autotest CopyBatch Multi L3 Room", "Autotest Multi L3 L2", "Autotest Multi L3 L3");

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, fileIds: [file1.Id, file2.Id]));

        // Assert
        var destTitles = FileTitles(await GetFolderContent(folder3Id));
        destTitles.Should().Contain(file1.Title);
        destTitles.Should().Contain(file2.Title);
    }

    [Fact]
    public async Task CopyFolder_ToLevel3Subfolder_FolderAndContentsAppearAtCorrectLevel()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string sourceFolderTitle = "Autotest CopyBatch Folder to L3";
        var sourceFolder = await CreateFolder(sourceFolderTitle, myDocsId);
        const string innerFileTitle = "Autotest CopyBatch Folder to L3 Inner.docx";
        await CreateFile(innerFileTitle, sourceFolder.Id);
        var (_, folder3Id) = await CreateLevel3Folder("Autotest CopyBatch Folder L3 Room", "Autotest CopyBatch Folder L3 L2", "Autotest CopyBatch Folder L3 L3");

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, folderIds: [sourceFolder.Id]));

        // Assert
        FolderTitles(await GetFolderContent(folder3Id)).Should().Contain(sourceFolderTitle);
        var copiedFolderId = await FindEntryIdByTitle(folder3Id, sourceFolderTitle, inFolders: true);
        FileTitles(await GetFolderContent(copiedFolderId)).Should().Contain(innerFileTitle);
    }

    [Fact]
    public async Task CopyLevel3Subfolder_ToAnotherRoom_SubfolderAndContentsAppearInDestination()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCustomRoom("Autotest CopyBatch L3Sub Src Room");
        var level2 = await CreateFolder("Autotest L3Sub L2", srcRoom.Id);
        const string subFolderTitle = "Autotest L3Sub Level3 Folder";
        var level3 = await CreateFolder(subFolderTitle, level2.Id);
        const string innerFileTitle = "Autotest CopyBatch L3Sub Inner File.docx";
        await CreateFile(innerFileTitle, level3.Id);
        var destRoom = await CreateCustomRoom("Autotest CopyBatch L3Sub Dest Room");

        // Act
        await CopyAndWait(BuildCopyRequest(destRoom.Id, folderIds: [level3.Id]));

        // Assert
        FolderTitles(await GetFolderContent(destRoom.Id)).Should().Contain(subFolderTitle);
        var copiedFolderId = await FindEntryIdByTitle(destRoom.Id, subFolderTitle, inFolders: true);
        FileTitles(await GetFolderContent(copiedFolderId)).Should().Contain(innerFileTitle);
    }

    [Fact]
    public async Task CopyFolder_WithContentTrue_ToLevel3Subfolder_ContentsAppearDirectly()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsId = await GetUserFolderIdAsync(Owner);
        const string srcFolderTitle = "Autotest CopyBatch ContentL3 Src";
        var srcFolder = await CreateFolder(srcFolderTitle, myDocsId);
        const string innerFileTitle = "Autotest CopyBatch ContentL3 Inner.docx";
        await CreateFile(innerFileTitle, srcFolder.Id);
        var (_, folder3Id) = await CreateLevel3Folder("Autotest CopyBatch ContentL3 Room", "Autotest ContentL3 L2", "Autotest ContentL3 L3");

        // Act
        await CopyAndWait(BuildCopyRequest(folder3Id, content: true, folderIds: [srcFolder.Id]));

        // Assert
        var content = await GetFolderContent(folder3Id);
        FileTitles(content).Should().Contain(innerFileTitle);
        FolderTitles(content).Should().NotContain(srcFolderTitle, "content=true copies the folder's contents, not the folder itself");
    }

    private async Task<(FolderDtoInteger Room, int Folder3Id)> CreateLevel3Folder(string roomTitle, string level2Title, string level3Title)
    {
        var room = await CreateCustomRoom(roomTitle);
        var level2 = await CreateFolder(level2Title, room.Id);
        var level3 = await CreateFolder(level3Title, level2.Id);

        return (room, level3.Id);
    }
}
