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

namespace ASC.Files.Tests.Tests._06_Operations.Statuses;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/terminate/{id}</c> on operations that really are running: cancelling stops
/// the job for good, keeps what was already done, loses nothing, and touches no other operation. Operations
/// too fast to cancel on the test portal (a delete of 100 files, the archiving of a room) are checked for the
/// invariants that hold either way, never for "was cancelled". The simple cases (a one-file operation that is
/// removed from the active list) are in the Terminate folder.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class TerminateRunningOperationsTests(
    AspireAppFixture fixture)
    : OperationFlowTestBase(fixture)
{
    [Fact]
    public async Task TerminateTasks_CopyHalfWay_StopsForGoodPartArrivesSourceIsWholeNothingKeepsRunning()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Copy", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Copy Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);
        await PollOperation(op.Id, o => o.Progress >= 15);

        // Act
        var result = await Terminate(op.Id);

        // Assert
        result.Should().NotBeNull();
        var record = await FinishedRecord(op.Id);
        record.Progress.Should().Be(100);
        record.Error.Should().NotBeNullOrEmpty("the cancellation is reported in error");

        var landed = await Settled(() => CountFiles(room.Id));
        landed.Should().BeLessThan(LongFiles, "stopped before the end");
        landed.Should().BeGreaterThan(0, "what was copied stays");
        (await CountFiles(folderId)).Should().Be(LongFiles);
        await ExpectNothingRunning();
        (await GetStatusesByType(FileOperationType.Copy)).Should().BeEmpty();
    }

    /// <remarks>
    /// The status of a cancelled task is supposed to tell it from one that ran to its end, but a cancelled
    /// operation reports Completed (2). The number is pending: the bug is not filed yet.
    /// </remarks>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task TerminateTasks_CancelledOperation_ReportsTheCanceledTaskStatus()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Status", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Status Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);
        await PollOperation(op.Id, o => o.Progress >= 10);
        await Terminate(op.Id);
        var record = await FinishedRecord(op.Id);

        // premise: the record is the cancelled one
        record.Error.Should().NotBeNullOrEmpty();

        // Assert
        record.Status.Should().Be(DistributedTaskStatus.Canceled);
    }

    [Fact]
    public async Task TerminateTasks_MoveHalfWay_LosesNothingEveryFileIsInSourceOrDestinationOnce()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Move", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Move Room");
        var op = Started(await StartMoveOfFolderContent(folderId, room.Id), FileOperationType.Move);
        await PollOperation(op.Id, o => o.Progress >= 15);

        // Act
        await Terminate(op.Id);
        await FinishedRecord(op.Id);

        // Assert
        var inDest = await Settled(() => CountFiles(room.Id));
        var inSource = await Settled(() => CountFiles(folderId));
        (inDest + inSource).Should().Be(LongFiles);
        inDest.Should().BeGreaterThan(0);
        inSource.Should().BeGreaterThan(0, "stopped before the end");
        await ExpectNothingRunning();
    }

    [Fact]
    public async Task TerminateTasks_DuplicationHalfWay_LeavesOriginalWholeAndPartialCopyThatStopsGrowing()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Duplicate", LongFiles);
        var op = Started(await StartDuplicateOfFolder(folderId), FileOperationType.Duplicate);
        await PollOperation(op.Id, o => o.Progress >= 15);

        // Act
        await Terminate(op.Id);
        await FinishedRecord(op.Id);

        // Assert
        (await CountFiles(folderId)).Should().Be(LongFiles);

        var (_, folders) = await RawFolderListing.ReadAsync(
            _filesClient, myDocs.ToString(CultureInfo.InvariantCulture), TestContext.Current.CancellationToken);
        var copies = folders.FindAll(f => f.Id != folderId && f.Title.StartsWith("Autotest Terminate Duplicate", StringComparison.Ordinal));
        copies.Should().ContainSingle();

        (await Settled(() => CountFiles(copies[0].Id))).Should().BeLessThan(LongFiles);
        await ExpectNothingRunning();
    }

    [Fact]
    public async Task TerminateTasks_DownloadHalfWay_EndsForGoodWithoutEverHandingOutAnArchiveAddress()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Download", LongFiles);
        var op = Started(await StartDownloadOfFolder(folderId), FileOperationType.Download);
        await PollOperation(op.Id, o => o.Progress >= 10);

        // Act
        await Terminate(op.Id);

        // Assert: a cancelled download is reported differently from a cancelled copy (its record may never turn
        // finished), so only what must hold either way is asserted: no sample carries an archive address, and
        // the record goes away
        var samples = new List<FileOperationDto>();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        FileOperationDto? record;

        while (true)
        {
            record = (await GetStatuses(op.Id)).Find(o => o.Id == op.Id);

            if (record is not null)
            {
                samples.Add(record);
            }

            if (record is null || record.Finished || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }

        (record is null || record.Finished).Should().BeTrue();
        samples.Should().OnlyContain(s => string.IsNullOrEmpty(s.Url));
        await ExpectNothingRunning();
        (await GetStatusesByType(FileOperationType.Download)).Should().BeEmpty();
    }

    [Fact]
    public async Task TerminateTasks_RightAfterStart_StopsTheCopyBeforeItCompletes()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Early", 40);
        var room = await CreateCustomRoom("Autotest Terminate Early Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);

        // Act
        await Terminate(op.Id);

        // Assert
        var record = await FinishedRecord(op.Id);
        record.Error.Should().NotBeNullOrEmpty();
        (await Settled(() => CountFiles(room.Id))).Should().BeLessThan(40);
        (await CountFiles(folderId)).Should().Be(40);
        await ExpectNothingRunning();
    }

    [Fact]
    public async Task TerminateTasks_DeleteOrArchivingThatFinishesTooFast_LeavesAConsistentStateEitherWay()
    {
        // Arrange: a delete of 100 files - each file is in the Trash or still in place, never lost
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, fileIds) = await Seed(myDocs, "Autotest Terminate Delete", 100);
        var del = Started(
            (await _filesOperationsApi.DeleteBatchItemsAsync(
                new DeleteBatchRequestDto(fileIds: [.. fileIds.Select(id => new DeleteBatchRequestDtoAllOfFileIds(id))], deleteAfter: false, immediately: false),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Delete);

        // Act
        await Terminate(del.Id);
        await ExpectNothingRunning();

        // Assert
        var inPlace = await Settled(() => CountFiles(folderId));
        var (trashFiles, _) = await RawFolderListing.ReadAsync(_filesClient, "@trash", TestContext.Current.CancellationToken);
        var trashed = trashFiles.Count(f => fileIds.Contains(f.Id));
        (inPlace + trashed).Should().Be(100);

        // an archiving: the room is archived whole or not at all, its files intact
        var room = await CreateCustomRoom("Autotest Terminate Archive Room");

        for (var i = 0; i < 20; i += 10)
        {
            await Task.WhenAll(Enumerable.Range(i, 10).Select(n => CreateFile($"a{n}.docx", room.Id)));
        }

        var archive = (await _roomsApi.ArchiveRoomAsync(room.Id, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken)).Response;
        await Terminate(archive.Id);
        await ExpectNothingRunning();

        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.RootFolderType.Should().BeOneOf(FolderType.Archive, FolderType.VirtualRooms);
        (await Settled(() => CountFiles(room.Id))).Should().Be(20);
    }

    [Fact]
    public async Task TerminateTasks_FinishedOperation_UndoesNothingFilesStayWhereTheOperationPutThem()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest Terminate Finished.docx", myDocs);
        var room = await CreateCustomRoom("Autotest Terminate Finished Room");
        var op = Started(await StartCopyOfFile(file.Id, room.Id), FileOperationType.Copy);
        (await WaitForCount(() => CountFiles(room.Id), 1)).Should().Be(1);

        // Act
        var result = await Terminate(op.Id);

        // Assert
        result.Should().NotBeNull();
        (await CountFiles(room.Id)).Should().Be(1);
        (await CountFiles(myDocs)).Should().BeGreaterThanOrEqualTo(1);
        await ExpectNothingRunning();
    }

    [Fact]
    public async Task TerminateTasks_SameOperationTwice_IsHarmlessSecondCallReturnsEmptyAndNothingChanges()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Twice", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Twice Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);
        await PollOperation(op.Id, o => o.Progress >= 10);
        await Terminate(op.Id);
        await FinishedRecord(op.Id);
        var landed = await Settled(() => CountFiles(room.Id));

        // Act
        var again = await Terminate(op.Id);

        // Assert
        again.Should().BeEmpty();
        (await Settled(() => CountFiles(room.Id))).Should().Be(landed);
        (await CountFiles(folderId)).Should().Be(LongFiles);
    }

    /// <remarks>
    /// A whitespace-only id is read as the omitted id and stops every operation of the caller, instead of
    /// cancelling nothing like any other id that is not the caller's operation. Bug number pending.
    /// </remarks>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task TerminateTasks_WhitespaceOnlyId_CancelsNothing()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Blank", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Blank Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);
        var running = await PollOperation(op.Id, o => o.Progress >= 5);
        running.Should().NotBeNull();

        // Act
        var result = await Terminate(" ");
        var done = await FinishedRecord(op.Id);

        // Assert
        result.Should().NotBeNull();
        done.Error.Should().BeNullOrEmpty();
        done.Status.Should().Be(DistributedTaskStatus.Completed);
        (await CountFiles(room.Id)).Should().Be(LongFiles);
    }

    [Fact]
    public async Task TerminateTasks_IdsThatAreNotTheCallersOperations_CancelNothingAndRunningCopyCompletesInFull()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Terminate Junk", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Junk Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);

        // Act & Assert
        foreach (var id in new[] { "00000000-0000-0000-0000-000000000000", "abc", "0", "-1", new string('x', 300), "%00", "../fileops", "'; drop table" })
        {
            var result = await Terminate(id);
            result.Should().NotBeNull($"id {id[..Math.Min(24, id.Length)]}");
        }

        var done = await FinishedRecord(op.Id);
        done.Error.Should().BeNullOrEmpty();
        done.Progress.Should().Be(100);
        done.Status.Should().Be(DistributedTaskStatus.Completed);
        (await CountFiles(room.Id)).Should().Be(LongFiles);
    }

    [Fact]
    public async Task TerminateTasks_OneOperation_LeavesTheOtherOfAnotherKindRunningToItsFullEnd()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderA, _) = await Seed(myDocs, "Autotest Terminate Pair A", LongFiles);
        var (folderB, _) = await Seed(myDocs, "Autotest Terminate Pair B", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate Pair Room");
        var copy = Started(await StartCopyOfFolder(folderA, room.Id), FileOperationType.Copy);
        var dup = Started(await StartDuplicateOfFolder(folderB), FileOperationType.Duplicate);
        await PollOperation(copy.Id, o => o.Progress >= 10);

        // Act
        await Terminate(copy.Id);

        // Assert
        var copyRecord = await FinishedRecord(copy.Id);
        copyRecord.Error.Should().NotBeNullOrEmpty();
        var dupRecord = await FinishedRecord(dup.Id);
        dupRecord.Error.Should().BeNullOrEmpty();
        dupRecord.Status.Should().Be(DistributedTaskStatus.Completed);
        (await Settled(() => CountFiles(room.Id))).Should().BeLessThan(LongFiles);

        var (_, folders) = await RawFolderListing.ReadAsync(
            _filesClient, myDocs.ToString(CultureInfo.InvariantCulture), TestContext.Current.CancellationToken);
        var duplicate = folders.Find(f => f.Id != folderB && f.Title.StartsWith("Autotest Terminate Pair B", StringComparison.Ordinal));
        duplicate.Should().NotBeNull();
        (await CountFiles(duplicate!.Id)).Should().Be(LongFiles);
        (await CountFiles(folderA)).Should().Be(LongFiles);
    }

    [Fact]
    public async Task TerminateTasks_EmptyId_StopsEveryOperationTheCallerIsRunning()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderA, _) = await Seed(myDocs, "Autotest Terminate All A", LongFiles);
        var (folderB, _) = await Seed(myDocs, "Autotest Terminate All B", LongFiles);
        var room = await CreateCustomRoom("Autotest Terminate All Room");
        var copy = Started(await StartCopyOfFolder(folderA, room.Id), FileOperationType.Copy);
        var dup = Started(await StartDuplicateOfFolder(folderB), FileOperationType.Duplicate);
        await PollOperation(copy.Id, o => o.Progress >= 8);
        await PollOperation(dup.Id, o => o.Progress >= 5);

        // Act
        var result = await Terminate(string.Empty);

        // Assert
        result.Should().NotBeNull();
        var copyRecord = await FinishedRecord(copy.Id);
        var dupRecord = await FinishedRecord(dup.Id);
        copyRecord.Error.Should().NotBeNullOrEmpty();
        dupRecord.Error.Should().NotBeNullOrEmpty();
        (await Settled(() => CountFiles(room.Id))).Should().BeLessThan(LongFiles);
        await ExpectNothingRunning();
        (await CountFiles(folderA)).Should().Be(LongFiles);
        (await CountFiles(folderB)).Should().Be(LongFiles);
    }
}
