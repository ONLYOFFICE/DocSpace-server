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
    FileBuilderOperationsManager fileBuilderOperationsManager,
    CommonLinkUtility commonLinkUtility)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Starts a background job that packs files and folders into one archive. The call converts the files of
    /// `fileConvertIds` before it packs them. Requires the right to download the listed items. If the caller can
    /// download none of them, the call returns 403. A room that forbids downloads denies this right to the Read role.
    /// The settings of an external link can deny it too. The call checks the portal's own items and the items on
    /// third-party storage as two separate groups. Each group with listed items needs one item the caller can
    /// download. The archive skips the items the caller cannot download and the ids that do not exist. A caller who
    /// is not signed in needs an external link to the items. Without one, the call returns 401. The call queues work.
    /// Repeat `GET api/2.0/files/fileops` until `finished` is true. Then take the archive address from `url`. Do not
    /// repeat the call while the job runs. Until it ends, each call of the same caller returns 403, even with an empty
    /// selection. At other times, an empty selection queues nothing and returns the existing operations. The call
    /// returns the download operations of the caller, including the new one. If no listed item of a group exists, the
    /// call returns 404. The call returns 400 if the body cannot be read or an item of `fileConvertIds` has no `key` or
    /// `value`.
    /// </remarks>
    /// <summary>Bulk download</summary>
    /// <path>api/2.0/files/fileops/bulkdownload</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [RaisesWebhook(WebhookTrigger.FolderDownloaded, When = "for each listed folder or room when the job packs it, not for the folders inside")]
    [RaisesWebhook(WebhookTrigger.FileDownloaded, When = "for each listed file when the job packs it, not for the files inside listed folders")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The download operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(400, "The request body cannot be read, or an item of `fileConvertIds` has no `key` or `value`")]
    [SwaggerResponse(401, "The caller is not signed in and holds no external link")]
    [SwaggerResponse(403, "The caller can download none of the listed items that exist, counted separately for the portal's own items and for those on third-party accounts, or another download of theirs is still queued or running")]
    [SwaggerResponse(404, "None of the listed items exists, counted separately for the portal's own items and for those on third-party accounts")]
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
    /// Starts a background job that copies files and folders into `destFolderId`. With `content` true, the call
    /// copies the content of each listed folder, not the folder. Requires the right to create content in the
    /// destination and to copy every listed item. Other callers get 403. In a room, room managers and content
    /// creators can create content. A room that forbids downloads denies copying to the Read role. The call queues
    /// work. Repeat `GET api/2.0/files/fileops` until `finished` is true. Its `files` and `folders` then name the
    /// copies. What a repeated call does depends on `conflictResolveType`. With `Skip`, it copies only the files whose
    /// names are still free in the destination. With `Overwrite`, it adds a new version to each earlier copy of a
    /// file. With `Duplicate`, it makes new copies under new names. The call returns the copy operations of the
    /// caller, including the new one. An empty selection queues nothing and returns the existing operations. To find
    /// name clashes first, use `GET api/2.0/files/fileops/move`. To check that the destination accepts the files, use
    /// `GET api/2.0/files/fileops/checkdestfolder`. To move the items instead, use `PUT api/2.0/files/fileops/move`.
    /// The call also returns 403 if the destination is inside a listed folder. A listed folder that would exceed a
    /// room or user quota also returns 403. It also returns 403 if a file to overwrite is locked, open for editing, or
    /// not editable by the caller. A file that is not a PDF form sent to a form-filling room also returns 403. A
    /// listed file too large for a knowledge folder also returns 403. If the destination or a listed file or folder
    /// does not exist, the call returns 404. The call returns 415 for a file format the portal does not accept for
    /// upload. It also returns 415 for a format a knowledge folder cannot index.
    /// </remarks>
    /// <summary>Copy files and folders</summary>
    /// <path>api/2.0/files/fileops/copy</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderCopied, When = "for every copied folder, the folders inside it included")]
    [RaisesWebhook(WebhookTrigger.RoomCopied, When = "for every listed room copied into its own section, an AI agent included")]
    [RaisesWebhook(WebhookTrigger.FileCopied, When = "for every copied file, including the files inside copied folders")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The copy operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder or cannot copy one of the listed items, the destination is a listed folder or lies inside one, a listed folder would exceed the room or user quota, a file that is not a PDF form goes to a form-filling room, a file is too large for a knowledge folder, or a file to overwrite is locked, open for editing or cannot be edited by the caller")]
    [SwaggerResponse(404, "The destination folder, a listed file or a listed folder does not exist")]
    [SwaggerResponse(415, "A listed file has a format the portal does not accept for upload, or one a knowledge folder cannot index")]
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
    /// Starts a background job that deletes files and folders. With `immediately` false, the call moves them to the
    /// Trash of the caller. With `immediately` true, it deletes them for good. It always deletes for good the items in
    /// the Trash or on a third-party storage. The call deletes a folder with all its content. Requires the right to
    /// delete each listed item. Other callers get 403. In a room, a room manager can delete any item, and a content
    /// creator only their own. The call queues work, and a deletion for good cannot be undone. Repeat
    /// `GET api/2.0/files/fileops` until `finished` is true. A failure on one item shows in `error`, not as a status
    /// code. Do not repeat the call. A repeat deletes for good the items that the first call moved to the Trash. It
    /// moves the items restored in the meantime to the Trash again. After a deletion for good, a repeated call returns
    /// 404. The call returns the delete operations of the caller, including the new one. An empty selection queues
    /// nothing and returns the existing operations. To empty the Trash, use `PUT api/2.0/files/fileops/emptytrash`.
    /// The call also returns 403 if an item is locked by another user or open for editing. It also returns 403 for a
    /// room when `immediately` is false. If a listed file or folder does not exist, the call returns 404.
    /// </remarks>
    /// <summary>Delete files and folders</summary>
    /// <path>api/2.0/files/fileops/delete</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderTrashed, When = "for every listed folder moved to the trash, not for the folders inside it")]
    [RaisesWebhook(WebhookTrigger.FolderDeleted, When = "instead for a listed folder deleted immediately")]
    [RaisesWebhook(WebhookTrigger.RoomDeleted, When = "for every listed room")]
    [RaisesWebhook(WebhookTrigger.AgentDeleted, When = "instead for a listed AI agent")]
    [RaisesWebhook(WebhookTrigger.FileTrashed, When = "for every listed file moved to the trash, not for the files inside listed folders")]
    [RaisesWebhook(WebhookTrigger.FileDeleted, When = "instead for a listed file deleted for good, not moved to the trash")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller does not have the rights to delete one of the listed items, one of them is locked by another user or open for editing, or a room is sent to Trash without `immediately`")]
    [SwaggerResponse(404, "A listed file or folder does not exist")]
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
    /// Starts a background job that deletes versions from the history of one file. The file stays with its other
    /// versions. An empty `versions` list deletes the whole file instead. Requires the right to delete the file.
    /// Other callers get 403. A DocSpace admin also gets 403 for a file in the personal documents of another user.
    /// The call queues work, and deleted versions cannot be restored. Repeat `GET api/2.0/files/fileops` until
    /// `finished` is true. A failure during the job shows in `error`, not as a status code. The call skips version
    /// numbers that do not exist, so you can repeat it. The call returns the delete operations of the caller,
    /// including the new one. To delete the file itself, use `PUT api/2.0/files/fileops/delete`. The call also
    /// returns 403 if `versions` includes the current version. It also returns 403 if the file is locked, open for
    /// editing, in an archived room, or in the Trash. If the file does not exist, the call returns 404. The call
    /// returns 400 if the body cannot be read or has no `fileId` or `versions`.
    /// </remarks>
    /// <summary>Delete file versions</summary>
    /// <path>api/2.0/files/fileops/deleteversion</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "once for every removed version")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `fileId` or `versions`")]
    [SwaggerResponse(403, "The caller cannot delete the file, the file is locked by another user, open for editing, in an archived room or in Trash, or `versions` includes the current version")]
    [SwaggerResponse(404, "The file does not exist")]
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
    /// Starts a background job that deletes for good the content of the Trash of the caller. With `folderType`, the
    /// call deletes only the items that came from sections or rooms of those types. Without it, the call empties the
    /// whole Trash. Any signed-in user can call it. The call queues work, and the deletion cannot be undone. Repeat
    /// `GET api/2.0/files/fileops` until `finished` is true. You can repeat the call. A repeat while a job of this
    /// call still runs can show `error` for the items that job deleted. On an empty Trash, the call queues nothing and
    /// returns the existing operations. The call returns the delete operations of the caller, including the new one.
    /// To move items to the Trash, use `PUT api/2.0/files/fileops/delete`. If `folderType` is not a folder type, the
    /// call returns 400.
    /// </remarks>
    /// <summary>Empty the "Trash" folder</summary>
    /// <path>api/2.0/files/fileops/emptytrash</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderDeleted, When = "for every folder lying directly in the trash")]
    [RaisesWebhook(WebhookTrigger.FileDeleted, When = "for each file directly in the trash, not for the files inside trashed folders")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The delete operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(400, "`folderType` holds a value that is not a folder type")]
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
    /// Returns the background file operations of the caller that still run or have an unread result. The list starts
    /// with duplications, then moves and copies, deletions, downloads, mark-as-read jobs, and builder runs. Anyone
    /// can call it without signing in. Each caller sees only their own operations. An anonymous caller sees the
    /// operations of their external link session. This is the call to poll. Repeat it until `finished` is true. A
    /// finished operation appears in one answer only, here or in the answer of the call that started it. So an empty
    /// list means nothing is left to report, not that the work failed. An unknown `id` returns an empty list. To read
    /// one kind of operation, use `GET api/2.0/files/fileops/{operationType}`.
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
        List<IFileOperationsManager> managers = [fileDuplicateOperationsManager, fileMoveCopyOperationsManager, fileDeleteOperationsManager, fileDownloadOperationsManager, fileMarkAsReadOperationsManager, fileBuilderOperationsManager];

        foreach (var manager in managers)
        {
            foreach (var e in await manager.GetOperationResults(inDto.Id))
            {
                yield return await fileOperationDtoHelper.GetAsync(e);
            }
        }
    }

    /// <remarks>
    /// Returns the background file operations of the caller of one kind, named by `operationType`. Anyone can call it
    /// without signing in. Each caller sees only their own operations. As in `GET api/2.0/files/fileops`, a finished
    /// operation appears in one answer and then leaves the list. A kind without a queue of its own, such as an import
    /// or a conversion, returns an empty list. To read moves, use `GET api/2.0/files/fileops` and pick the move
    /// records. The path `api/2.0/files/fileops/move` belongs to another operation. If `operationType` is not a known
    /// kind, the call returns 400.
    /// </remarks>
    /// <summary>Get file operations by type</summary>
    /// <path>api/2.0/files/fileops/{operationType}</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The operations of the caller that are of the requested kind", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(400, "`operationType` is neither the number nor the name of an operation type")]
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
            FileOperationType.Build => fileBuilderOperationsManager,
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
    /// Starts a background job that clears the new-item badges of files and folders for the caller. Marking a folder
    /// also clears the badges of its content. Other members keep their badges. Any signed-in user can call it. The
    /// call skips the items the caller cannot read and the ids that do not exist, without an error. The call queues
    /// work. Repeat `GET api/2.0/files/fileops` until `finished` is true. You can repeat it with the same body. The
    /// call returns the mark-as-read operations of the caller, including the new one. An empty selection queues
    /// nothing and returns the existing operations. To see new items, use `GET api/2.0/files/{folderId}/news` for a
    /// folder or `GET api/2.0/files/rooms/news` for rooms.
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
    /// Starts a background job that moves files and folders into `destFolderId`. With `content` true, the call moves
    /// the content of each listed folder, not the folder. Requires the right to create content in the destination and
    /// to remove the items from their source. Other callers get 403. In a room, room managers and content creators
    /// have the right to create content. In a room, a content creator can move only the items they created. The call
    /// queues work. Repeat `GET api/2.0/files/fileops` until `finished` is true. What a repeated call does depends on
    /// `conflictResolveType`. With `Skip`, it changes nothing. With `Overwrite`, it adds to each listed file a new
    /// version of itself. With `Duplicate`, it gives each listed folder a new name and a new id. The call returns the
    /// move operations of the caller, including the new one. An empty selection queues nothing and returns the
    /// existing operations. A room can move only between the Rooms section and the archive. To archive a room, use
    /// `PUT api/2.0/files/rooms/{id}/archive`. To find name clashes first, use `GET api/2.0/files/fileops/move`. To
    /// check that the destination accepts the files, use `GET api/2.0/files/fileops/checkdestfolder`. To keep the
    /// originals, use `PUT api/2.0/files/fileops/copy`. The call also returns 403 if the destination is inside a
    /// listed folder. A listed folder that would exceed a room or user quota also returns 403. It also returns 403 if
    /// a listed file is locked or open for editing. A file open for editing inside a listed folder also returns 403.
    /// A file to overwrite that is locked, open for editing, or not editable by the caller also returns 403. A file
    /// that is not a PDF form sent to a form-filling room also returns 403. A folder or several files moved into a
    /// form-filling room from outside also return 403. A listed file too large for a knowledge folder also returns
    /// 403. If the destination or a listed file or folder does not exist, the call returns 404. The call returns 415
    /// for a file format the portal does not accept for upload. It also returns 415 for a format a knowledge folder
    /// cannot index.
    /// </remarks>
    /// <summary>Move files and folders</summary>
    /// <path>api/2.0/files/fileops/move</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderMoved, When = "for each listed folder, and for its subfolders only when it merges into an existing folder")]
    [RaisesWebhook(WebhookTrigger.FolderRestored, When = "instead for a folder moved out of the trash")]
    [RaisesWebhook(WebhookTrigger.RoomArchived, When = "for every listed room moved to the archive")]
    [RaisesWebhook(WebhookTrigger.RoomRestored, When = "for every listed room moved out of the archive")]
    [RaisesWebhook(WebhookTrigger.FileMoved, When = "for each listed file, and for files of folders that merge into existing folders")]
    [RaisesWebhook(WebhookTrigger.FileRestored, When = "instead for a file moved out of the trash")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The move operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder or cannot take one of the items out of its source, the destination is a listed folder or lies inside one, a folder or several files are moved into a form-filling room from outside it, a listed folder would exceed the room or user quota, a file that is not a PDF form goes to a form-filling room, a file is too large for a knowledge folder, a listed file is locked or open for editing, a file inside a listed folder is open for editing, or a file to overwrite is locked, open for editing or cannot be edited by the caller")]
    [SwaggerResponse(404, "The destination folder, a listed file or a listed folder does not exist")]
    [SwaggerResponse(415, "A listed file has a format the portal does not accept for upload, or one a knowledge folder cannot index")]
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
    /// Starts a background job that copies each listed file and folder into the folder that holds it. A copy gets the
    /// name of the original with a number added. The call copies a folder with its content. Requires the right to
    /// create content in the folder that holds each item. Other callers get 403. In a room, a member with the Editing
    /// or Read role also gets 403. The call queues work. Repeat `GET api/2.0/files/fileops` until `finished` is true.
    /// Do not repeat the call. Each call adds one more copy of every item. The call returns the duplicate operations
    /// of the caller, including the new one. An empty selection queues nothing and returns the existing operations.
    /// To copy items to another folder, use `PUT api/2.0/files/fileops/copy`. The call also returns 403 for an item
    /// in an archived room or in the Trash. If a listed file or folder does not exist, the call returns 404. The call
    /// returns 415 for a file format the portal does not accept for upload.
    /// </remarks>
    /// <summary>Duplicate files and folders</summary>
    /// <path>api/2.0/files/fileops/duplicate</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderCopied, When = "for every duplicated folder, the folders inside it included")]
    [RaisesWebhook(WebhookTrigger.RoomCopied, When = "for every listed room, an AI agent included")]
    [RaisesWebhook(WebhookTrigger.FileCopied, When = "for every duplicated file, including the files inside duplicated folders and rooms")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The duplicate operations of the caller, the one just queued included", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller cannot create items in the folder that holds one of the listed items, or cannot copy that item")]
    [SwaggerResponse(404, "A listed file or folder does not exist")]
    [SwaggerResponse(415, "A listed file has a format the portal does not accept for upload")]
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
    /// Returns whether the destination folder accepts the listed files before a move or a copy. The call reads only
    /// `fileIds` and `destFolderId` from the request. A form-filling room accepts only PDF forms. Every other folder
    /// accepts every file. A file listed twice counts once. Requires the right to create content in the destination.
    /// Other callers get 403. The call also returns 403 for an archived room and for the Rooms, Forms, and AI agents
    /// section roots. The call is read-only. It does not look for name clashes. To find them, use
    /// `GET api/2.0/files/fileops/move`. If the destination does not exist, the call returns 404. Without
    /// `destFolderId`, the call returns 400.
    /// </remarks>
    /// <summary>Check the destination folder</summary>
    /// <path>api/2.0/files/fileops/checkdestfolder</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Whether the destination accepts all of the listed files, some of them or none, and which ones it accepts", typeof(CheckDestFolderDto))]
    [SwaggerResponse(400, "`destFolderId` is not given")]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder, or the destination is the Rooms, Forms or AI agents section root, which holds rooms only")]
    [SwaggerResponse(404, "The destination folder does not exist")]
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

        // a file listed twice is queued once by the operation, so the verdict counts the distinct ids
        var (requestedIntIds, requestedStringIds) = FileOperationsManager.GetIds(inDto.FileIds);
        var requestedCount = requestedIntIds.Distinct().Count() + requestedStringIds.Distinct().Count();

        var result = requestedCount - entries.Count != 0 ?
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
    /// Returns the listed files and folders that have a same-named item in `destFolderId`. Use it to settle name
    /// clashes before a move or a copy. With `content` true, the call checks the content of each listed folder
    /// instead of the folder. Requires the right to create content in the destination. Other callers get 403. The
    /// call is read-only, though `PUT api/2.0/files/fileops/move` has the same path. An empty list means no clashes.
    /// The call ignores `conflictResolveType`, encrypted files, and ids that do not exist. It checks only the listed
    /// folders the caller can read. To check that the destination accepts the files, use
    /// `GET api/2.0/files/fileops/checkdestfolder`. The call also returns 403 if the destination is an archived room,
    /// a listed folder, or inside one. A file or a plain folder sent to the Rooms, Forms, or AI agents root also
    /// returns 403. A plain folder sent to a knowledge folder or the archive also returns 403. A room sent anywhere
    /// except the Rooms root or the archive also returns 403. If the destination does not exist, the call
    /// returns 404. Without `destFolderId`, the call returns 400.
    /// </remarks>
    /// <summary>Check move or copy conflicts</summary>
    /// <path>api/2.0/files/fileops/move</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The listed items that already have a same-named entry in the destination folder", typeof(IAsyncEnumerable<FileEntryBaseDto>))]
    [SwaggerResponse(400, "`destFolderId` is not given")]
    [SwaggerResponse(403, "The caller cannot create items in the destination folder, the destination is a listed folder or lies inside one, a file or a folder that is not a room is offered to the Rooms, Forms or AI agents section root, or a room is offered anywhere but the Rooms root or the archive")]
    [SwaggerResponse(404, "The destination folder does not exist")]
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
            (checkedFiles, checkedFolders) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetInt32(), inDto.Content);
        }
        else
        {
            (checkedFiles, checkedFolders) = await fileStorageService.MoveOrCopyFilesCheckAsync(inDto.FileIds.ToList(), inDto.FolderIds.ToList(), inDto.DestFolderId.GetString(), inDto.Content);
        }

        var entries = await fileStorageService.GetItemsAsync(checkedFiles.OfType<int>().Select(Convert.ToInt32), checkedFolders.OfType<int>().Select(Convert.ToInt32), FilterType.None, false);
        entries.AddRange(await fileStorageService.GetItemsAsync(checkedFiles.OfType<string>(), checkedFolders.OfType<string>(), FilterType.None, false));

        foreach (var e in entries)
        {
            yield return await GetFileEntryWrapperAsync(e);
        }
    }

    /// <remarks>
    /// Cancels a background file operation of the caller and returns the operations that are left. Without `id`, the
    /// call cancels every running operation of the caller. Anyone can call it without signing in. A caller can cancel
    /// only their own operations. An anonymous caller reaches only the operations of their external link session. The
    /// call stops the job but does not undo it. Items already copied, moved, or deleted stay that way. You can repeat
    /// the call. An unknown `id` cancels nothing and is not an error. The answer is usually empty. After the job
    /// stops, `GET api/2.0/files/fileops` lists it once, finished, with the status `Canceled`.
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
        List<IFileOperationsManager> managers = [fileDuplicateOperationsManager, fileMoveCopyOperationsManager, fileDeleteOperationsManager, fileDownloadOperationsManager, fileMarkAsReadOperationsManager, fileBuilderOperationsManager];

        foreach (var manager in managers)
        {
            foreach (var e in await manager.CancelOperations(inDto.Id))
            {
                yield return await fileOperationDtoHelper.GetAsync(e);
            }
        }
    }
}
