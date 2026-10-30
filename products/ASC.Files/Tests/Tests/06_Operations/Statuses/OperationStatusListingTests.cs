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
/// <c>GET /api/2.0/files/fileops</c> and <c>GET /api/2.0/files/fileops/{operationType}</c> with several
/// operations alive: kinds listed together in the documented order, filtering by id and by kind, and the
/// rule that operations are private to the account that started them. Not pinned (the contract differs or
/// is silent): <c>byType(Move)</c> cannot be read by name here, see
/// <see cref="GetOperationStatusesByTypeTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class OperationStatusListingTests(
    AspireAppFixture fixture)
    : OperationFlowTestBase(fixture)
{
    [Fact]
    public async Task GetOperationStatuses_DifferentKinds_AreListedTogetherInDocumentedOrderAndFilteringByIdReturnsOnlyThatOne()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (bigFolder, _) = await Seed(myDocs, "Autotest Status Kinds Big", LongFiles);
        var room = await CreateCustomRoom("Autotest Status Kinds Room");
        var moveRoom = await CreateCustomRoom("Autotest Status Kinds Move Room");
        var small = await CreateFolder("Autotest Status Kinds Small", myDocs);
        var toMove = await CreateFile("move.docx", small.Id);
        var toDelete = await CreateFile("delete.docx", small.Id);
        var toDuplicate = await CreateFile("duplicate.docx", small.Id);
        var toDownload = await CreateFile("download.docx", small.Id);

        var copy = Started(await StartCopyOfFolder(bigFolder, room.Id), FileOperationType.Copy);
        var move = Started(
            (await _filesOperationsApi.MoveBatchItemsAsync(
                new BatchRequestDto(
                    fileIds: [new BatchRequestDtoAllOfFileIds(toMove.Id)],
                    destFolderId: new BatchRequestDtoAllOfDestFolderId(moveRoom.Id),
                    conflictResolveType: FileConflictResolveType.Skip,
                    deleteAfter: false),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Move);
        var del = Started(
            (await _filesOperationsApi.DeleteBatchItemsAsync(
                new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(toDelete.Id)], deleteAfter: false, immediately: false),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Delete);
        var dup = Started(
            (await _filesOperationsApi.DuplicateBatchItemsAsync(
                new DuplicateRequestDto(fileIds: [new DuplicateRequestDtoAllOfFileIds(toDuplicate.Id)]),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Duplicate);
        var download = Started(
            (await _filesOperationsApi.BulkDownloadAsync(
                new DownloadRequestDto(fileIds: [], folderIds: [], fileConvertIds: [new DownloadRequestItemDto(new DownloadRequestItemDtoKey(toDownload.Id), string.Empty)]),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Download);

        // Act: the long copy is still running, the short ones have left unread records
        var all = await GetStatuses();

        // Assert
        all.Select(o => o.Id).Should().BeEquivalentTo(new[] { copy.Id, move.Id, del.Id, dup.Id, download.Id });

        // documented order: duplications, moves and copies, deletions, downloads
        static int Rank(FileOperationType type) => type switch
        {
            FileOperationType.Duplicate => 0,
            FileOperationType.Move or FileOperationType.Copy => 1,
            FileOperationType.Delete => 2,
            FileOperationType.Download => 3,
            _ => 99
        };

        var ranks = all.Select(o => Rank(o.Operation)).ToList();
        ranks.Should().BeInAscendingOrder();
        all.Find(o => o.Id == download.Id)!.Finished.Should().BeTrue();
        all.Find(o => o.Id == download.Id)!.Url.Should().NotBeNullOrEmpty();
        all.Find(o => o.Id == copy.Id)!.Finished.Should().BeFalse();

        // filtering by id: only that operation is returned
        (await GetStatuses(copy.Id)).Select(o => o.Id).Should().Equal(copy.Id);
        await FinishedRecord(copy.Id);
    }

    [Fact]
    public async Task GetOperationStatuses_AnotherAccount_SeesNoneOfOwnersOperationsByAnyReadAndCannotStopThem()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest Status Isolation", LongFiles);
        var room = await CreateCustomRoom("Autotest Status Isolation Room");
        var op = Started(await StartCopyOfFolder(folderId, room.Id), FileOperationType.Copy);
        var admin = await InviteMember(EmployeeType.DocSpaceAdmin);

        // positive control: the owner sees the operation (stops at the first finished sample, so the record is not lost)
        (await PollOperation(op.Id, _ => true))!.Id.Should().Be(op.Id);

        // Act
        await _filesClient.Authenticate(admin);
        var general = await GetStatuses();
        var byId = await GetStatuses(op.Id);
        var byType = await GetStatusesByType(FileOperationType.Copy);
        var stopOne = await Terminate(op.Id);
        var stopAll = await Terminate(string.Empty);

        // Assert
        general.Should().BeEmpty();
        byId.Should().BeEmpty();
        byType.Should().BeEmpty();
        stopOne.Should().BeEmpty();
        stopAll.Should().BeEmpty();

        await _filesClient.Authenticate(Owner);
        var done = await FinishedRecord(op.Id);
        done.Error.Should().BeNullOrEmpty();
        done.Progress.Should().Be(100);
        (await CountFiles(room.Id)).Should().Be(LongFiles);
    }

    [Fact]
    public async Task GetOperationStatusesByType_EachKind_ReturnsOnlyItsOwnRecordAndHandsOutOnlyThatOne()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var room = await CreateCustomRoom("Autotest ByType Room");
        var dir = await CreateFolder("Autotest ByType Files", myDocs);
        var toCopy = await CreateFile("copy.docx", dir.Id);
        var toDelete = await CreateFile("delete.docx", dir.Id);
        var toDuplicate = await CreateFile("duplicate.docx", dir.Id);
        var toDownload = await CreateFile("download.docx", dir.Id);

        var copy = Started(await StartCopyOfFile(toCopy.Id, room.Id), FileOperationType.Copy);
        var del = Started(
            (await _filesOperationsApi.DeleteBatchItemsAsync(
                new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(toDelete.Id)], deleteAfter: false, immediately: false),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Delete);
        var dup = Started(
            (await _filesOperationsApi.DuplicateBatchItemsAsync(
                new DuplicateRequestDto(fileIds: [new DuplicateRequestDtoAllOfFileIds(toDuplicate.Id)]),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Duplicate);
        var download = Started(
            (await _filesOperationsApi.BulkDownloadAsync(
                new DownloadRequestDto(fileIds: [], folderIds: [], fileConvertIds: [new DownloadRequestItemDto(new DownloadRequestItemDtoKey(toDownload.Id), string.Empty)]),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Download);

        // Act & Assert
        foreach (var (type, started) in new[] { (FileOperationType.Download, download), (FileOperationType.Duplicate, dup) })
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            List<FileOperationDto> list;

            while (true)
            {
                list = await GetStatusesByType(type);

                if (list.Exists(o => o.Finished) || DateTime.UtcNow >= deadline)
                {
                    break;
                }

                await Task.Delay(200, TestContext.Current.CancellationToken);
            }

            list.Select(o => o.Operation).Should().Equal(type);
            list[0].Finished.Should().BeTrue();
            list[0].Id.Should().Be(started.Id);
        }

        // the two kinds that were read are gone; the others are still there
        (await GetStatuses()).Select(o => o.Id).Should().BeEquivalentTo(new[] { copy.Id, del.Id });
    }

    [Fact]
    public async Task GetOperationStatusesByType_OtherKindOrOtherId_ReturnsEmptyArrayAndKeepsTheRecord()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest ByType Mismatch.docx", myDocs);
        var del = Started(
            (await _filesOperationsApi.DeleteBatchItemsAsync(
                new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(file.Id)], deleteAfter: false, immediately: false),
                TestContext.Current.CancellationToken)).Response,
            FileOperationType.Delete);

        // Act
        var wrongKind = await GetStatusesByType(FileOperationType.Copy, del.Id);
        var wrongId = await GetStatusesByType(FileOperationType.Delete, "00000000-0000-0000-0000-000000000000");

        // Assert
        wrongKind.Should().BeEmpty();
        wrongId.Should().BeEmpty();

        // neither read touched the record: the right pair still returns it
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<FileOperationDto> right;

        while (true)
        {
            right = await GetStatusesByType(FileOperationType.Delete, del.Id);

            if (right.Exists(o => o.Finished) || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        right.Should().ContainSingle().Which.Id.Should().Be(del.Id);
    }

    [Fact]
    public async Task GetOperationStatusesByType_ActiveMove_IsNotMixedIntoCopyKindAndGeneralListReportsItAsMove()
    {
        // Arrange
        var myDocs = await GetUserFolderIdAsync(Owner);
        var (folderId, _) = await Seed(myDocs, "Autotest ByType Move", LongFiles);
        var room = await CreateCustomRoom("Autotest ByType Move Room");
        var move = Started(await StartMoveOfFolderContent(folderId, room.Id), FileOperationType.Move);

        // Act
        var asCopy = await GetStatusesByType(FileOperationType.Copy);
        var general = await PollOperation(move.Id, _ => true);

        // Assert
        asCopy.Should().BeEmpty();
        general!.Operation.Should().Be(FileOperationType.Move);

        var done = general.Finished ? general : await FinishedRecord(move.Id);
        done.Error.Should().BeNullOrEmpty();
        (await CountFiles(room.Id)).Should().Be(LongFiles);
    }

    /// <remarks>
    /// The kind is a route segment. 9 and -1 are well-formed numbers outside the enum; "abc" is not a number at
    /// all and cannot be produced by the typed client, hence the raw request.
    /// </remarks>
    [Theory]
    [InlineData("9")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task GetOperationStatusesByType_InvalidKind_IsBadRequest(string kind)
    {
        // Act
        using var response = await _filesClient.GetAsync($"api/2.0/files/fileops/{kind}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
