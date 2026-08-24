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
/// <c>PUT /api/2.0/files/fileops/emptytrash</c> — functional coverage: emptying a Trash holding a
/// file, a folder, a mix of both, an already-empty Trash, and a file trashed from inside a room.
/// Access control lives in <see cref="EmptyTrashPermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "EmptyTrash")]
public class EmptyTrashTests(
    AspireAppFixture fixture)
    : EmptyTrashTestBase(fixture)
{
    [Fact]
    public async Task EmptyTrash_OneFileInTrash_FileRemoved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest EmptyTrash File.docx";
        var file = await CreateFileInMy(fileName, Owner);
        await DeleteFileToTrashAsync(file.Id);

        var trashBefore = await GetTrashAsync();
        trashBefore.Files.Should().Contain(f => f.Title == fileName);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().NotContain(f => f.Title == fileName);
    }

    [Fact]
    public async Task EmptyTrash_OneFolderInTrash_FolderRemoved()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string folderTitle = "Autotest EmptyTrash Folder";
        var folder = await CreateFolderInMy(folderTitle, Owner);
        await DeleteFolderToTrashAsync(folder.Id);

        var trashBefore = await GetTrashAsync();
        trashBefore.Folders.Should().Contain(f => f.Title == folderTitle);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trashAfter = await GetTrashAsync();
        trashAfter.Folders.Should().NotContain(f => f.Title == folderTitle);
    }

    [Fact]
    public async Task EmptyTrash_MixedFileAndFolder_TrashEmptiedOfBoth()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest EmptyTrash Mixed File.docx";
        const string folderTitle = "Autotest EmptyTrash Mixed Folder";

        var file = await CreateFileInMy(fileName, Owner);
        var folder = await CreateFolderInMy(folderTitle, Owner);

        await DeleteFileToTrashAsync(file.Id);
        await DeleteFolderToTrashAsync(folder.Id);

        // Act
        await EmptyTrashAndWaitAsync();

        // Assert
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().NotContain(f => f.Title == fileName);
        trashAfter.Folders.Should().NotContain(f => f.Title == folderTitle);
    }

    [Fact]
    public async Task EmptyTrash_AlreadyEmpty_ReturnsOk()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act & Assert - the endpoint accepts the request even when there is nothing to delete
        await EmptyTrashAndWaitAsync();
    }

    [Fact]
    public async Task EmptyTrash_FileDeletedFromRoom_RemovedFromPersonalTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest EmptyTrash Room");

        const string fileName = "Autotest EmptyTrash Room File.docx";
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

    [Fact]
    public async Task EmptyTrash_SingleTrue_TrashEmptiedOfFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        const string fileName = "Autotest EmptyTrash Single.docx";
        var file = await CreateFileInMy(fileName, Owner);
        await DeleteFileToTrashAsync(file.Id);

        // Act
        await EmptyTrashAndWaitAsync(single: true);

        // Assert
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().NotContain(f => f.Title == fileName);
    }

    /// <summary>
    /// <c>emptytrash</c> has no <c>rootFolderType</c> parameter, so it empties the entire unified
    /// Trash regardless of which section the request names - the UI calls
    /// <c>emptytrash?single=true&amp;folderType=USER</c> from the Files section expecting only "My
    /// documents" items to be cleared, but Rooms and Forms-room items are cleared too.
    /// </summary>
    [Fact]
    [Trait("Bug", "82588")]
    public async Task EmptyTrash_EmptyingFilesSectionOnly_DoesNotTouchRoomsOrFormsSectionTrash()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var filesTitle = $"Autotest ET Files {Guid.NewGuid():N}.docx";
        var roomsTitle = $"Autotest ET Rooms {Guid.NewGuid():N}.docx";
        var formsTitle = $"Autotest ET Forms {Guid.NewGuid():N}.docx";

        var myFile = await CreateFileInMy(filesTitle, Owner);
        await DeleteFileToTrashAsync(myFile.Id);

        var room = await CreateCustomRoom($"Autotest EmptyTrash Room {Guid.NewGuid():N}");
        var roomFile = await CreateFile(roomsTitle, room.Id);
        await DeleteFileToTrashAsync(roomFile.Id);

        // The TS suite builds this file through a docx -> docxf -> pdf-form conversion pipeline
        // (createOoForm) purely to land it inside a Filling Forms room. That conversion has no
        // equivalent helper elsewhere in this suite and is unrelated to what the bug is about, so a
        // plain file inside a Filling Forms room stands in for it here.
        var formRoom = await CreateFillingFormsRoom($"Autotest EmptyTrash Form Room {Guid.NewGuid():N}");
        var formFile = await CreateFile(formsTitle, formRoom.Id);
        await DeleteFileToTrashAsync(formFile.Id);

        var trashBefore = await GetTrashAsync();
        trashBefore.Files.Should().Contain(f => f.Title == filesTitle);
        trashBefore.Files.Should().Contain(f => f.Title == roomsTitle);
        trashBefore.Files.Should().Contain(f => f.Title == formsTitle);

        // Act - empty trash from the Files section only
        await EmptyTrashForFolderTypesAndWaitAsync(true, FolderType.USER);

        // Assert - the Files section item is gone, but Rooms and Forms-room items must remain
        var trashAfter = await GetTrashAsync();
        trashAfter.Files.Should().NotContain(f => f.Title == filesTitle);
        trashAfter.Files.Should().Contain(f => f.Title == roomsTitle);
        trashAfter.Files.Should().Contain(f => f.Title == formsTitle);
    }
}
