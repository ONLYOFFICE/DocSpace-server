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

namespace ASC.ClearEvents.Tests.Tests;

/// <summary>
/// Room history as Files writes it, through a cleanup pass: a room keeps its history however old it is, an
/// entry in the trash keeps its own, and only deleting an entry for good lets its history go. The history is
/// written by the services a moment before the pass and then aged past every retention
/// (<see cref="BaseTest.AgeAuditTrailAsync"/>), since the API cannot write an event in the past.
/// </summary>
public class RoomHistoryLifecycleTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const int PastEveryRetention = PaidLifeTimeDays * 2;

    [Fact]
    public async Task ClearAsync_Room_KeepsItsHistory()
    {
        // Arrange
        var room = await CreateCustomRoomAsync("Audit cleanup: room history");
        await CreateFileAsync(room.Id, "Kept.docx");

        var before = await PollRoomHistoryAsync(room.Id, MessageAction.RoomCreated, MessageAction.FileCreated);
        before.Should().Contain([MessageAction.RoomCreated, MessageAction.FileCreated]);

        var history = await HistoryEventsAsync(room.Id, FolderEntry);
        var outsideHistory = await SeedEventAsync(0);

        await AgeAuditTrailAsync(PastEveryRetention);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync(history)).Should().BeEquivalentTo(history);
        (await RemainingEventsAsync([outsideHistory])).Should().BeEmpty();

        var after = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.ConvertAll(e => e.Action?.Id).Should().Contain(before);
    }

    [Fact]
    public async Task ClearAsync_FileInTrash_KeepsItsHistory()
    {
        // Arrange
        var room = await CreateCustomRoomAsync("Audit cleanup: file in trash");
        var file = await CreateFileAsync(room.Id, "Trashed.docx");

        await PollRoomHistoryAsync(room.Id, MessageAction.FileCreated);

        await _filesApi.DeleteFileAsync(file.Id, new DeleteFileRequest(immediately: false), cancellationToken: TestContext.Current.CancellationToken);
        await WaitLongOperationAsync();

        var roomHistory = await PollRoomHistoryAsync(room.Id, MessageAction.FileMovedToTrash);
        roomHistory.Should().Contain(MessageAction.FileMovedToTrash);

        var fileHistory = await HistoryEventsAsync(file.Id, FileEntry);
        fileHistory.Should().NotBeEmpty("a file in the trash keeps the references to its history");

        await AgeAuditTrailAsync(PastEveryRetention);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync(fileHistory)).Should().BeEquivalentTo(fileHistory);
    }

    [Fact]
    public async Task ClearAsync_FileDeletedForGood_StaysInTheHistoryOfItsRoom()
    {
        // Arrange
        var room = await CreateCustomRoomAsync("Audit cleanup: deleted file");
        var file = await CreateFileAsync(room.Id, "Deleted.docx");

        await PollRoomHistoryAsync(room.Id, MessageAction.FileCreated);

        var fileHistory = await HistoryEventsAsync(file.Id, FileEntry);
        fileHistory.Should().NotBeEmpty();

        await _filesApi.DeleteFileAsync(file.Id, new DeleteFileRequest(immediately: true), cancellationToken: TestContext.Current.CancellationToken);
        await WaitLongOperationAsync();

        (await HistoryEventsAsync(file.Id, FileEntry)).Should().BeEmpty("deleting a file for good removes the references to its history");

        await AgeAuditTrailAsync(PastEveryRetention);

        // Act
        await RunCleanupAsync();

        // Assert: the file's events are still referenced by the room they happened in.
        (await RemainingEventsAsync(fileHistory)).Should().BeEquivalentTo(fileHistory);

        var roomHistory = (await _foldersApi.GetFolderHistoryAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        roomHistory.ConvertAll(e => e.Action?.Id).Should().Contain(MessageAction.FileCreated);
    }

    [Fact]
    public async Task ClearAsync_RoomDeletedForGood_RemovesItsHistory()
    {
        // Arrange
        var room = await CreateCustomRoomAsync("Audit cleanup: deleted room");
        await CreateFileAsync(room.Id, "Gone.docx");

        await PollRoomHistoryAsync(room.Id, MessageAction.RoomCreated, MessageAction.FileCreated);

        var history = await HistoryEventsAsync(room.Id, FolderEntry);
        history.Should().NotBeEmpty();

        await _roomsApi.DeleteRoomAsync(room.Id, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperationAsync();

        await AgeAuditTrailAsync(PastEveryRetention);

        // Act
        await RunCleanupAsync();

        // Assert
        (await RemainingEventsAsync(history)).Should().BeEmpty();
    }
}
