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

namespace ASC.Files.Tests.Tests._06_Operations.Move;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/move</c> against a third-party (Nextcloud) destination room. Both
/// cases need a reachable Nextcloud (<c>RequireNextcloud</c>, inherited below) and skip otherwise.
/// Reuses the Nextcloud connection helpers from the rooms/third-party suite rather than
/// duplicating them - see the base class for why WebDAV/Nextcloud is the only provider these tests
/// can drive. The destination room's folder id is string-typed (third-party rooms never get an
/// int id), so this suite cannot read its content back through the typed SDK - only int-keyed
/// folders (<c>GetFolderByFolderIdAsync</c>) are generated - and sticks to observing the source
/// folder and the operation result instead.
/// </summary>
[Trait("Category", "Bug")]
[Trait("Feature", "Files")]
public class ThirdPartyMoveBugTests(
    AspireAppFixture fixture)
    : ASC.Files.Tests.Tests._03_Rooms.ThirdParty.ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// BUG 82242: moving into a third-party room with <c>Skip</c> conflict resolution does not
    /// skip - the file is moved (and renamed by Nextcloud to avoid the name clash) instead of
    /// staying untouched in the source: Skip has to leave both copies where they are.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82242")]
    public async Task MoveBatchItems_SkipConflictToThirdPartyRoom_LeavesSourceUntouched()
    {
        RequireNextcloud();

        // Arrange
        await _filesClient.Authenticate(Owner);
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);

        var connected = await ConnectNextcloud("Autotest MoveBatch TP Skip");
        var destRoom = (await _roomsApi.CreateRoomThirdPartyAsync(
            connected.Id,
            new CreateThirdPartyRoom(title: "Autotest MoveBatch TP Skip Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        var fileTitle = "Autotest MoveBatch TP Skip Conflict.docx";
        var file1 = await CreateFile(fileTitle, myDocsFolderId);

        // A pre-existing conflict at the destination: file1 copied there first with Overwrite.
        await _filesOperationsApi.CopyBatchItemsAsync(new BatchRequestDto
        {
            FileIds = [new(file1.Id)],
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Overwrite,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        var file2 = await CreateFile(fileTitle, myDocsFolderId);
        var srcCountBeforeMove = (await _foldersApi.GetFolderByFolderIdAsync(
            myDocsFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response.Files.Count(f => f.Title == fileTitle);
        srcCountBeforeMove.Should().Be(2, "both file1 and file2 share the same title before the move");

        // Act
        await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            FileIds = [new(file2.Id)],
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            DeleteAfter = false
        }, TestContext.Current.CancellationToken);
        await WaitLongOperation();

        // Assert
        var srcCountAfterSkip = (await _foldersApi.GetFolderByFolderIdAsync(
            myDocsFolderId, cancellationToken: TestContext.Current.CancellationToken)).Response.Files.Count(f => f.Title == fileTitle);

        srcCountAfterSkip.Should().Be(2, "Skip leaves the conflicting file in the source folder");
    }

    /// <remarks>
    /// BUG 83271: moving a folder from a Collaboration room to a third-party room used to fail
    /// inside the operation with "Object reference not set to an instance of an object". Recorded
    /// here as a normal (non-<c>test.fail</c>) case in the TypeScript suite - the reproduction below
    /// does not trigger it, so this documents the expected, non-throwing outcome and keeps the bug
    /// number attached for whichever narrower repro still fails.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83271")]
    public async Task MoveBatchItems_FolderFromCollaborationRoomToThirdPartyRoom_CompletesWithoutError()
    {
        RequireNextcloud();

        // Arrange
        await _filesClient.Authenticate(Owner);
        var srcRoom = await CreateCollaborationRoom("Autotest MoveBatch TP Src Room");
        var folder = await CreateFolder("Autotest MoveBatch ThirdParty Collab Folder", srcRoom.Id);

        var connected = await ConnectNextcloud("Autotest MoveBatch TP Collab Folder");
        var destRoom = (await _roomsApi.CreateRoomThirdPartyAsync(
            connected.Id,
            new CreateThirdPartyRoom(title: "Autotest MoveBatch ThirdParty Collab Room", roomType: RoomType.CustomRoom),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(new BatchRequestDto
        {
            FolderIds = [new(folder.Id)],
            DestFolderId = new(destRoom.Id),
            ConflictResolveType = FileConflictResolveType.Skip,
            DeleteAfter = false,
            ReturnSingleOperation = true
        }, TestContext.Current.CancellationToken)).Response;

        results.Should().NotBeEmpty();
        results[0].Operation.Should().Be(FileOperationType.Move);

        var finished = await WaitLongOperation(results[0].Id);

        // Assert
        finished.Should().NotBeNull();
        finished!.TrueForAll(r => r.Finished).Should().BeTrue();
        finished.Should().NotContain(r => !string.IsNullOrEmpty(r.Error));
    }
}
