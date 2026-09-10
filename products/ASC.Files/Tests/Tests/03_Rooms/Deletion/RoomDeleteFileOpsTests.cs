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

namespace ASC.Files.Tests.Tests._03_Rooms.Deletion;

/// <summary>
/// Room-level batch file operations that sit next to deletion: <c>PUT /files/fileops/duplicate</c>
/// against a whole room, and <c>PUT /files/fileops/delete</c> against a room holding a file that is
/// currently opened for editing.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomDeleteFileOpsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task DuplicateBatchItems_OwnerDuplicatesOwnRoom_Finishes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Room To Duplicate");

        // Act
        var results = (await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto { FolderIds = [new(room.Id)] },
            TestContext.Current.CancellationToken)).Response;

        if (results.Exists(r => !r.Finished))
        {
            results = await WaitLongOperation(results[0].Id);
        }

        // Assert
        results.Should().OnlyContain(r => r.Finished && r.Error == "");
    }

    [Fact]
    [Trait("Bug", "81232")]
    public async Task DuplicateBatchItems_OwnerDuplicatesDocSpaceAdminsRoom_AppearsInOwnersRoomList()
    {
        // Arrange
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        var room = await CreateCustomRoom("Autotest Admin Room For Owner Duplicate");

        await _filesClient.Authenticate(Owner);

        // Act
        var results = (await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto { FolderIds = [new(room.Id)] },
            TestContext.Current.CancellationToken)).Response;

        if (results.Exists(r => !r.Finished))
        {
            results = await WaitLongOperation(results[0].Id);
        }

        // Assert
        results.Should().OnlyContain(r => r.Finished && r.Error == "");

        var list = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        list.Folders.Should().Contain(f => f.Title != null && f.Title.Contains("Autotest Admin Room For Owner Duplicate"));
    }

    /// <summary>
    /// BUG 81287: deleting a room with an open file removed part of the content before failing, so
    /// the delete was not atomic. Fixed by pre-validating the whole subtree in
    /// <c>DeletePermissionsCheck.CheckSubtreeFilesPermissionsAsync</c> — one blocked file now fails
    /// the folder delete before anything is removed.
    /// </summary>
    [Fact]
    [Trait("Bug", "81287")]
    public async Task DeleteBatchItems_RoomWithOpenFile_RollsBackAtomicallyWithClearError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Delete Room With Open File");

        var file1 = await CreateFile("file1", room.Id);
        var file2 = await CreateFile("file2", room.Id);
        var openedFile = await CreateFile("opened-file", room.Id);

        var editConfig = (await _filesApi.OpenEditFileAsync(openedFile.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var docKey = editConfig.Document.Key;
        await _filesApi.TrackEditFileAsync(openedFile.Id, Guid.NewGuid(), docKey, false, TestContext.Current.CancellationToken);

        // Act - start room deletion
        var response = await _filesOperationsApi.DeleteBatchItemsWithHttpInfoAsync(
            new DeleteBatchRequestDto { FolderIds = [new(room.Id)], Immediately = true },
            TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var operations = await WaitLongOperation();

        // Assert - the operation reports the open-file conflict rather than silently succeeding
        operations.Should().OnlyContain(o => o.Finished);
        operations.Should().Contain(o => o.Error != null && o.Error.Contains("opened for editing"));

        // Assert - the room and every one of its files must still exist (atomic rollback)
        var roomInfo = await _roomsApi.GetRoomInfoWithHttpInfoAsync(room.Id, TestContext.Current.CancellationToken);
        roomInfo.StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var fileId in new[] { file1.Id, file2.Id, openedFile.Id })
        {
            var fileInfo = await _filesApi.GetFileInfoWithHttpInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
            fileInfo.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
