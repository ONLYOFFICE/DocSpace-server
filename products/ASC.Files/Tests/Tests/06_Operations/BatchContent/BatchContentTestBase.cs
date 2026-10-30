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

namespace ASC.Files.Tests.Tests._06_Operations.BatchContent;

/// <summary>
/// Shared helpers for the content-level batch-operation suites (copy, move, delete, empty trash): they
/// start an operation, wait for its final record and let the test compare folder listings and file bytes.
/// Access is checked before a job is queued, so refused calls are asserted directly with the SDK exception.
/// </summary>
public abstract class BatchContentTestBase(
    AspireAppFixture fixture)
    : UploadSession.UploadSessionTestBase(fixture)
{
    protected static BatchRequestDto BatchRequest(
        int destFolderId,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType resolve = FileConflictResolveType.Skip,
        bool content = false) =>
        new(
            folderIds: folderIds?.Select(id => new BatchRequestDtoAllOfFolderIds(id)).ToList()!,
            fileIds: fileIds?.Select(id => new BatchRequestDtoAllOfFileIds(id)).ToList()!,
            destFolderId: new BatchRequestDtoAllOfDestFolderId(destFolderId),
            conflictResolveType: resolve,
            deleteAfter: false,
            content: content)
        {
            ReturnSingleOperation = true
        };

    protected static DeleteBatchRequestDto DeleteRequest(
        bool immediately,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null) =>
        new(
            folderIds: folderIds?.Select(id => new DeleteBatchRequestDtoAllOfFolderIds(id)).ToList()!,
            fileIds: fileIds?.Select(id => new DeleteBatchRequestDtoAllOfFileIds(id)).ToList()!,
            deleteAfter: false,
            immediately: immediately)
        {
            ReturnSingleOperation = true
        };

    /// <summary>
    /// Waits for the operation just queued. A record the start answer or an earlier read already handed out is
    /// gone, in which case there is nothing to check here and the caller's own end-state assertions decide.
    /// </summary>
    protected async Task<FileOperationDto?> Complete(List<FileOperationDto> started, FileOperationType type)
    {
        var all = started;

        if (all.Count == 0 || all.Exists(o => !o.Finished))
        {
            all = await WaitLongOperation(all.LastOrDefault()?.Id) ?? [];
        }

        var operation = all.LastOrDefault(o => o.Operation == type);

        if (operation is not null)
        {
            operation.Finished.Should().BeTrue();
            operation.Error.Should().BeNullOrEmpty();
            operation.Progress.Should().Be(100);
        }

        return operation;
    }

    protected async Task<FileOperationDto?> CopyItems(
        int destFolderId,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType resolve = FileConflictResolveType.Skip)
    {
        var started = (await _filesOperationsApi.CopyBatchItemsAsync(
            BatchRequest(destFolderId, fileIds, folderIds, resolve), TestContext.Current.CancellationToken)).Response;

        return await Complete(started, FileOperationType.Copy);
    }

    protected async Task<FileOperationDto?> MoveItems(
        int destFolderId,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null,
        FileConflictResolveType resolve = FileConflictResolveType.Skip)
    {
        var started = (await _filesOperationsApi.MoveBatchItemsAsync(
            BatchRequest(destFolderId, fileIds, folderIds, resolve), TestContext.Current.CancellationToken)).Response;

        return await Complete(started, FileOperationType.Move);
    }

    protected async Task<FileOperationDto?> DeleteItems(
        bool immediately,
        IEnumerable<int>? fileIds = null,
        IEnumerable<int>? folderIds = null)
    {
        var started = (await _filesOperationsApi.DeleteBatchItemsAsync(
            DeleteRequest(immediately, fileIds, folderIds), TestContext.Current.CancellationToken)).Response;

        return await Complete(started, FileOperationType.Delete);
    }

    protected async Task<FileOperationDto?> EmptyTrash()
    {
        // `single` is this endpoint's ReturnSingleOperation: without it the answer lists every delete operation of the
        // caller, an earlier unread one included, and Complete could wait on that one instead of the emptying.
        var started = (await _filesOperationsApi.EmptyTrashAsync(single: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return await Complete(started, FileOperationType.Delete);
    }

    protected async Task ExpectNoOperationQueued()
    {
        (await _filesOperationsApi.GetOperationStatusesAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Response.Should().BeEmpty();
    }

    protected async Task<List<int>> TrashFileIds()
    {
        var (files, _) = await RawFolderListing.ReadAsync(_filesClient, "@trash", TestContext.Current.CancellationToken);

        return [.. files.Select(f => f.Id)];
    }

    protected async Task<List<int>> TrashFolderIds()
    {
        var (_, folders) = await RawFolderListing.ReadAsync(_filesClient, "@trash", TestContext.Current.CancellationToken);

        return [.. folders.Select(f => f.Id)];
    }

    protected static int IdOf(string listingEntry) =>
        int.Parse(listingEntry[..listingEntry.IndexOf(':', StringComparison.Ordinal)], CultureInfo.InvariantCulture);

    /// <summary>A folder with a child, a grandchild and a file.</summary>
    protected async Task<(int Root, int Child, int Grandchild)> NestedFolders(int parentId, string name)
    {
        var root = await CreateFolder(name, parentId);
        var child = await CreateFolder($"{name} Child", root.Id);
        var grandchild = await CreateFolder($"{name} Grandchild", child.Id);
        await PutFile(root.Id, $"{name} File.bin", MakeBytes(64, 1));

        return (root.Id, child.Id, grandchild.Id);
    }

    /// <summary>Invites a User to the room as a content creator and returns the user; the caller stays the owner.</summary>
    protected async Task<User> InviteContentCreator(int roomId)
    {
        await _filesClient.Authenticate(Owner);
        var user = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomId, user, FileShare.ContentCreator);

        return user;
    }
}
