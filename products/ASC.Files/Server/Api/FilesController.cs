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

[ConstraintRoute("int")]
public class FilesControllerInternal(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : FilesController<int>(
        filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        editHistoryMapper,
        daoFactory,
        fileSecurity,
        documentServiceHelper,
        documentServiceConnector,
        pathProvider,
        userManager,
        authContext,
        globalStore,
        baseCommonLinkUtility,
        encryptionKeyPairDtoHelper)
{
    /// <remarks>
    /// Returns the activity log of one file from the portal audit trail. The log shows who renamed, moved, shared,
    /// converted, locked, or edited the file, and when. Requires read access to the file. Other callers get 403. The
    /// call is read-only. Entries come newest first. The call merges the events of one action into one entry, and its
    /// `related` list holds the others. The response headers carry the total number of entries, not the body. If no
    /// file has this id, the call returns 404. The call returns 400 if `fromDate` or `toDate` has no `Z` or UTC
    /// offset. To read the log of a folder or a room, use `GET api/2.0/files/folder/{folderId}/log`.
    /// </remarks>
    /// <summary>
    /// Get file history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The activity entries of the file, newest first", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, the `count` is outside its allowed range, or `fromDate` or `toDate` is not a date and time ending in `Z` or a UTC offset")]
    [SwaggerResponse(403, "The caller has no read access to the file")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFileHistory(HistoryRequestDto inDto)
    {
        return historyApiHelper.GetFileHistoryAsync(inDto.FileId, inDto.FromDate, inDto.ToDate, inDto.StartIndex, inDto.Count);
    }
}

public class FilesControllerThirdparty(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : FilesController<string>(
        filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        editHistoryMapper,
        daoFactory,
        fileSecurity,
        documentServiceHelper,
        documentServiceConnector,
        pathProvider,
        userManager,
        authContext,
        globalStore,
        baseCommonLinkUtility,
        encryptionKeyPairDtoHelper);

public abstract class FilesController<T>(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    EditHistoryMapper editHistoryMapper,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    UserManager userManager,
    AuthContext authContext,
    GlobalStore globalStore,
    BaseCommonLinkUtility baseCommonLinkUtility,
    EncryptionKeyPairDtoHelper encryptionKeyPairDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Closes or reopens a revision group in the version history of a file. It returns every stored version of the
    /// file, newest first. A `version` of 0 means the current version. With `continueVersion=false`, the call copies
    /// the current version as a new version. That version starts a new revision group, so later edits no longer
    /// extend the previous one. For an older `version`, or while someone edits the file, it changes nothing. With
    /// `continueVersion=true`, the call merges the revision group after `version` into the group of `version`. If no
    /// group follows it, nothing changes. Requires the right to change the version history, which a room manager and
    /// a content creator have. Other callers get 403, including an editor. The call changes data. Do not repeat it.
    /// With `continueVersion=false`, a repeat adds another version only if `version` is 0. With
    /// `continueVersion=true`, each call merges the next revision group, if any, into the group of `version`. If the
    /// body has no `version`, the call returns 400. If the file or the version does not exist, the call returns 404.
    /// If copying the current version exceeds the room or user storage quota, the call returns 402. A file that another
    /// user locked also returns 403. The call does not work for a file in the trash or on a third-party storage.
    /// </remarks>
    /// <summary>
    /// Change version history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The versions of the file after the change", typeof(IAsyncEnumerable<FileDto<int>>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `version`")]
    [SwaggerResponse(402, "Completing the current version needs more space than the room or user storage quota leaves")]
    [SwaggerResponse(403, "The caller may not change the version history of the file, or the file is locked by somebody else")]
    [SwaggerResponse(404, "The file id, or the requested version of it, resolves to nothing")]
    [HttpPut("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> ChangeVersionHistory(ChangeHistoryRequestDto<T> inDto)
    {
        return filesControllerHelper.ChangeHistoryAsync(inDto.FileId, inDto.File.Version, inDto.File.ContinueVersion);
    }

    /// <remarks>
    /// Returns the conversion status of a file as a list with one entry or none. With `start=true`, the call also
    /// starts the conversion with the format from the portal settings and no password. Requires read access to the
    /// file. Other callers get 403. With `start=true`, the call also requires the right to convert, which a room
    /// manager and a content creator have. Without `start`, the call is read-only. With `start=true`, it queues work.
    /// This is the call to poll after `PUT api/2.0/files/file/{fileId}/checkconversion`. Repeat it until `progress`
    /// is 100 or the list is empty. Read the converted file from `file` and the reason of a failure from `error`. The
    /// call returns a finished entry once and then deletes it. It deletes the entry of a stopped conversion a few
    /// minutes later. So an empty list means the result was already returned, the conversion never started, or no
    /// file has this id. If `fileId` is neither a number nor the id of a third-party file, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Get conversion status
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The conversion entry of the file, or an empty list when the portal has none", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [SwaggerResponse(403, "The caller cannot read the file, or, with `start=true`, may not convert it")]
    [SwaggerResponse(404, "The file id is neither a number nor the id of a file in a known third-party storage")]
    [HttpGet("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> CheckConversionStatus(CheckConversionStatusRequestDto<T> inDto)
    {
        return filesControllerHelper.CheckConversionAsync(new CheckConversionRequestDto<T>
        {
            FileId = inDto.FileId,
            StartConvert = inDto.Start
        });
    }

    /// <remarks>
    /// Returns a download address for the current version of a file as a plain string. The address carries a
    /// time-limited key, so an app that cannot sign in can download the file. Requires read access to the file. Other
    /// callers get 403. The call is read-only, but each call creates a new key. The address stops working when its
    /// key expires. It keeps the version that was current when the call built it. To download the content, follow the
    /// address. If no file has this id, the call returns 404. For a caller who may only fill forms, a form whose
    /// filling has not started returns 400. For an address that the document service signs, use
    /// `GET api/2.0/files/file/{fileId}/presigned`.
    /// </remarks>
    /// <summary>
    /// Get file download link
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The download address of the current file version", typeof(string))]
    [SwaggerResponse(400, "The file is a PDF form in a form-filling room whose filling has not started, and the caller may only fill forms there")]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetPresignedUri(inDto.FileId);
    }

    /// <remarks>
    /// Returns `true` if a file is a PDF form made in the editors, and `false` otherwise. Requires read access to the
    /// file. Other callers get 403. The call is read-only. The call reads the start of the file and looks for the
    /// mark that the editors write into their forms. So an ordinary PDF and a PDF form from other software return
    /// `false`. A file without the PDF extension returns `false` without a read. Form filling can start only for a
    /// file that returns `true`. To read the state of the filling, use `GET api/2.0/files/file/{fileId}/formroles`.
    /// If no file has this id, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Check the PDF file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "True when the file is a PDF form made in the editors, false otherwise", typeof(bool))]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.isFormPDF(inDto.FileId);
    }

    /// <remarks>
    /// Copies a file into a folder under a new title and returns the copy. If the new title has another extension,
    /// the call converts the content. The extension of `destTitle` decides the action. The same extension as the
    /// source copies the content as it is. Another extension makes the document service convert it first. With
    /// `toForm=true`, the call converts a document into a PDF form. Requires read access to the file and the right to
    /// create files in the destination folder. Other callers get 403. The call changes data. Do not repeat it. Each
    /// call adds another copy. If `destFolderId` is neither a number nor a string, the call copies nothing and
    /// returns an empty body. It also returns 403 for an encrypted file, and for a file or a destination in a private
    /// room. If the file or the folder does not exist, the call returns 404. If the copy exceeds the storage quota or
    /// the converted content exceeds the maximum upload size, the call returns 402. If the installation does not
    /// accept the format of the new title, the call returns 415. If the new title is empty and the source has no
    /// extension, the call returns 400. To copy many items without conversion, use `PUT api/2.0/files/fileops/copy`.
    /// </remarks>
    /// <summary>
    /// Copy a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    [RaisesWebhook(WebhookTrigger.FileCreated)]
    [RaisesWebhook(WebhookTrigger.FileUploaded, When = "as well when the copy is converted to another format")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The copy that was created", typeof(FileEntryBaseDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `destTitle` or `destFolderId`, or the new title is empty while the source file has no extension")]
    [SwaggerResponse(402, "The copy does not fit into the storage quota of the portal, the room or the user, or the converted content exceeds the maximum upload size")]
    [SwaggerResponse(403, "The caller may not read the file, the destination folder does not exist, the caller may not create files in it, or the file or the destination lies in a private room")]
    [SwaggerResponse(404, "The file or the destination folder does not exist")]
    [SwaggerResponse(415, "The installation filters uploads and does not accept the format of the new title")]
    [HttpPost("file/{fileId}/copyas")]
    public async Task<FileEntryBaseDto> CopyFileAs(CopyAsRequestDto<T> inDto)
    {
        return inDto.File.DestFolderId.ValueKind switch
        {
            JsonValueKind.Number => await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetInt32(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm),
            JsonValueKind.String => await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetString(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm),
            _ => null
        };
    }

    /// <remarks>
    /// Creates a file in a folder and returns the stored file. The extension in `title` decides the format. The call
    /// changes a known text, spreadsheet, or presentation extension to DOCX, XLSX, or PPTX. It adds DOCX to a title
    /// without an extension. It keeps an unknown extension and the few formats that the portal stores as they are.
    /// With `enableExternalExt=true`, the call keeps the title exactly as sent. If `templateId` is set, the call
    /// copies that file, which the caller must be able to read. Otherwise, `formId` copies a form from the form
    /// gallery. Without both, the call uses the blank template of the format in the caller's language. Requires the
    /// right to create files in the folder. Other callers get 403. The call changes data. Do not repeat it. Each call
    /// creates another file. The call returns 403 in the Rooms root, the archive, and the template sections, even for
    /// a DocSpace admin. It also returns 403 for a missing or unreadable template, or no gallery form of this format.
    /// If `templateId` is a string that matches no third-party file, the call returns 404. If the new file exceeds
    /// the storage quota, the call returns 402. The call returns 400 if `title` is missing, empty, or too long. To
    /// create the file in your own section, use `POST api/2.0/files/@my/file`.
    /// </remarks>
    /// <summary>
    /// Create a file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/file</path>
    [RaisesWebhook(WebhookTrigger.FileCreated)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file created in the folder: its id and the title the portal actually stored, whose extension may differ from the requested one; `thumbnailStatus` says whether the preview is already built", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, or the title is empty or longer than 165 characters")]
    [SwaggerResponse(402, "The new file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller may not create files in the folder, the folder does not exist or is a section where files cannot be created, the template does not exist or cannot be read, the form gallery has no file of the title's format, or the file is not a PDF and the folder lies in a form-filling room")]
    [SwaggerResponse(404, "The folder id or `templateId` is a string that is not the id of an item in a known third-party storage")]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFile(CreateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.TemplateId, inDto.File.FormId, inDto.File.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML file in a folder from `content`, or saves it as a new version of a file. It returns the stored
    /// file. The call adds `.html` to the title if the title does not end with it. The parameter `createNewIfExist`
    /// works the opposite way to its name. With `true`, the call updates the file with this title and adds a version
    /// to its history. With `false`, the default, each call creates another file with a unique title, such as
    /// `Notes (1).html`. The call never updates a file that is locked, encrypted, in the trash, or open for editing.
    /// It also never updates a file that the caller cannot edit. In these cases, it creates a new file instead.
    /// Requires the right to create files in the folder. Other callers get 403. The call changes data. Do not repeat
    /// it. Each call adds a file or a version. If the folder does not exist, the call returns 404. If the content
    /// exceeds the maximum upload size or the storage quota, the call returns 402. If `content` is missing, the call
    /// returns 400. To create the file in your own section, use `POST api/2.0/files/@my/html`.
    /// </remarks>
    /// <summary>
    /// Create an HTML file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/html</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated HTML file", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `content`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(402, "The content exceeds the maximum upload size, or the file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller may not create files in this folder, the folder is a section where files cannot be created, or the folder lies in a form-filling room, which takes PDF files only")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateHtmlFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text file in a folder from `content`, or saves it as a new version of a file. It returns the stored
    /// file. The call adds `.txt` to the title, or `.html` if the content has something like an HTML tag. It adds no
    /// extension if the title already ends with it. The parameter `createNewIfExist` works the opposite way to its
    /// name. With `true`, the call updates the file with this title and adds a version to its history. With `false`,
    /// the default, each call creates another file with a unique title, such as `Notes (1).txt`. The call never
    /// updates a file that is locked, encrypted, in the trash, or open for editing. It also never updates a file that
    /// the caller cannot edit. In these cases, it creates a new file instead. Requires the right to create files in
    /// the folder. Other callers get 403. The call changes data. Do not repeat it. Each call adds a file or a
    /// version. If the folder does not exist, the call returns 404. If the content exceeds the maximum upload size or
    /// the storage quota, the call returns 402. If `content` is missing, the call returns 400. To create the file in
    /// your own section, use `POST api/2.0/files/@my/text`.
    /// </remarks>
    /// <summary>
    /// Create a text file
    /// </summary>
    /// <path>api/2.0/files/{folderId}/text</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated text file", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `content`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(402, "The content exceeds the maximum upload size, or the file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller may not create files in this folder, the folder is a section where files cannot be created, or the folder lies in a form-filling room, which takes PDF files only")]
    [SwaggerResponse(404, "The folder does not exist")]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateTextFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Queues the deletion of a file. It returns the file operations of the caller, including this one. By default,
    /// the call moves the file to the trash, where you can restore it. With `immediately=true`, the call deletes the
    /// file for good. It always deletes for good a file in the trash, an encrypted file, or a third-party file.
    /// Requires the right to delete the file. Other callers get 403, including an editor. A room manager has this
    /// right, and a content creator has it for their own files. The call queues work. Do not repeat it. A repeat
    /// after the move to the trash deletes the file for good. The file is still there when the response arrives.
    /// Repeat `GET api/2.0/files/fileops` until the operation has `finished` set to true. Then read its `error` to
    /// learn if the deletion failed. With `deleteAfter=true`,
    /// `GET api/2.0/files/fileops` stops listing the operation when it ends. So you cannot read its result. With
    /// `returnSingleOperation=true`, the call returns only this deletion. If the file is locked by another user or
    /// open for editing, the call returns 403. If no file has this id, the call returns 404. To delete several items,
    /// use `PUT api/2.0/files/fileops/delete`.
    /// </remarks>
    /// <summary>
    /// Delete a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FileTrashed, When = "once the queued operation moves the file to the trash")]
    [RaisesWebhook(WebhookTrigger.FileDeleted, When = "instead if `immediately` is true or the file is in the trash or third-party storage")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file operations of the caller, including the deletion just queued", typeof(IAsyncEnumerable<FileOperationDto>))]
    [SwaggerResponse(403, "The caller may not delete the file, or the file is locked by somebody else or open in an editing session")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpDelete("file/{fileId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFile(DeleteRequestDto<T> inDto)
    {
        var taskId = await fileOperationsManager.Publish([], [inDto.FileId], false, !inDto.File.DeleteAfter, inDto.File.Immediately);

        foreach (var e in await fileOperationsManager.GetOperationResults(inDto.ReturnSingleOperation ? taskId : null))
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <remarks>
    /// Returns the result of a completed form-filling session. It holds the filled copy, the original form, and the
    /// submission number in the room. It also holds the room and the account that started the filling. Anyone can
    /// call it without signing in. The session id in `fillingSessionId` is the only secret. The call is read-only.
    /// The portal keeps the result only for a limited time after the filling ends. So request it soon after the
    /// document service reports the session id. The call returns 404 for a session that never completed, has expired,
    /// or has a wrong format. If `fillingSessionId` is missing, the call returns 400. To read the filled copy, use
    /// `GET api/2.0/files/file/{fileId}`.
    /// </remarks>
    /// <summary>
    /// Get form-filling result
    /// </summary>
    /// <path>api/2.0/files/file/fillresult</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The result of the completed form-filling session", typeof(FillingFormResultDto<int>))]
    [SwaggerResponse(400, "The `fillingSessionId` is missing or empty")]
    [SwaggerResponse(404, "No completed form-filling session with this identifier is remembered")]
    [AllowAnonymous]
    [HttpGet("file/fillresult")]
    public async Task<FillingFormResultDto<T>> GetFillResult(GetFillResultRequestDto inDto)
    {
        var completedFormId = await hybridCache.GetOrDefaultAsync<string>(inDto.FillingSessionId);

        if (completedFormId != null)
        {
            return await filesControllerHelper.GetFillResultAsync((T)Convert.ChangeType(completedFormId, typeof(T)));
        }
        throw new ItemNotFoundException();
    }

    /// <remarks>
    /// Returns what an editor needs to show the changes of one file version. It holds the address, key, and format of
    /// the version, and the same for the version before it. It also holds the address of the changes and a token that
    /// signs the answer for the document service. A `version` of 0 means the current version. Requires the right to
    /// read the file history, which editing access gives. Other callers get 403, including read-only and commenting
    /// access. An anonymous caller and a file in a third-party storage also get 403. The call is read-only. The
    /// fields `changesUrl` and `previous` are set only if the portal stored the changes of the version. An editing
    /// session stores them, and an uploaded version has none. If there is no earlier version, `previous` points to a
    /// blank template. The addresses carry their own time-limited keys. If the file or the version does not exist,
    /// the call returns 404. To list the versions, use `GET api/2.0/files/file/{fileId}/edit/history`.
    /// </remarks>
    /// <summary>
    /// Get changes URL
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The addresses and keys the editor needs to show the changes", typeof(EditHistoryDataDto))]
    [SwaggerResponse(403, "The caller may not read the history of the file, as with an anonymous caller, read-only or commenting access, or a file in a third-party storage")]
    [SwaggerResponse(404, "The file id, or the requested version of it, resolves to nothing")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrl(EditDiffUrlRequestDto<T> inDto)
    {
        var version = inDto.Version;
        var fileId = inDto.FileId;

        var fileDao = daoFactory.GetFileDao<T>();

        var file = version > 0
            ? await fileDao.GetFileAsync(fileId, version)
            : await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
    }

        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (file.ProviderEntry)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var result = new EditHistoryDataDto
        {
            FileType = file.ConvertedExtension.Trim('.'),
            Key = await documentServiceHelper.GetDocKeyAsync(file),
            Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file)),
            Version = version
        };

        if (await fileDao.ContainChangesAsync(file.Id, file.Version))
        {
            string previousKey;
            string sourceFileUrl;
            string sourceExt;

            var history = await fileDao.GetFileHistoryAsync(file.Id).ToListAsync();
            var previousFileStable = history.OrderByDescending(r => r.Version).FirstOrDefault(r => r.Version < file.Version);
            if (previousFileStable != null)
            {
                sourceFileUrl = pathProvider.GetFileStreamUrl(previousFileStable);
                sourceExt = previousFileStable.ConvertedExtension;

                previousKey = await documentServiceHelper.GetDocKeyAsync(previousFileStable);
            }
            else
            {
                var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
                var storeTemplate = await globalStore.GetStoreTemplateAsync();
                var fileExt = FileUtility.GetFileExtension(file.Title);
                var path = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExt, culture);
                var uri = await storeTemplate.GetUriAsync("", path);

                sourceFileUrl = baseCommonLinkUtility.GetFullAbsolutePath(uri.ToString());
                sourceExt = fileExt.Trim('.');

                previousKey = DocumentServiceConnector.GenerateRevisionId(Guid.NewGuid().ToString());
            }

            result.Previous = new EditHistoryUrlDto { Key = previousKey, Url = documentServiceConnector.ReplaceCommunityAddress(sourceFileUrl), FileType = sourceExt.Trim('.') };

            result.ChangesUrl = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileChangesUrl(file));
        }

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    /// <remarks>
    /// Returns the editing revisions of a file, oldest first. The list has only the versions that a person saved,
    /// without the autosaves of editing sessions. That is how it differs from
    /// `GET api/2.0/files/file/{fileId}/history`. Requires the right to read the file history, which editing access
    /// gives. Other callers get 403, including read-only and commenting access. An anonymous caller and a file in a
    /// third-party storage also get 403. The call is read-only. If no file has this id, the call returns 404. To show
    /// the changes of an entry, use `GET api/2.0/files/file/{fileId}/edit/diff`. To restore an entry, use
    /// `POST api/2.0/files/file/{fileId}/restoreversion`.
    /// </remarks>
    /// <summary>
    /// Get version history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editing revisions of the file, oldest first", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [SwaggerResponse(403, "The caller may not read the history of the file, as with an anonymous caller, read-only or commenting access, or a file in a third-party storage")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistory(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetEditHistoryAsync(inDto.FileId).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Returns a file with its state for the caller. The state includes the actions that the caller may perform and
    /// the sharing rights that give access. A `version` of -1 means the current version. Requires read access to the
    /// file. Other callers get 403. An anonymous caller without an external link gets 401. The call is read-only.
    /// Some files lie in another person's section, in a folder that the caller cannot read. For them, the answer
    /// names the Shared with me section as the folder. If the file or the version does not exist, the call
    /// returns 404. For a caller who may only fill forms, a form whose filling has not started also returns 404. To
    /// read every version, use `GET api/2.0/files/file/{fileId}/history`.
    /// </remarks>
    /// <summary>
    /// Get file information
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file as it is stored, with the state it has for the caller", typeof(FileDto<int>))]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id, or the requested version of it, resolves to nothing, or the file is a PDF form in a form-filling room whose filling has not started and the caller may only fill forms there")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfo(FileInfoRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetFileInfoAsync(inDto.FileId, inDto.Version);
    }


    /// <remarks>
    /// Returns every stored version of a file, newest first, each in the shape of a file. Unlike the list of
    /// `GET api/2.0/files/file/{fileId}/edit/history`, this list includes the autosaves of editing sessions. Requires
    /// the right to read the file history. Other callers get 403, including a DocSpace admin with only read access
    /// and an anonymous caller. In a room, a room manager and a content creator have this right. In a personal
    /// section, editing access is enough. The call is read-only. If no file has this id, the call also returns 403.
    /// If `fileId` is neither a number nor the id of a third-party file, the call returns 404. To restore a version,
    /// use `POST api/2.0/files/file/{fileId}/restoreversion`. To close or reopen a revision group, use
    /// `PUT api/2.0/files/file/{fileId}/history`.
    /// </remarks>
    /// <summary>
    /// Get file versions
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Every stored version of the file, newest first", typeof(IAsyncEnumerable<FileDto<int>>))]
    [SwaggerResponse(403, "The caller may not read the history of the file, or the file id resolves to nothing")]
    [SwaggerResponse(404, "The file id is neither a number nor the id of a file in a known third-party storage")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfo(FileIdRequestDto<T> inDto)
    {
        return filesControllerHelper.GetFileVersionInfoAsync(inDto.FileId);
    }

    /// <remarks>
    /// Locks a file so that other users cannot change it, or removes the lock. It returns the file with its lock
    /// state. With `lockFile=true`, the call removes all other users from the editing session of the file. The lock
    /// then blocks editing, renaming, and deleting for everyone except its owner and room managers. With
    /// `lockFile=false`, the call adds a note about the unlock to the comment of the current version. A file in a
    /// third-party storage gets no note. Requires the right to lock the file, which a room manager and a content
    /// creator have. Other callers get 403. A file in the trash also returns 403. Only a room manager can remove a
    /// lock that another user set. The call changes data. You can repeat it with the same body. A repeated call
    /// changes nothing and returns the file again. If no file has this id, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Lock a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "on every call, even one that changes nothing, and twice for an unlock in portal storage")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file with its lock state as it now stands", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller may not lock or unlock the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFile(LockFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.LockFileAsync(inDto.FileId, inDto.File.LockFile);
    }

    /// <remarks>
    /// Turns the Custom Filter mode of a spreadsheet on or off and returns the file. In this mode, the sorting and
    /// filters of one user are visible only to that user. With the mode off, all users share the filters again.
    /// Turning it on removes other users from the editing session. Requires the right to use the mode, which a room
    /// manager has. Other callers get 403, including read-only access. Only the user who turned the mode on, a room
    /// manager, or a DocSpace admin can turn it off. Other users get 403. The call
    /// changes data, and you can repeat it with the same body. If the format does not support the mode, the call
    /// returns 400. If no file has this id, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Set the Custom Filter editing mode
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/customfilter</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The spreadsheet with its Custom Filter state as it now stands", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The file is not in a format that supports the Custom Filter mode")]
    [SwaggerResponse(403, "The caller may not use the Custom Filter mode on the file, or somebody else switched the mode on and the caller is neither a room manager nor a DocSpace admin")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("file/{fileId}/customfilter")]
    public async Task<FileDto<T>> SetCustomFilterTag(FileCustomFilterRequestDto<T> inDto)
    {
        var result = await fileStorageService.SetCustomFilterTagAsync(inDto.FileId, inDto.Parameters.Enabled);

        return await _fileDtoHelper.GetAsync(result);
    }

    /// <remarks>
    /// Restores an earlier version of a file and returns the editing revisions after the restore. The call stores the
    /// content of the version again as a new version, with a comment that names it. The versions in between stay.
    /// With `url`, the call takes the content from that address instead. The document service uses it to return a
    /// document with some changes rolled back. Requires the right to change the version history. Other callers
    /// get 403, including an anonymous caller. With `url`, the call also requires editing access. The call changes
    /// data. Do not repeat it. Each call adds another version. The call deletes the links to drafts of the file. It
    /// marks the file as new for the other users who can read it. If `version` is missing or is the current version,
    /// the call returns 400. Without `url`, a `version` above the current one copies the current version. If the file
    /// or the version does not exist, the call returns 404. If the content exceeds
    /// the storage quota, the call returns 402. A file that another user locked also returns 403. Without `url`, the
    /// call does not work for an encrypted file or a file that someone is editing. It also does not work for a file in
    /// the trash or on a third-party storage.
    /// </remarks>
    /// <summary>
    /// Restore a file version
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [RaisesWebhook(WebhookTrigger.FileUpdated)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The editing revisions of the file after the restore", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [SwaggerResponse(400, "The version is missing or is already the current one")]
    [SwaggerResponse(402, "The restored content does not fit into the storage quota")]
    [SwaggerResponse(403, "The caller may not change the version history of the file, the file is locked by somebody else, or, with `url`, may not edit the file or the file is being edited")]
    [SwaggerResponse(404, "Without `url`, the file id or the requested version resolves to nothing")]
    [AllowAnonymous]
    [HttpPost("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreFileVersion(RestoreVersionRequestDto<T> inDto)
    {
        return fileStorageService.RestoreVersionAsync(inDto.FileId, inDto.Version, inDto.Url).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Starts the conversion of a file into an editable format and returns the conversion entry to poll. You can omit
    /// the whole body. If `outputType` is empty, the call uses the default format for the type of the file. With
    /// `createNewIfExist=true`, the call creates a new file beside the source. With `false`, the default, it replaces
    /// the converted file that already exists. With `sync=true`, the call converts within the request and returns the
    /// finished result. Use it only for small documents. Requires the right to convert the file, which a room manager
    /// and a content creator have. Other callers get 403. Without `sync`, the call queues work. While a queued
    /// conversion runs, a repeated call without `sync` queues nothing. Otherwise, each call converts the file again.
    /// After this call, repeat `GET api/2.0/files/file/{fileId}/checkconversion` until `progress` is 100. Then read
    /// the converted file from `file`. If the format needs no conversion, the call queues nothing and returns the
    /// current status. If `outputType` names a format that the file cannot convert to, the call returns 400. To save a
    /// PDF copy in another folder, use `POST api/2.0/files/file/{id}/saveaspdf`.
    /// </remarks>
    /// <summary>
    /// Start file conversion
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The conversion entry to poll, or the finished result when the conversion is synchronous", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [SwaggerResponse(400, "`outputType` names a format the portal cannot convert this file to")]
    [SwaggerResponse(403, "The caller may not convert the file")]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartFileConversion(StartConversionRequestDto<T> inDto)
    {
        inDto.CheckConversion ??= new CheckConversionRequestDto<T>();
        inDto.CheckConversion.FileId = inDto.FileId;

        return filesControllerHelper.StartConversionAsync(inDto.CheckConversion);
    }

    /// <remarks>
    /// Changes the comment of one version of a file and returns the stored comment. An empty comment clears it.
    /// Requires the right to change the version history, which a room manager and a content creator have. Other
    /// callers get 403, including an editor of another user's file. The call changes data, and you can repeat it with
    /// the same body. If the version does not exist, the call returns 400. If no file has this id, the call
    /// returns 404. A file that another user locked or a file in the trash also returns 403. To
    /// read the comments of all versions, use `GET api/2.0/files/file/{fileId}/edit/history`.
    /// </remarks>
    /// <summary>
    /// Update a comment
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    [RaisesWebhook(WebhookTrigger.FileUpdated)]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The comment as it was stored", typeof(string))]
    [SwaggerResponse(400, "The request body cannot be read or has no `version`, the version is below 1 or does not exist, or the comment is longer than 255 characters")]
    [SwaggerResponse(403, "The caller may not change the version history of the file, or the file is locked by somebody else")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("file/{fileId}/comment")]
    public async Task<string> UpdateFileComment(UpdateCommentRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateCommentAsync(inDto.FileId, inDto.File.Version, inDto.File.Comment);
    }

    /// <remarks>
    /// Renames a file, restores one of its versions, or both, and returns the file. A non-empty `title` renames the
    /// file but keeps the stored extension. So a rename cannot change the format. An empty `title` keeps the name. A
    /// `lastVersion` above 0 restores that version as `POST api/2.0/files/file/{fileId}/restoreversion` does. With
    /// both fields empty, the call only reads the file. Requires editing access to the file. Other callers get 403,
    /// including a DocSpace admin who is not a room member. Renaming the file of another user also requires the room
    /// manager role. A content creator can rename their own files. An anonymous caller without an external link
    /// gets 401. The call changes data. You can repeat a rename with the same body, but each restore adds a version.
    /// In Dropbox, SharePoint, and WebDAV storages, a rename changes the file id. A repeat then needs the new id from
    /// the response. A rename marks the file as new for the other users who can read it. If `lastVersion` is the
    /// current version, the call returns 400. A `lastVersion` above the current one copies the current version. If
    /// the file or `lastVersion` does not exist, the call returns 404. For
    /// a caller who may only fill forms, a form whose filling has not started also returns 404. If the restore
    /// exceeds the room or user storage quota, the call returns 402. A file that another user locked also returns 403.
    /// </remarks>
    /// <summary>
    /// Update a file
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "for a rename that takes effect, and again for a switch to `lastVersion`")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file after the rename, the restore, or both", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The title is longer than 165 characters, or `lastVersion` is the current version")]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
    [SwaggerResponse(402, "Restoring `lastVersion` needs more space than the room or user storage quota leaves")]
    [SwaggerResponse(403, "The caller may not read or rename the file or change its version, or the file is locked by somebody else")]
    [SwaggerResponse(404, "The file id, or `lastVersion` of it, resolves to nothing, or the file is a PDF form in a form-filling room whose filling has not started and the caller may only fill forms there")]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFile(UpdateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateFileAsync(inDto.FileId, inDto.File.Title, inDto.File.LastVersion);
    }

    /// <remarks>
    /// Replaces the content of a file with an uploaded file and returns the updated file. The call takes the first
    /// file of the form, or the `file` field if the form has none. Requires editing access to the file. Other callers
    /// get 403. The call changes data. Do not repeat it. Each call saves the content again. If no file has this id,
    /// the call returns 404.
    /// </remarks>
    /// <summary>Update file contents</summary>
    /// <path>api/2.0/files/{fileId}/update</path>
    [RaisesWebhook(WebhookTrigger.FileUpdated)]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("{fileId}/update")]
    public async Task<FileDto<T>> UpdateFileStreamFromForm(FileStreamRequestDto<T> inDto)
    {
        IEnumerable<IFormFile> files = Request.Form.Files;
        var file = files.Any() ? files.First() : inDto.File;

        return await filesControllerHelper.UpdateFileStreamAsync(file.OpenReadStream(), inDto.FileId, inDto.FileExtension, inDto.Encrypted, inDto.Forcesave);
    }

    /// <remarks>
    /// Returns the primary external link of a file, and creates it if the file has none. The body sets only a new
    /// link: its rights, lifetime, password, download restriction, and sign-in requirement. In a personal section, a
    /// link without `expirationDate` never expires. A PDF form always gets the rights that filling needs. For a form
    /// in a form-filling room, the call returns the link of the room. Requires the right to share the file. Other
    /// callers get 403. The call changes data only when it creates the link. You can repeat it. Later calls return
    /// the existing link and ignore the body. If the primary link was revoked, the call creates a new one with a new
    /// ID. Until then, `GET api/2.0/files/file/{id}/link` returns 404. The call returns 403 if links to this file
    /// cannot have this access level, or the link limit is reached. It also returns 403 if the portal allows external
    /// links only in public rooms. It returns 400 if the password does not meet the password policy or
    /// `expirationDate` is over 10 years ahead. If no file has this id, the call returns 404. The address is in
    /// `sharedTo.shareLink`. To change the link, use `PUT api/2.0/files/file/{id}/links`.
    /// </remarks>
    /// <summary>
    /// Create the file primary external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The primary external link of the file", typeof(FileShareDto))]
    [SwaggerResponse(400, "The title or password is longer than 255 characters, the password does not meet the portal password policy, or `expirationDate` lies more than 10 years ahead")]
    [SwaggerResponse(403, "The caller may not share the file, the access level is not available for links to this file, the link limit is reached, or the admin restricts external links to public rooms")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPost("file/{id}/link")]
    public async Task<FileShareDto> CreateFilePrimaryExternalLink(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(
            inDto.Id,
            FileEntryType.File,
            inDto.File.Access,
            expirationDate: inDto.File.ExpirationDate,
            requiredAuth: inDto.File.Internal,
            allowUnlimitedDate: true,
            denyDownload: inDto.File.DenyDownload,
            password: inDto.File.Password,
            recreateRevoked: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Returns the primary external link of a file, and creates it on the first call if the file has none. The new
    /// link has read rights, no password, and no expiration date. For a PDF form in a form-filling room, the call
    /// returns the link of the room. Requires the right to share the file, which its author and a room manager have.
    /// Other callers get 403. An anonymous caller gets 401. The call changes data only on the first call, and later
    /// calls return the same link. If the primary link was revoked, the call returns 404 and does not create it
    /// again. The address is in `sharedTo.shareLink`. If `count` is outside its allowed range, the call returns 400.
    /// To list all links of the file, use `GET api/2.0/files/file/{id}/links`.
    /// </remarks>
    /// <summary>
    /// Get the file primary external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The primary external link of the file", typeof(FileShareDto))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
    [SwaggerResponse(403, "The caller may not share the file")]
    [SwaggerResponse(404, "The file does not exist, or its primary link was revoked")]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLink(FilePrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File, allowUnlimitedDate: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Returns the external links of a file. Requires read access to the file. Other callers get 403. A caller who
    /// cannot share the file gets an empty list. The call is read-only. The response headers carry the total number
    /// of links, not the body. A file without links returns an empty list. The list includes the primary link after
    /// `GET api/2.0/files/file/{id}/link` creates it. For a PDF form in a form-filling room, the call adds the link
    /// of the room at the end. If no file has this id, the call returns 404. If `count` is outside its allowed range,
    /// the call returns 400. To change or revoke a link, use `PUT api/2.0/files/file/{id}/links` with its id.
    /// </remarks>
    /// <summary>
    /// Get file external links
    /// </summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The external links of the file", typeof(IAsyncEnumerable<FileShareDto>))]
    [SwaggerResponse(400, "A parameter has the wrong type, or the `count` is outside its allowed range")]
    [SwaggerResponse(403, "The caller cannot read the file")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpGet("file/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetFileLinks(FilePrimaryIdRequestDto<T> inDto)
    {
        var offset = inDto.StartIndex;
        var count = inDto.Count;

        var totalCount = await fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <remarks>
    /// Creates an external link of a file, or changes or revokes one, and returns the link. The value of `linkId`
    /// decides the action. An unused id, including the empty one, creates a link. The id of an existing link changes
    /// that link. The call applies every field each time, so a field that you leave out goes back to its default. An
    /// `access` of `None` revokes the link, and the call returns an empty body. A later read does not create a
    /// revoked primary link again. With `primary=true`, the link becomes the primary link of the file. For a PDF form
    /// in a form-filling room, `FillForms` and `None` change the link of the room. Requires the right to share the
    /// file. Other callers get 403. The call changes data. Do not repeat a call with an empty `linkId`. Each such
    /// call creates another link. With any other `linkId`, you can repeat it with the same body. The call returns
    /// 403 if links to this file cannot have this access level, or the link limit is reached. It also returns 403 if
    /// the portal settings for external links forbid the change. It returns 400 if the password does not meet the
    /// password policy or `expirationDate` is over 10 years ahead. If no file has this id, the call returns 404.
    /// </remarks>
    /// <summary>
    /// Set a file external link
    /// </summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The link as it now stands, or nothing when it was revoked", typeof(FileShareDto))]
    [SwaggerResponse(400, "The title or password is longer than 255 characters, the password does not meet the portal password policy, or `expirationDate` lies more than 10 years ahead")]
    [SwaggerResponse(403, "The caller may not share the file, the access level is not available for links to this file, the link limit is reached, or the admin's restriction on external links forbids the change")]
    [SwaggerResponse(404, "The file id resolves to nothing")]
    [HttpPut("file/{id}/links")]
    public async Task<FileShareDto> SetFileExternalLink(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.SetExternalLinkAsync(
            inDto.Id,
            FileEntryType.File,
            inDto.File.LinkId,
            inDto.File.Title,
            inDto.File.Access,
            inDto.File.ExpirationDate,
            inDto.File.Password,
            inDto.File.DenyDownload,
            inDto.File.Internal,
            inDto.File.Primary);

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <remarks>
    /// Moves a file to a position in its folder and returns the file with its new `order`. Positions start at 1. The
    /// call shifts the file at that position and all files after it, so the folder has no gaps. A position past the
    /// end puts the file last. In a dotted path such as `1.2.3`, the call reads only the last part. The order matters
    /// only in rooms with indexed contents. Elsewhere, the call stores it, but nothing uses it. Requires editing
    /// access to the file. Other callers get 403. The call changes data, and you can repeat it with the same body. If
    /// no file has this id, the call returns 404. If `order` is below 1 or is not a number, the call returns 400. To
    /// move several items, use `PUT api/2.0/files/order`.
    /// </remarks>
    /// <summary>
    /// Set file order
    /// </summary>
    /// <path>api/2.0/files/{fileId}/order</path>
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "when the order changes")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file with the position it now holds", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read, or `order` is below 1 or is neither a number nor a dotted path ending in one")]
    [SwaggerResponse(403, "The caller may not reorder this file")]
    [SwaggerResponse(404, "The file does not exist")]
    [HttpPut("{fileId}/order")]
    public async Task<FileDto<T>> SetFileOrder(OrderFileRequestDto<T> inDto)
    {
        var file = await fileStorageService.SetFileOrder(inDto.FileId, inDto.Order.Order);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Moves several files and folders to new positions and returns the moved entries with their positions. Each item
    /// of `items` names an entry, its type, and its position from 1. In a dotted path such as `1.2.3`, the call reads
    /// only the last part. The call applies the items one by one in the sent order, and each item shifts its
    /// neighbors. So the result depends on the order of `items`. Requires the right to manage the room of every
    /// entry, which a room manager has. Other callers get 403. An entry outside a room also returns 403. The call
    /// changes data, and you can repeat it with the same body. The request is not one transaction. If an item fails,
    /// the items before it stay moved. The order matters only in rooms with indexed contents. If an entry does not
    /// exist or has the wrong `entryType`, the call returns 404. If an `order` is below 1 or is not a number, the
    /// call returns 400. For one file, use `PUT api/2.0/files/{fileId}/order`.
    /// </remarks>
    /// <summary>
    /// Set order of files
    /// </summary>
    /// <path>api/2.0/files/order</path>
    /// <collection>list</collection>
    [RaisesWebhook(WebhookTrigger.FolderUpdated, When = "for every folder in the list whose order changes")]
    [RaisesWebhook(WebhookTrigger.FileUpdated, When = "for every file in the list whose order changes")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The files and folders that were moved, with the positions they now hold", typeof(IAsyncEnumerable<FileEntryDto<int>>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `items`, an item has no `entryId` or `entryType`, or an `order` is below 1 or is neither a number nor a dotted path ending in one")]
    [SwaggerResponse(403, "The caller may not administer the room of an entry, or an entry lies outside any room")]
    [SwaggerResponse(404, "An entry does not exist or is sent with the wrong `entryType`")]
    [HttpPut("order")]
    public IAsyncEnumerable<FileEntryDto<T>> SetFilesOrder(OrdersRequestDto<T> inDto)
    {
        return fileStorageService.SetOrderAsync(inDto.Items).Select<FileEntry<T>, FileEntryDto<T>>(
            async (e, _) => e.FileEntryType == FileEntryType.Folder ?
                await _folderDtoHelper.GetAsync(e as Folder<T>) :
                await _fileDtoHelper.GetAsync(e as File<T>));
    }

    /// <remarks>
    /// Converts a file to PDF and saves the PDF as a new file in the folder from the body. It returns the created
    /// file, and the source stays as it is. The call adds `.pdf` to `title`. An empty `title` uses the name of the
    /// source. The document service converts the file within the request, so the call takes as long as the
    /// conversion. Requires read access to the source file and the right to create files in the destination folder.
    /// Other callers get 403. The call changes data. Do not repeat it. Each call adds another PDF with the same
    /// title. In a third-party storage, each PDF gets a unique title instead. The call marks the result as new in the
    /// room. It saves the result as a PDF form if the portal recognizes the source as a form. If the body has no
    /// `folderId` or `title`, the call returns 400. If the file or the folder does not exist, the call returns 404.
    /// If the PDF exceeds the storage quota, the call returns 402. To convert the file itself, use
    /// `PUT api/2.0/files/file/{fileId}/checkconversion`.
    /// </remarks>
    /// <summary>
    /// Save a file as PDF
    /// </summary>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    [RaisesWebhook(WebhookTrigger.FileCreated)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The PDF file that was created", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `folderId` or `title`")]
    [SwaggerResponse(402, "The PDF does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller cannot read the source file or may not create files in the destination folder")]
    [SwaggerResponse(404, "The source file or the destination folder does not exist")]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveFileAsPdf(SaveAsPdfRequestDto<T> inDto)
    {
        return await filesControllerHelper.SaveAsPdf(inDto.Id, inDto.File.FolderId, inDto.File.Title);
    }

    /// <remarks>
    /// Assigns the roles of a PDF form to users and starts the filling. The call marks the form as being filled and
    /// records the caller as the user who started it. It notifies the users in the roles, except the caller. Members
    /// who may only fill forms can then see the form. Each role has a name, an account, and a sequence number that
    /// sets the turn. Roles with the same number are filled in parallel, and different numbers make a queue. The call
    /// replaces the whole set of roles. In a virtual data room, an empty role list resets a stopped filling and
    /// deletes the assignment. Elsewhere, or before a stop, an empty list returns 403. The call
    /// reads the form id only from `formId` in the body. Send the same value in the route. The form must be a PDF in
    /// a room. Requires the right to start or reset the filling, which a room manager and a content creator have.
    /// Other callers get 403. The call changes data. Do not repeat it. In a virtual data room, a repeated call
    /// returns 403. In a form-filling room, each call notifies the users in the roles again. If `roles` is missing,
    /// the call returns 400. To read the stored roles, use `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Save form role mapping
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/formrolemapping</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The roles were stored and the filling was started or reset")]
    [SwaggerResponse(400, "The request body cannot be read or has no `formId` or `roles`, or `roles` is null")]
    [SwaggerResponse(403, "The caller may not start or reset the filling of this form, or the file is not a PDF or lies outside a room")]
    // The handler reads the id from the body (`formId`), not from the route, so nothing binds this
    // placeholder; the client sends the same value in both places.
    [SwaggerPathParameter("fileId", "The form the role mapping belongs to. Send the same value as the `formId` of the request body, which is the one the handler reads.")]
    [HttpPost("file/{fileId}/formrolemapping")]
    public async Task SaveFormRoleMapping(SaveFormRoleMappingDto<T> inDto)
    {
        await fileStorageService.SaveFormRoleMapping(inDto.FormId, inDto.Roles.Select(r => r.Map()));
    }

    /// <remarks>
    /// Returns the roles of a PDF form with the state of each. The roles of earlier turns are complete, and the roles
    /// of later turns are waiting. The role of the current turn is yours to fill, or in progress if its user opened
    /// the form. After a stop, the role where the filling stopped shows as stopped. A form whose filling never
    /// started returns an empty list. Requires read access to the form. Other callers get 403. The call is read-only.
    /// The file must be a PDF form or a filled copy of one, and another file returns 403. If no file has this id, the
    /// call returns 404. To assign the roles, use `POST api/2.0/files/file/{fileId}/formrolemapping`.
    /// </remarks>
    /// <summary>
    /// Get form roles
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/formroles</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The roles of the form with the state of each", typeof(IEnumerable<FormRoleDto>))]
    [SwaggerResponse(403, "The caller has no read access to the form, or the file is not a PDF")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpGet("file/{fileId}/formroles")]
    public IAsyncEnumerable<FormRoleDto> GetAllFormRoles(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetAllFormRoles(inDto.FileId);
    }

    /// <remarks>
    /// Changes the filling state of a PDF form as `action` sets. In a form-filling room, `Start` (2) opens the form
    /// for filling to members who may only fill forms. If the form version differs from the one that `Edit`
    /// remembered, `Start` also deletes the drafts of the previous round. The action `Stop` (0) stops the filling.
    /// In a virtual data room, it records who stopped it and at which role. It also notifies the users of the other
    /// roles. In a form-filling room, `Stop` closes the form for filling. The action `Resume` (1) continues a stopped
    /// filling and clears that record. The action `Edit` (3) returns the form to editing, closes it for filling, and
    /// remembers the version.
    /// The call reads the form id only from `formId` in the body. Send the same value in the route. The form must be
    /// a PDF in a room. The action `Start` requires the right to start the filling, which a room manager and a
    /// content creator have. Other callers get 403. Only a room manager can stop a filling that another user started.
    /// A content creator gets 403 there. In a form-filling room, `Edit` requires editing access to the form. The call
    /// changes data. You can repeat `Resume` and `Edit` with the same body. A repeated `Stop` returns 403 in a
    /// virtual data room and changes nothing in a form-filling room. Do not repeat `Start` in a form-filling room. A
    /// repeat can delete the drafts that users started after the first call. If the body has no `formId`, the call
    /// returns 400. If `action` is not a known value, the call returns 403. To read the resulting state, use
    /// `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Perform form filling action
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/manageformfilling</path>
    [RaisesWebhook(WebhookTrigger.FormStopped, When = "when `action` is `Stop`, and for `Edit` in a form-filling room where filling had started")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The action was applied to the form")]
    [SwaggerResponse(400, "The request body cannot be read or has no `formId`")]
    [SwaggerResponse(403, "The form does not exist, is not a PDF or lies outside a room, the caller may not start or stop its filling or, in a form-filling room, put it back into editing, or `action` is not one of the known values")]
    // Same shape as formrolemapping above: the id travels in the body, the route placeholder is
    // unbound, and the client fills both with the same value.
    [SwaggerPathParameter("fileId", "The form the action applies to. Send the same value as the `formId` of the request body, which is the one the handler reads.")]
    [HttpPut("file/{fileId}/manageformfilling")]
    public async Task ManageFormFilling(ManageFormFillingDto<T> inDto)
    {
        await fileStorageService.ManageFormFilling(inDto.FormId, inDto.Action);
    }

    /// <remarks>
    /// Reports which fields of the original PDF form of a form-filling room the user renamed in the editor since it opened
    /// `version`, told apart by the field ids the editor keeps. The editor sends it while editing, only when
    /// `GET api/2.0/files/file/{fileId}/openedit` returns `trackFormFieldChanges` true. The caller
    /// needs the right to edit the form; once its filling is started, a content creator keeps it only for a form they
    /// created or started. A report replaces the previous one of the same `sessionId`, even after a forcesave, and one
    /// whose `sequence` is not above the last of that session is dropped, so a repeated or late call is harmless; give
    /// every editor tab its own `sessionId`. A field left out of `renames` kept its key. When the editor saves the
    /// version, a renamed field keeps its earlier answers under its new name in the forms database and the spreadsheet
    /// report. Ignored, still with 200: a `version` the form does not have yet, a compared one older than the last
    /// earlier version with responses, a new session past 20 of one user on one version, a report past the renames the
    /// form can keep. With Document Server Community Edition the call is refused with 403.
    /// </remarks>
    /// <summary>
    /// Report renamed form fields
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/fieldchanges</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The report was taken; a late, repeated or surplus one is dropped without an error")]
    [SwaggerResponse(400, "The request body is missing, `version` is less than 1, `sequence` is negative, `renames` has more than 500 entries or a null one, a key is empty or longer than 255 characters, or `sessionId` is longer than 64 characters or has other characters than letters, digits, hyphens and underscores")]
    [SwaggerResponse(403, "The file is not a PDF form of a form-filling room, it is a copy of a form, the caller may not edit it, or the document editors are Community Edition, which do not track renamed fields")]
    [SwaggerResponse(404, "No file with this identifier exists")]
    [HttpPut("file/{fileId}/fieldchanges")]
    public Task SetFormFieldChanges(SetFormFieldChangesRequestDto inDto)
    {
        return fileStorageService.SetEditorFieldRenamesAsync(inDto.FileId, inDto.Changes.Version, inDto.Changes.Sequence, inDto.Changes.SessionId, inDto.Changes.Renames);
    }

    /// <remarks>
    /// Returns all submissions of a PDF form. The field `metadata` describes the form fields in layout order, and
    /// `submissions` has one record per completed copy. The call returns only the submissions of the version that is
    /// being filled now. The spreadsheet report of `POST api/2.0/files/file/{fileId}/xlsx` covers every version, one
    /// sheet per group of versions with the same fields. Requires read access to the form. Other callers get 403.
    /// The call is read-only. The call also returns 403 if the file does not exist or is not a PDF form. It returns
    /// 403 if the filling never started, the file is a filled copy, or the form left its room. To read the roles and
    /// whose turn it is, use `GET api/2.0/files/file/{fileId}/formroles`.
    /// </remarks>
    /// <summary>
    /// Get form submission results
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/submissions</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The submissions collected for the form, with the description of its fields", typeof(FormSubmissionsDto))]
    [SwaggerResponse(403, "The form does not exist or is not a PDF, the caller has no read access to it, its filling has not started, it is a copy rather than the original form, or it lies outside the room its filling was started in")]
    [HttpGet("file/{fileId}/submissions")]
    public Task<FormSubmissionsDto> GetFormSubmissions(FileIdRequestDto<int> inDto)
    {
        return fileStorageService.GetSubmissionsByFormId(inDto.FileId);
    }

    /// <remarks>
    /// Returns the keys that the caller needs to decrypt a file of an end-to-end encrypted private room. The field
    /// `userKeys` holds the key pairs of the caller, and `fileKeys` the file keys issued to the caller. The call
    /// never returns the keys of other users. Requires read access to the file. Other callers get 403. If the file
    /// does not exist, the call also returns 403. The call is read-only. Without a key pair or an issued key, the
    /// lists are empty. So an empty `fileKeys` means that the caller cannot open the file, not that the file is
    /// unencrypted. If the file cannot carry encryption keys, the call returns 400. If `fileId` is neither a number
    /// nor the id of a third-party file, the call returns 404. To issue keys, use
    /// `PUT api/2.0/files/{fileId}/access`. To manage your own key pairs, use `api/2.0/privacyroom/keys`.
    /// </remarks>
    /// <summary>
    /// Get file encryption information
    /// </summary>
    /// <path>api/2.0/files/{fileId}/access</path>
    /// <param name="fileId">The file whose encryption keys are read. Only a file in an end-to-end encrypted
    /// private room has any.</param>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The key pairs of the caller and the file keys issued to them", typeof(FileEncryptionInfoDto))]
    [SwaggerResponse(400, "The file cannot carry encryption keys")]
    [SwaggerResponse(403, "The file does not exist, or the caller has no read access to it")]
    [SwaggerResponse(404, "The file id is neither a number nor the id of a file in a known third-party storage")]
    [HttpGet("{fileId}/access")]
    public async Task<FileEncryptionInfoDto> GetEncryptionInfoAsync(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new InvalidOperationException( FilesCommonResource.ErrorMessage_SecurityException);
        }

        var userKeys = await encryptionKeyPairDtoHelper.GetKeyPairAsync();
        var fileKeys = await fileDao.GetFileKeys(fileId, authContext.CurrentAccount.ID);

        return new FileEncryptionInfoDto
        {
            UserKeys = userKeys,
            FileKeys = fileKeys?.Select(r => r.Map()).ToList()
        };
    }

    /// <remarks>
    /// Stores the file keys that let the named users open a file of an end-to-end encrypted private room. Each entry
    /// names the account, the public key, and the key encrypted with it. So the portal never gets the plain key.
    /// Before the call, encrypt the key for each recipient with the public key from
    /// `GET api/2.0/files/file/{fileId}/publickeys`. The call replaces the keys of the named accounts and keeps all
    /// other keys. Requires read access to the file and the right to create content in the room. Other callers
    /// get 403. The call changes data. You can repeat it with the same body. If the file does not exist or is not in
    /// a private room, the call also returns 403. If `fileId` is neither a number nor the id of a third-party file,
    /// the call returns 404. The call skips recipients without read access to the file. It returns them in
    /// `skippedUserIds`. To read the keys, use `GET api/2.0/files/{fileId}/access`.
    /// </remarks>
    /// <summary>
    /// Set file encryption information
    /// </summary>
    /// <path>api/2.0/files/{fileId}/access</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file keys of the recipients who can read the file were stored; the others are listed in `skippedUserIds`", typeof(FileKeysWrittenDto))]
    [SwaggerResponse(403, "The file does not exist, the caller may not issue keys for it, or the file is not in a private room")]
    [SwaggerResponse(404, "The file id is neither a number nor the id of a file in a known third-party storage")]
    [HttpPut("{fileId}/access")]
    public async Task<FileKeysWrittenDto> SetEncryptionInfoAsync(AccessRequestDto<T> inDto)
    {
        return new FileKeysWrittenDto { SkippedUserIds = await fileStorageService.SetEncryptionInfoAsync(inDto.FileId, inDto.Keys.Project()) };
    }
}

public class FilesControllerCommon(
        GlobalFolderHelper globalFolderHelper,
        FileStorageService fileStorageService,
        FilesControllerHelper filesControllerHelperInternal,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Creates a file in the "My documents" section of the caller and returns it. The extension in the title sets the
    /// format. The portal changes a known text, spreadsheet, or presentation extension to DOCX, XLSX, or PPTX. A
    /// title without an extension gets DOCX. The portal keeps an unknown extension and a few other formats. If
    /// `enableExternalExt` is true, the portal keeps the title exactly as sent. If `templateId` is set, the portal
    /// copies that file and ignores `formId`. Otherwise, a nonzero `formId` makes the portal copy that form from the
    /// form gallery. If neither is set, the portal uses its blank template for the format and the language of the
    /// caller. If there is no such template, or `enableExternalExt` is true, the file is empty. Any signed-in user
    /// can call it, except a guest. A guest who has My documents gets 403. The call changes data. Do not repeat it.
    /// Each call creates another file. The returned title shows the extension that the portal stored. To create a
    /// file in a room or another folder, use `POST api/2.0/files/{folderId}/file`. The call returns 400 if `title`
    /// is missing, empty, or too long. It returns 402 if the file does not fit into a storage quota. It returns 403
    /// if the template does not exist or the caller cannot read it. It also returns 403 if the form gallery has no
    /// file of the format of the title. If a string `templateId` is not a file of a connected third-party storage,
    /// the call returns 404.
    /// </remarks>
    /// <summary>
    /// Create a file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/file</path>
    [RaisesWebhook(WebhookTrigger.FileCreated)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file created in My documents: its id and the title the portal actually stored, whose extension may differ from the requested one; `thumbnailStatus` says whether the preview is already built", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title`, or the title is empty or longer than 165 characters")]
    [SwaggerResponse(402, "The new file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller is a guest whose My documents still exists, the template does not exist or cannot be read, or the form gallery has no file of the title's format")]
    [SwaggerResponse(404, "`templateId` is a string that is not the id of a file in a known third-party storage")]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileInMyDocuments(CreateFileRequest<JsonElement> inDto)
    {
        return await filesControllerHelperInternal.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create an HTML file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/html</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "whether the content creates a new file or is saved as a new version of an existing file")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommon(CreateTextOrHtmlFileRequest inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates an HTML file in the "My documents" section of the caller, or updates one with the same title. The call
    /// saves `content` as the file content and returns the file. The portal adds `.html` to a title without it. If
    /// `createNewIfExist` is true, the portal adds a version to the file with this title. If there is no such file,
    /// it creates one. If `createNewIfExist` is false, the default, the portal creates another file with a unique
    /// title, such as `Notes (1).html`. The portal does not update a file that is locked, open for editing, or
    /// encrypted. It creates another file with the same title instead. Any signed-in user can call it, except a
    /// guest. A guest who has My documents gets 403. Other guests get 404. The call changes data. A repeat with
    /// `createNewIfExist` true adds a version. A repeat with false adds a numbered copy. To create the file in a room
    /// or another folder, use `POST api/2.0/files/{folderId}/html`. The call returns 400 if `title` or `content` is
    /// missing, or if `title` is blank or too long. It returns 402 if the content is too large for an upload or for a
    /// storage quota.
    /// </remarks>
    /// <summary>
    /// Create an HTML file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/html</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated HTML file", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `content`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(402, "The content exceeds the maximum upload size, or the file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller is a guest whose My documents still exists, where a guest may not create files")]
    [SwaggerResponse(404, "The caller is a guest, who has no My documents")]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyDocuments(CreateTextOrHtmlFileRequest inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text (.txt) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create a text file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/text</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "whether the content creates a new file or is saved as a new version of an existing file")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@common/text")]
    public async Task<FileDto<int>> CreateTextFileInCommon(CreateTextOrHtmlFileRequest inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text file in the "My documents" section of the caller, or updates one with the same title. The call
    /// saves `content` as the file content and returns the file. The portal uses the `.txt` extension. If `content`
    /// has a tag with attributes, it uses `.html` instead. A tag without a space inside, such as a bare `p` tag, does
    /// not count. The portal adds the extension to a title without it. If `createNewIfExist` is true, the portal adds
    /// a version to the file with this title. If there is no such file, it creates one. If `createNewIfExist` is
    /// false, the default, the portal creates another file with a unique title, such as `Notes (1).txt`. The portal
    /// does not update a file that is locked, open for editing, or encrypted. It creates another file with the same
    /// title instead. Any signed-in user can call it, except a guest. A guest who has My documents gets 403. Other
    /// guests get 404. The call changes data. A repeat with `createNewIfExist` true adds a version. A repeat with
    /// false adds a numbered copy. To create the file in a room or another folder, use
    /// `POST api/2.0/files/{folderId}/text`. The call returns 400 if `title` or `content` is missing, or if `title`
    /// is blank or too long. It returns 402 if the content is too large for an upload or for a storage quota.
    /// </remarks>
    /// <summary>
    /// Create a text file in My documents
    /// </summary>
    /// <path>api/2.0/files/@my/text</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created or updated text file", typeof(FileDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `title` or `content`, or the title is empty, blank or longer than 165 characters")]
    [SwaggerResponse(402, "The content exceeds the maximum upload size, or the file does not fit into the storage quota of the portal, the room or the user")]
    [SwaggerResponse(403, "The caller is a guest whose My documents still exists, where a guest may not create files")]
    [SwaggerResponse(404, "The caller is a guest, who has no My documents")]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyDocuments(CreateTextOrHtmlFileRequest inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Starts building preview thumbnails for the files in `fileIds`. The call reads only `fileIds` from the body. It
    /// returns these ids unchanged, and an empty list without them. This answer does not confirm that the portal
    /// queued anything. The portal writes a failure to queue the work to its log and does not report it. The portal
    /// does not build thumbnails for files of a third-party storage. The call does not check access to the files. Any
    /// signed-in user can call it. Anyone with an external share link can call it without signing in. Other callers
    /// get 401. The call queues work. You can repeat it. The response has no thumbnails. To get one, poll
    /// `GET api/2.0/files/file/{fileId}` until `thumbnailStatus` shows that the thumbnail is created. Then read
    /// `thumbnailUrl`.
    /// </remarks>
    /// <summary>
    /// Queue file thumbnails
    /// </summary>
    /// <path>api/2.0/files/thumbnails</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file ids from the request, echoed back", typeof(IEnumerable<JsonElement>))]
    [SwaggerResponse(401, "An anonymous caller has no external link")]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnails(BaseBatchRequestDto inDto)
    {
        // The endpoint only ever queues files; a request naming no files (folders only, or an
        // empty body) is answered with an empty list rather than an ArgumentNullException.
        if (inDto.FileIds == null)
        {
            return [];
        }

        return await fileStorageService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}
