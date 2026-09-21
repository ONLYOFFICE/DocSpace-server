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

namespace ASC.Files.Api;

[ApiEndpoint(Template = "fileops")]
public class OperationController(
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    FileDownloadOperationsManager fileDownloadOperationsManager,
    FileMoveCopyOperationsManager fileMoveCopyOperationsManager,
    FileDeleteOperationsManager fileDeleteOperationsManager,
    FileMarkAsReadOperationsManager fileMarkAsReadOperationsManager,
    FileDuplicateOperationsManager fileDuplicateOperationsManager,
    CommonLinkUtility commonLinkUtility)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Queues a background job that packs the requested files and folders into a single archive, and answers with the
    /// caller's download operations, including the one just started. The archive is not ready when the response
    /// arrives: poll `GET api/2.0/files/fileops` until the operation reports `finished`, then take the address of the
    /// archive from its `url`. Items listed in `fileConvertIds` are converted to the format named there before they
    /// are packed, while the items of `fileIds` are packed as they are. Read access to every listed item is required:
    /// an item the caller may not read fails the whole call with 403, and an id that resolves to nothing is answered
    /// as missing, so filter the selection beforehand. Only one download at a time is allowed per caller, and a
    /// second call made while the first is still running is refused with 403 as well. An empty selection queues
    /// nothing and simply answers with the operations that are already there. An anonymous caller may use the call
    /// for the items covered by the external link they hold.
    /// </remarks>
    /// <summary>Bulk download</summary>
    /// <path>api/2.0/files/fileops/bulkdownload</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The download operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "An item in the selection cannot be read by the caller, or another download of theirs is still running")]
    [AllowAnonymous]
    [HttpPut("bulkdownload")]
    public async IAsyncEnumerable<FileOperationDto> BulkDownload(DownloadRequestDto inDto)
    {
        var files = inDto.FileConvertIds.Select(fileId => new FilesDownloadOperationItem<JsonElement>(fileId.Key, fileId.Value, fileId.Password)).ToList();
        files.AddRange(inDto.FileIds.Select(fileId => new FilesDownloadOperationItem<JsonElement>(fileId, string.Empty, string.Empty)));

        var taskId = await fileDownloadOperationsManager.Publish(inDto.FolderIds, files, commonLinkUtility.ServerRootPath);

        foreach (var e in await fileDownloadOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that copies the requested files and folders into `destFolderId`, leaving the originals
    /// where they are, and answers with the caller's move and copy operations, including the one just started. Poll
    /// `GET api/2.0/files/fileops` until the operation reports `finished`; its `files` and `folders` then name what
    /// was produced. Before starting, `GET api/2.0/files/fileops/move` reports which items already have a same-named
    /// entry at the destination and `conflictResolveType` decides what happens to them, while
    /// `GET api/2.0/files/fileops/checkdestfolder` reports whether the destination accepts the files at all. The
    /// caller needs create access to the destination — room manager or content-creator rights inside a room — and
    /// read access to every source item; anything less is refused with 403. With `content=true` each listed folder is
    /// replaced by its own files and subfolders, so the folder itself is not recreated at the destination. An empty
    /// selection queues nothing and answers with the operations that are already there. To remove the originals
    /// instead use `PUT api/2.0/files/fileops/move`.
    /// </remarks>
    /// <summary>Copy files and folders</summary>
    /// <path>api/2.0/files/fileops/copy</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The move and copy operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder, or cannot read one of the listed items")]
    [HttpPut("copy")]
    public async IAsyncEnumerable<FileOperationDto> CopyBatchItems(BatchRequestDto inDto)
    {
        var taskId = await fileMoveCopyOperationsManager.Publish(inDto.FolderIds, inDto.FileIds, inDto.DestFolderId, true, inDto.ConflictResolveType, !inDto.DeleteAfter, inDto.ToFillOut, inDto.Content);

        foreach (var e in (await fileMoveCopyOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
                 .Where(r => r.OperationType == FileOperationType.Copy))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that deletes the requested files and folders, and answers with the caller's delete
    /// operations, including the one just started. Poll `GET api/2.0/files/fileops` until the operation reports
    /// `finished`, and read its `error`: a failure on a single item is reported there rather than as a status code.
    /// With `immediately=false` the items are moved to the caller's Trash and can be restored from it, while
    /// `immediately=true` removes them at once and for good; deleting a folder takes everything inside it either way.
    /// The call is destructive and it is not a no-op on repetition — a second call with the same ids deletes whatever
    /// has been restored in the meantime. Access is checked before the job is queued: deleting from a room requires
    /// room manager or content-creator rights, editing or read rights are refused with 403, and an id that resolves
    /// to nothing is answered as missing. An empty selection queues nothing and answers with the operations that are
    /// already there. To clear the Trash itself use `PUT api/2.0/files/fileops/emptytrash`.
    /// </remarks>
    /// <summary>Delete files and folders</summary>
    /// <path>api/2.0/files/fileops/delete</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller does not have the rights to delete one of the listed items")]
    [HttpPut("delete")]
    public async IAsyncEnumerable<FileOperationDto> DeleteBatchItems(DeleteBatchRequestDto inDto)
    {
        var taskId = await fileDeleteOperationsManager.Publish(inDto.FolderIds, inDto.FileIds, false, !inDto.DeleteAfter, inDto.Immediately);

        foreach (var e in await fileDeleteOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that removes the listed versions from the history of one file, and answers with the
    /// caller's delete operations, including the one just started. Poll `GET api/2.0/files/fileops` until the
    /// operation reports `finished`; a failure met while the job runs is reported in its `error` rather than as a
    /// status code. Removal is permanent — deleted versions do not travel through Trash and cannot be restored, while
    /// the file itself stays in place with the versions that are left. Send the numbers that
    /// `GET api/2.0/files/file/{fileId}/history` reports, and send at least one: an empty list is not an empty
    /// request, it deletes the whole file instead. The number of the current version is refused before anything is
    /// queued, while numbers that no longer exist are passed over without a complaint. The caller needs the rights
    /// that deleting the file itself would need, so a member with read-only rights is refused, as are a file in an
    /// archived room and a file that is already in Trash, and a file that does not exist is answered as missing. To
    /// delete the file itself use `PUT api/2.0/files/fileops/delete`.
    /// </remarks>
    /// <summary>Delete file versions</summary>
    /// <path>api/2.0/files/fileops/deleteversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpPut("deleteversion")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFileVersions(DeleteVersionBatchRequestDto inDto)
    {
        var taskId = await fileDeleteOperationsManager.Publish([], [inDto.FileId], false, !inDto.DeleteAfter, true, versions: inDto.Versions);

        foreach (var e in await fileDeleteOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that permanently removes the content of the caller's own Trash, and answers with the
    /// caller's delete operations, including the one just started. Poll `GET api/2.0/files/fileops` until the
    /// operation reports `finished`. Every authenticated account may empty its own Trash and only its own: no
    /// per-item access check takes place because nothing outside the caller's Trash is touched. With `folderType` the
    /// sweep is narrowed to the items that were originally stored in sections and rooms of the named types, so
    /// clearing what came from personal documents leaves what came from rooms untouched; without the parameter the
    /// whole Trash is emptied. What is removed here cannot be restored afterwards, which is the difference from
    /// `PUT api/2.0/files/fileops/delete`, where `immediately=false` puts items into Trash in the first place.
    /// Calling it on an already empty Trash queues nothing and answers with the operations that are already there.
    /// </remarks>
    /// <summary>Empty the "Trash" folder</summary>
    /// <path>api/2.0/files/fileops/emptytrash</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpPut("emptytrash")]
    public async IAsyncEnumerable<FileOperationDto> EmptyTrash(EmptyTrashRequestDto inDto)
    {
        var (foldersId, filesId) = await fileStorageService.GetTrashContentAsync(inDto.FolderType);

        var taskId = await fileDeleteOperationsManager.Publish(foldersId, filesId, false, true, false, true);

        foreach (var e in await fileDeleteOperationsManager.GetOperationResults(inDto.Single ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Returns the background file operations of the caller that are still running or whose finished result has not
    /// been read yet, grouped by kind: duplications first, then moves and copies, deletions, downloads and
    /// mark-as-read. This is the polling target for every operation in this section — an operation appears here as
    /// soon as it is queued and carries `progress` from 0 to 100, `finished`, the `error` of a failed item and, for a
    /// download, the address of the archive in `url`. A record is dropped once its finished state has been handed
    /// out, so a completed operation is reported once and an empty array means there is nothing left to report rather
    /// than that the work failed. Pass `id` to follow a single operation; an id that is not among the caller's
    /// operations gives an empty array. Operations are private to the account that started them, an anonymous caller
    /// being scoped to the session of the external link. The call changes nothing. To follow one kind only use
    /// `GET api/2.0/files/fileops/{operationType}`.
    /// </remarks>
    /// <summary>Get active file operations</summary>
    /// <path>api/2.0/files/fileops</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The file operations of the caller that are still running or not yet read", typeof(IAsyncEnumerable<FileOperationDto>))]
    [AllowAnonymous]
    [HttpGet("")]
    public async IAsyncEnumerable<FileOperationDto> GetOperationStatuses(FileOperationResultRequestBaseDto inDto)
    {
        List<IFileOperationsManager> managers = [fileDuplicateOperationsManager, fileMoveCopyOperationsManager, fileDeleteOperationsManager, fileDownloadOperationsManager, fileMarkAsReadOperationsManager];

        foreach (var manager in managers)
        {
            foreach (var e in await manager.GetOperationResults(inDto.Id))
            {
                yield return await fileOperationDtoHelper.GetAsync(e);
            }
        }
    }

    /// <remarks>
    /// Returns the background file operations of the caller that are of one kind, named by the number in the route:
    /// `1` for a copy, `2` for a deletion, `3` for a download, `4` for a mark-as-read and `7` for a duplication. The
    /// answer carries the same records as `GET api/2.0/files/fileops`, with the same rule that a finished operation
    /// is reported once and then dropped, and `id` narrows it further to a single operation. Moves, kind `0`, cannot
    /// be read through this route: the address `api/2.0/files/fileops/move` belongs to another operation, so read
    /// moves from `GET api/2.0/files/fileops` and pick the records whose `operation` is `0`. A kind that has no queue
    /// of its own — `5` for an import, `6` for a conversion — is accepted and answers with an empty array, while a
    /// number outside the operation type is rejected as an invalid request. The call changes nothing and never shows
    /// another account's operations.
    /// </remarks>
    /// <summary>Get file operations by type</summary>
    /// <path>api/2.0/files/fileops/{operationType}</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The operations of the caller that are of the requested kind", typeof(IAsyncEnumerable<FileOperationDto>))]
    [AllowAnonymous]
    [HttpGet("{operationType}")]
    public async IAsyncEnumerable<FileOperationDto> GetOperationStatusesByType(FileOperationResultRequestDto inDto)
    {
        IFileOperationsManager manager = inDto.OperationType switch
        {
            FileOperationType.Move or FileOperationType.Copy => fileMoveCopyOperationsManager,
            FileOperationType.Delete => fileDeleteOperationsManager,
            FileOperationType.Download => fileDownloadOperationsManager,
            FileOperationType.MarkAsRead => fileMarkAsReadOperationsManager,
            FileOperationType.Duplicate => fileDuplicateOperationsManager,
            _ => null
        };

        if (manager == null)
        {
            yield break;
        }

        foreach (var e in (await manager.GetOperationResults(inDto.Id))
                 .Where(r => r.OperationType == inDto.OperationType).ToList())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that clears the new-item badge from the requested files and folders for the calling
    /// account, and answers with the caller's mark-as-read operations, including the one just started. Poll
    /// `GET api/2.0/files/fileops` until the operation reports `finished`. Marking a folder clears the badges of
    /// everything inside it as well. Items the caller cannot read are passed over in silence rather than refused, so
    /// the call succeeds even when the whole selection is inaccessible, and an empty selection queues nothing and
    /// answers with the operations that are already there. Repeating the call on items that are already read changes
    /// nothing, and nothing is opened, moved or modified by it — only the caller's own badges are affected, while
    /// other members keep theirs. To see what is currently marked as new use `GET api/2.0/files/{folderId}/news` for
    /// one folder and `GET api/2.0/files/rooms/news` for the rooms of the caller.
    /// </remarks>
    /// <summary>Mark files and folders as read</summary>
    /// <path>api/2.0/files/fileops/markasread</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The mark-as-read operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpPut("markasread")]
    public async IAsyncEnumerable<FileOperationDto> MarkAsRead(BaseBatchRequestDto inDto)
    {
        var taskId = await fileMarkAsReadOperationsManager.Publish(inDto.FolderIds, inDto.FileIds);

        foreach (var e in await fileMarkAsReadOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that moves the requested files and folders into `destFolderId`, removing them from
    /// where they were, and answers with the caller's move and copy operations, including the one just started. Poll
    /// `GET api/2.0/files/fileops` until the operation reports `finished`. Before starting,
    /// `GET api/2.0/files/fileops/move` reports which items already have a same-named entry at the destination and
    /// `conflictResolveType` decides what happens to them, while `GET api/2.0/files/fileops/checkdestfolder` reports
    /// whether the destination accepts the files at all. The caller needs create access to the destination and the
    /// right to take the items out of their source, which is why room members with editing or review rights are
    /// refused with 403, and why content-creator rights inside a room allow copying an item out of it but not moving
    /// it. A room cannot be moved this way — use `PUT api/2.0/files/rooms/{id}/archive` instead. To keep the
    /// originals use `PUT api/2.0/files/fileops/copy`. An empty selection queues nothing.
    /// </remarks>
    /// <summary>Move files and folders</summary>
    /// <path>api/2.0/files/fileops/move</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The move and copy operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder, or cannot take one of the items out of its source")]
    [HttpPut("move")]
    public async IAsyncEnumerable<FileOperationDto> MoveBatchItems(BatchRequestDto inDto)
    {
        var taskId = await fileMoveCopyOperationsManager.Publish(inDto.FolderIds, inDto.FileIds, inDto.DestFolderId, false, inDto.ConflictResolveType, !inDto.DeleteAfter, inDto.ToFillOut, inDto.Content);

        foreach (var e in (await fileMoveCopyOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
                 .Where(r => r.OperationType == FileOperationType.Move))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Queues a background job that copies each requested file and folder next to itself, into the folder where it
    /// already is, and answers with the caller's duplicate operations, including the one just started. Poll
    /// `GET api/2.0/files/fileops` until the operation reports `finished`. The copies keep the name of the original
    /// with a numeric suffix, so nothing is overwritten and every repetition adds one more copy; duplicating a folder
    /// duplicates its content as well. No destination is taken — to place a copy somewhere else use
    /// `PUT api/2.0/files/fileops/copy`. The caller needs the rights that creating an item in that folder would need,
    /// which inside a room means room manager or content-creator rights: read or editing rights, and an item the
    /// caller has no access to at all, are refused with 403. An empty selection queues nothing and answers with the
    /// operations that are already there.
    /// </remarks>
    /// <summary>Duplicate files and folders</summary>
    /// <path>api/2.0/files/fileops/duplicate</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The duplicate operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the folder that holds one of the listed items")]
    [HttpPut("duplicate")]
    public async IAsyncEnumerable<FileOperationDto> DuplicateBatchItems(DuplicateRequestDto inDto)
    {
        var taskId = await fileDuplicateOperationsManager.Publish(inDto.FolderIds, inDto.FileIds);

        foreach (var e in await fileDuplicateOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Reports whether the destination folder accepts the listed files, before a move or a copy is started. Only
    /// `fileIds` and `destFolderId` are read from the request: `result` says whether all of the files are accepted,
    /// only some of them or none, and `files` names the ones that are. The check is about what the destination allows
    /// to be stored in it rather than about name clashes — everywhere except a form-filling room every file is
    /// accepted, while a form-filling room accepts only PDF forms, so a text document offered to one comes back as
    /// none accepted. The caller needs create access to the destination, so a room the caller cannot write to and an
    /// archived room are refused with 403, a destination that does not exist is answered as missing, and a request
    /// without `destFolderId` is rejected as an invalid request. Folder ids and the copying options of the request
    /// play no part here. The call changes nothing; for same-named entries at the destination use
    /// `GET api/2.0/files/fileops/move`.
    /// </remarks>
    /// <summary>Check the destination folder</summary>
    /// <path>api/2.0/files/fileops/checkdestfolder</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Whether the destination accepts all of the listed files, some of them or none, and which ones it accepts", typeof(CheckDestFolderDto))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder")]
    [HttpGet("checkdestfolder")]
    public async Task<CheckDestFolderDto> CheckMoveOrCopyDestFolder([ModelBinder(BinderType = typeof(BatchModelBinder))] BatchRequestDto inDto)
    {
        List<object> checkedFiles;

        if (inDto.DestFolderId.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException();
        }

        if (inDto.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            checkedFiles = await fileStorageService.MoveOrCopyDestFolderCheckAsync(inDto.FileIds.ToList(), inDto.DestFolderId.GetInt32());
        }
        else
        {
            checkedFiles = await fileStorageService.MoveOrCopyDestFolderCheckAsync(inDto.FileIds.ToList(), inDto.DestFolderId.GetString());
        }

        var entries = await fileStorageService.GetItemsAsync(checkedFiles.Select(Convert.ToInt32), checkedFiles.Select(Convert.ToInt32), FilterType.FilesOnly, false);
        entries.AddRange(await fileStorageService.GetItemsAsync(checkedFiles.OfType<string>(), [], FilterType.FilesOnly, false));

        var filesTask = GetFilesDto(entries).ToListAsync();

        var result = inDto.FileIds.Count - entries.Count != 0 ?
                     entries.Count != 0 ? CheckDestFolderResult.PartAllowed : CheckDestFolderResult.NoneAllowed : CheckDestFolderResult.AllAllowed;

        return new CheckDestFolderDto
        {
            Result = result,
            Files = await filesTask
        };

        async IAsyncEnumerable<FileEntryBaseDto> GetFilesDto(IEnumerable<FileEntry> fileEntries)
        {
            foreach (var entry in fileEntries)
            {
                yield return await GetFileEntryWrapperAsync(entry);
            }
        }
    }

    /// <remarks>
    /// Reports which of the requested files and folders already have a same-named entry in `destFolderId`, so that
    /// the clash can be settled before the move or the copy is started. Nothing is moved, copied or changed by the
    /// call, although the address is shared with `PUT api/2.0/files/fileops/move`: the answer is the part of the
    /// request that clashes, and an empty array means the batch would go through without one. The
    /// `conflictResolveType` of the request is not taken into account — clashing items are reported whatever it says
    /// — and encrypted files are left out of the report. A source id that resolves to nothing is not an error and is
    /// passed over. The caller needs create access to the destination: an archived room and a room the caller cannot
    /// write to are refused with 403, a destination that does not exist is answered as missing, and a request without
    /// `destFolderId` is rejected as an invalid request. To learn whether the destination accepts the files at all
    /// use `GET api/2.0/files/fileops/checkdestfolder`.
    /// </remarks>
    /// <summary>Check move or copy conflicts</summary>
    /// <path>api/2.0/files/fileops/move</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The listed items that already have a same-named entry in the destination folder", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder")]
    [HttpGet("move")]
    public async IAsyncEnumerable<FileEntryBaseDto> CheckMoveOrCopyBatchItems([ModelBinder(BinderType = typeof(BatchModelBinder))] BatchRequestDto inDto)
    {
        List<object> checkedFiles;
        List<object> checkedFolders;

        if (inDto.DestFolderId.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException();
        }

        if (inDto.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            (checkedFiles, checkedFolders) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetInt32());
        }
        else
        {
            (checkedFiles, checkedFolders) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetString());
        }

        var entries = await fileStorageService.GetItemsAsync(checkedFiles.OfType<int>().Select(Convert.ToInt32), checkedFolders.OfType<int>().Select(Convert.ToInt32), FilterType.None, false);
        entries.AddRange(await fileStorageService.GetItemsAsync(checkedFiles.OfType<string>(), checkedFolders.OfType<string>(), FilterType.None, false));

        foreach (var e in entries)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <remarks>
    /// Cancels a background file operation of the caller and answers with the operations that are left. Pass the `id`
    /// that was reported when the operation started to stop that one; a call that leaves the trailing route segment
    /// out stops every operation the caller has running, of every kind. Cancelling stops the job where it stands and
    /// does not undo it: what has already been copied, moved or deleted stays that way, so a cancelled batch can
    /// leave part of itself at the destination and part of it at the source, and the result has to be read back
    /// rather than assumed. The cancelled record is dropped from `GET api/2.0/files/fileops` at once, which is why
    /// the answer here is usually empty. An id that is not among the caller's operations cancels nothing and is not
    /// an error. Operations are private to the account that started them, an anonymous caller being scoped to the
    /// session of the external link, so the call can never reach an operation of anyone else.
    /// </remarks>
    /// <summary>Cancel file operations</summary>
    /// <path>api/2.0/files/fileops/terminate/{id}</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The operations of the caller that are left after the cancellation", typeof(IAsyncEnumerable<FileOperationDto>))]
    [AllowAnonymous]
    [HttpPut("terminate/{id?}")]
    public async IAsyncEnumerable<FileOperationDto> TerminateTasks(OperationIdRequestDto inDto)
    {
        List<IFileOperationsManager> managers = [fileDuplicateOperationsManager, fileMoveCopyOperationsManager, fileDeleteOperationsManager, fileDownloadOperationsManager, fileMarkAsReadOperationsManager];

        foreach (var manager in managers)
        {
            foreach (var e in await manager.CancelOperations(inDto.Id))
            {
                yield return await fileOperationDtoHelper.GetAsync(e);
            }
        }
    }
}
