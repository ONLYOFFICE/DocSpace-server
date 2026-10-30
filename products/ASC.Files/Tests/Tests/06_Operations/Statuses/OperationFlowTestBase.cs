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
/// Helpers for the suites that watch long-running operations while they run: seeding a folder with many
/// files, starting copy / duplicate / download of it, and polling one operation's record. A finished record
/// is handed out once, so the polling helpers stop at the first finished sample and never read past it.
/// </summary>
public abstract class OperationFlowTestBase(
    AspireAppFixture fixture)
    : OperationsStatusesTestBase(fixture)
{
    /// <summary>Enough files for a copy or duplication to be observable mid-flight.</summary>
    protected const int LongFiles = 60;

    /// <summary>A folder with <paramref name="count"/> empty documents, created ten at a time.</summary>
    protected async Task<(int FolderId, List<int> FileIds)> Seed(int parentId, string title, int count)
    {
        var folder = await CreateFolder(title, parentId);
        var fileIds = new List<int>();

        for (var i = 0; i < count; i += 10)
        {
            var batch = await Task.WhenAll(
                Enumerable.Range(i, Math.Min(10, count - i)).Select(n => CreateFile($"file {n}.docx", folder.Id)));

            fileIds.AddRange(batch.Select(f => f.Id));
        }

        return (folder.Id, fileIds);
    }

    /// <summary>Files in a folder and all its subfolders; 0 when the folder cannot be read.</summary>
    protected async Task<int> CountFiles(int folderId)
    {
        try
        {
            var (files, folders) = await FolderListing.ReadAsync(_foldersApi, folderId, TestContext.Current.CancellationToken);

            var total = files.Count;

            foreach (var folder in folders)
            {
                total += await CountFiles(folder.Id);
            }

            return total;
        }
        catch (HttpRequestException)
        {
            return 0;
        }
    }

    /// <summary>Waits until a count stops changing and returns it; on the deadline returns the last value read.</summary>
    protected async Task<int> Settled(Func<Task<int>> read, int quietReads = 4)
    {
        var deadline = DateTime.UtcNow.AddSeconds(40);
        var last = -1;
        var same = 0;

        while (true)
        {
            var value = await read();
            same = value == last ? same + 1 : 1;
            last = value;

            if (same >= quietReads || DateTime.UtcNow >= deadline)
            {
                return last;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>Waits until the count reaches <paramref name="expected"/> or the deadline passes; returns the last count read.</summary>
    protected async Task<int> WaitForCount(Func<Task<int>> read, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            var value = await read();

            if (value == expected || DateTime.UtcNow >= deadline)
            {
                return value;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The operation that was just queued: the one unfinished operation of its kind at progress 0.</summary>
    protected static FileOperationDto Started(List<FileOperationDto> response, FileOperationType type)
    {
        var fresh = response.FindAll(o => o.Operation == type && !o.Finished && o.Progress == 0);
        fresh.Should().ContainSingle("exactly one fresh operation of the kind was queued");

        return fresh[0];
    }

    /// <summary>
    /// Polls one operation until <paramref name="until"/> holds or it finishes; returns the last record read
    /// (<c>null</c> when it never showed up before the deadline). It stops at a finished record because reading one
    /// consumes it, and keeps polling while the record is missing: a cancelled task is dequeued at once and its
    /// final state is published again only when the worker notices the cancellation.
    /// </summary>
    protected async Task<FileOperationDto?> PollOperation(string id, Func<FileOperationDto, bool> until, int timeoutSeconds = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        FileOperationDto? last;

        while (true)
        {
            last = (await GetStatuses(id)).Find(o => o.Id == id);

            if (last is not null && (last.Finished || until(last)) || DateTime.UtcNow >= deadline)
            {
                return last;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>The finished record of an operation; this read also hands it out.</summary>
    protected async Task<FileOperationDto> FinishedRecord(string id)
    {
        var record = await PollOperation(id, o => o.Finished);

        record.Should().NotBeNull("the operation is still known");
        record!.Finished.Should().BeTrue();

        return record;
    }

    protected async Task ExpectNothingRunning()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        List<FileOperationDto> running;

        while (true)
        {
            running = (await GetStatuses()).FindAll(o => !o.Finished);

            if (running.Count == 0 || DateTime.UtcNow >= deadline)
            {
                break;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }

        running.Should().BeEmpty();
    }

    protected async Task<List<FileOperationDto>> StartCopyOfFolder(int folderId, int destFolderId, bool deleteAfter = false)
    {
        return (await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto(
                folderIds: [new BatchRequestDtoAllOfFolderIds(folderId)],
                destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
                conflictResolveType: FileConflictResolveType.Skip,
                deleteAfter: deleteAfter),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> StartCopyOfFile(int fileId, int destFolderId, bool deleteAfter = false)
    {
        return (await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto(
                fileIds: [new BatchRequestDtoAllOfFileIds(fileId)],
                destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
                conflictResolveType: FileConflictResolveType.Skip,
                deleteAfter: deleteAfter),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> StartMoveOfFolderContent(int folderId, int destFolderId)
    {
        return (await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto(
                folderIds: [new BatchRequestDtoAllOfFolderIds(folderId)],
                destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
                conflictResolveType: FileConflictResolveType.Skip,
                deleteAfter: false,
                content: true),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> StartDuplicateOfFolder(int folderId)
    {
        return (await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(folderIds: [new DuplicateRequestDtoAllOfFolderIds(folderId)]),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> StartDownloadOfFolder(int folderId)
    {
        return (await _filesOperationsApi.BulkDownloadAsync(
            new DownloadRequestDto(fileIds: [], folderIds: [new DownloadRequestDtoAllOfFolderIds(folderId)], fileConvertIds: []),
            TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> Terminate(string id)
    {
        return (await _filesOperationsApi.TerminateTasksAsync(id, TestContext.Current.CancellationToken)).Response;
    }
}
