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
/// Shared helpers for the <c>GET /api/2.0/files/fileops</c>, <c>GET /api/2.0/files/fileops/{type}</c>
/// and <c>PUT /api/2.0/files/fileops/terminate/{id}</c> suites: triggering a background operation and
/// polling its status. Derives from <see cref="RoomsPermissionsTestBase"/> (not <see cref="BaseTest"/>
/// directly) to reuse its <c>InviteMember</c> role dispatcher instead of duplicating it.
/// </summary>
public abstract class OperationsStatusesTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>Starts a delete of a single file and returns the id of the resulting operation.</summary>
    protected async Task<string> StartDelete(int fileId, bool immediately = true)
    {
        var results = (await _filesOperationsApi.DeleteBatchItemsAsync(
            new DeleteBatchRequestDto(fileIds: [new DeleteBatchRequestDtoAllOfFileIds(fileId)], immediately: immediately),
            TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    /// <summary>Starts a duplicate of a single file and returns the id of the resulting operation.</summary>
    protected async Task<string> StartDuplicate(int fileId)
    {
        var results = (await _filesOperationsApi.DuplicateBatchItemsAsync(
            new DuplicateRequestDto(fileIds: [new DuplicateRequestDtoAllOfFileIds(fileId)]),
            TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    /// <summary>Starts a copy of a single file into <paramref name="destFolderId"/> and returns the operation id.</summary>
    protected async Task<string> StartCopy(int fileId, int destFolderId)
    {
        var results = (await _filesOperationsApi.CopyBatchItemsAsync(
            new BatchRequestDto(
                fileIds: [new BatchRequestDtoAllOfFileIds(fileId)],
                destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
                conflictResolveType: FileConflictResolveType.Skip),
            TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    /// <summary>Starts a move of a single file into <paramref name="destFolderId"/> and returns the operation id.</summary>
    protected async Task<string> StartMove(int fileId, int destFolderId)
    {
        var results = (await _filesOperationsApi.MoveBatchItemsAsync(
            new BatchRequestDto(
                fileIds: [new BatchRequestDtoAllOfFileIds(fileId)],
                destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
                conflictResolveType: FileConflictResolveType.Skip),
            TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    /// <summary>Starts a mark-as-read of a single file and returns the operation id.</summary>
    protected async Task<string> StartMarkAsRead(int fileId)
    {
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            new BaseBatchRequestDto(fileIds: [new BaseBatchRequestDtoAllOfFileIds(fileId)]),
            TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    /// <summary>Starts emptying the trash and returns the operation id.</summary>
    protected async Task<string> StartEmptyTrash()
    {
        var results = (await _filesOperationsApi.EmptyTrashAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return results[0].Id;
    }

    protected async Task<List<FileOperationDto>> GetStatuses(string? id = null)
    {
        return (await _filesOperationsApi.GetOperationStatusesAsync(id: id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<List<FileOperationDto>> GetStatusesByType(FileOperationType operationType, string? id = null)
    {
        return (await _filesOperationsApi.GetOperationStatusesByTypeAsync(operationType, id: id, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// Polls the status list filtered by <paramref name="operationId"/> until the operation record
    /// disappears (it finishes and is dropped from the active list) or the deadline elapses. Returns
    /// the last observed list either way, so the caller's own assertion reports what was actually there.
    /// </summary>
    protected async Task<List<FileOperationDto>> WaitUntilGone(string operationId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        List<FileOperationDto> statuses;

        while (true)
        {
            statuses = await GetStatuses(operationId);

            if (statuses.Count == 0 || DateTime.UtcNow >= deadline)
            {
                return statuses;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls until <paramref name="operationId"/> is reported as still running, or the deadline
    /// elapses. Used to catch an operation mid-flight before terminating it. Returns the last
    /// observed record (possibly <c>null</c> if it was never seen), never throws on timeout.
    /// </summary>
    protected async Task<FileOperationDto?> WaitUntilInProgress(string operationId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        FileOperationDto? found = null;

        while (true)
        {
            var statuses = await GetStatuses(operationId);
            found = statuses.Find(s => s.Id == operationId);

            if (found is { Finished: false } || DateTime.UtcNow >= deadline)
            {
                return found;
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }
}
