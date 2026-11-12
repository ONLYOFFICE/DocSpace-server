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
public class UploadControllerInternal(
    UploadControllerHelper filesControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileUploader fileUploader,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    ChunkedUploadSessionHolder chunkedUploadSessionHolder,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager,
    SocketManager socketManager,
    AuthContext authContext,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    IEventBus eventBus,
    FileSecurity fileSecurity)
    : UploadController<int>(filesControllerHelper, folderDtoHelper, fileDtoHelper, fileUploader, chunkedUploadSessionHelper, chunkedUploadSessionHolder, filesMessageService, webhookManager, socketManager, authContext, tenantManager, daoFactory, eventBus, fileSecurity);

public class UploadControllerThirdparty(
    UploadControllerHelper filesControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileUploader fileUploader,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    ChunkedUploadSessionHolder chunkedUploadSessionHolder,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager,
    SocketManager socketManager,
    AuthContext authContext,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    IEventBus eventBus,
    FileSecurity fileSecurity)
    : UploadController<string>(filesControllerHelper, folderDtoHelper, fileDtoHelper, fileUploader, chunkedUploadSessionHelper, chunkedUploadSessionHolder, filesMessageService, webhookManager, socketManager, authContext, tenantManager, daoFactory, eventBus, fileSecurity);

public abstract class UploadController<T>(
    UploadControllerHelper filesControllerHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileUploader fileUploader,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    ChunkedUploadSessionHolder chunkedUploadSessionHolder,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager,
    SocketManager socketManager,
    AuthContext authContext,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    IEventBus eventBus,
    FileSecurity fileSecurity)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Opens a chunked upload session for a file in the folder and returns it in a success envelope. This call is
    /// deprecated. Use `POST api/2.0/files/{folderId}/session`, which returns the same session without the envelope.
    /// No content is stored yet. Send the parts to `POST api/2.0/files/{folderId}/session/{sessionId}/upload`. Each
    /// part is at most `chunkUploadSize` from `GET api/2.0/files/settings`. Then store the file with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. Requires the right to create content in the
    /// folder. Other callers get 403. In a room, its owner, room managers, and content creators have it. A guest has
    /// it there only as a content creator. In My documents, the owner and anyone with read and write access to the
    /// folder have it. A section root, an archived room, and an AI agent or AI project root also return 403. The call
    /// changes data. A repeated call opens another session. It creates the folders of `relativePath` that do not exist
    /// yet. If the folder has a file with this name, `createNewIfExist` true stores a copy with a numeric suffix. With
    /// false, the upload adds a new version to that file if the caller can edit it. A guest never adds a version this
    /// way. A session expires 12 hours after it opens or gets its last part. A missing folder returns 404. The call
    /// returns 400 for a negative `fileSize`. In a private room, it also returns 400 if the name is not a lowercase
    /// UUID with an optional extension. The call returns 402 if `fileSize` is over the limit for chunked uploads or
    /// for a knowledge folder. It returns 403 if a private room gets a file without `encrypted`, or a knowledge folder
    /// gets an unsupported format. It also returns 403 for a `relativePath` into a knowledge folder or an
    /// instructions room. If the installation does not allow uploads with this extension, the call returns 415.
    /// </remarks>
    /// <summary>Chunked upload</summary>
    /// <path>api/2.0/files/{folderId}/upload/create_session</path>
    [Obsolete]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The created session, wrapped in the success envelope", typeof(ChunkedUploadSessionResultDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read, has no `fileName`, `fileSize` is negative, or the name of a file in a private room is not a lowercase UUID with an optional lowercase extension")]
    [SwaggerResponse(402, "The declared `fileSize` exceeds the portal limit for chunked uploads or the size allowed in a knowledge folder")]
    [SwaggerResponse(403, "The caller cannot add content to the target folder, the folder is a section root, a knowledge folder does not accept this format, a `relativePath` points into a knowledge folder or an instructions room, or the file going into a private room is not marked `encrypted`")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("{folderId}/upload/create_session")]
    public async Task<ChunkedUploadSessionResultDto<T>> CreateUploadSession(SessionRequestDto<T> inDto)
    {
        var data =  await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);

        return new ChunkedUploadSessionResultDto<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <remarks>
    /// Opens a chunked upload session for a file in the folder and returns it. If `fileSize` is below
    /// `chunkUploadSize` from `GET api/2.0/files/settings`, send the whole file in one
    /// `POST api/2.0/files/{folderId}/session/{sessionId}`. That call stores the file and returns 201. For a larger
    /// file, send the parts to `POST api/2.0/files/{folderId}/session/{sessionId}/upload`. Then store the file with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. Requires the right to create content in the
    /// folder. Other callers get 403. In a room, its owner, room managers, and content creators have it. A guest has
    /// it there only as a content creator. In My documents, the owner and anyone with read and write access to the
    /// folder have it. A section root, an archived room, and an AI agent or AI project root also return 403. The call
    /// changes data. A repeated call opens another session. It creates the folders of `relativePath` that do not exist
    /// yet. If the folder has a file with this name, `createNewIfExist` true stores a copy with a numeric suffix. With
    /// false, the upload adds a new version to that file if the caller can edit it. A guest never adds a version this
    /// way. A session expires 12 hours after it opens or gets its last part. A missing folder returns 404. The call
    /// returns 400 for a negative `fileSize`. In a private room, it also returns 400 if the name is not a lowercase
    /// UUID with an optional extension. The call returns 402 if `fileSize` is over the limit for chunked uploads or
    /// for a knowledge folder. It returns 403 if a private room gets a file without `encrypted`, or a knowledge folder
    /// gets an unsupported format. It also returns 403 for a `relativePath` into a knowledge folder or an
    /// instructions room. If the installation does not allow uploads with this extension, the call returns 415.
    /// </remarks>
    /// <summary>Create an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The created upload session", typeof(ChunkedUploadSessionDto<int>))]
    [SwaggerResponse(400, "The request body cannot be read, has no `fileName`, `fileSize` is negative, or the name of a file in a private room is not a lowercase UUID with an optional lowercase extension")]
    [SwaggerResponse(402, "The declared `fileSize` exceeds the portal limit for chunked uploads or the size allowed in a knowledge folder")]
    [SwaggerResponse(403, "The caller cannot add content to the target folder, the folder is a section root, a knowledge folder does not accept this format, a `relativePath` points into a knowledge folder or an instructions room, or the file going into a private room is not marked `encrypted`")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("{folderId}/session")]
    public async Task<ChunkedUploadSessionDto<T>> CreateUploadSessionInFolder(SessionRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);
    }

    /// <remarks>
    /// Deletes a chunked upload session and the parts it has received. The file does not appear in the folder. Only
    /// the account that opened the session can delete it. Other callers get 403. The call cannot be undone, and a
    /// repeated call returns 404. The call returns 404 if the session does not exist, is finalized, has expired, or
    /// was opened for another folder. The response has no body. A session expires 12 hours after it opens or gets its
    /// last part. So an abandoned upload needs no call.
    /// </remarks>
    /// <summary>Abort an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The session and the parts received so far have been discarded")]
    [SwaggerResponse(403, "The session was opened by another account")]
    [SwaggerResponse(404, "No open session with the specified ID in this folder: it never existed, was finalized or aborted, has expired, or belongs to another folder")]
    [HttpDelete("{folderId}/session/{sessionId}")]
    public async Task AbortUploadSession(AbortSessionRequestDto<T> inDto)
    {
        await fileUploader.AbortUploadAsync(inDto.SessionId, inDto.FolderId);
    }

    //
    // [Tags("Files / Operations")]
    // [SwaggerResponse(200, "Information about created session")]
    // [SwaggerResponse(403, "You don't have enough permission to create")]
    // [HttpPut("{folderId}/session/initiate")]
    // public async Task<ChunkedUploadSessionDto<T>> InitiateUploadSession(InitiateSessionRequestDto<T> inDto)
    // {
    //     var createdSession =  await fileUploader.InitiateUploadAsync(inDto.FolderId, inDto.FileId, inDto.FileName, inDto.FileSize, inDto.Encrypted);
    //     return await chunkedUploadSessionHelper.ToResponseObjectAsync(createdSession, true);
    // }
    //

    /// <remarks>
    /// Uploads the next part of a file to a chunked upload session. Send the part as the multipart `File` field. The
    /// call adds parts in the order they arrive. Do not send two parts to one session at the same time. Any signed-in
    /// user with the session ID can call it. The call finds the session by its ID and ignores the folder in the path.
    /// The call changes data. Do not repeat a part, because the session adds it again. While bytes are missing, the
    /// response has `uploaded` false. When the parts reach the size of the session, the call stores the file. It then
    /// returns 201, `uploaded` true, and the file in `file`. A session below `chunkUploadSize` from
    /// `GET api/2.0/files/settings` stores the file on the first call. It needs no
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. The call returns 400 if the request has no `File`
    /// part. It returns 402 if the part is over `chunkUploadSize`, or the file is over a quota or size limit. If the
    /// session does not exist, is finalized, or has expired, the call returns 404. If the file is not a PDF and the
    /// room is a form-filling room, the call returns 403.
    /// </remarks>
    /// <summary>Upload the next chunk</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}</path>
    [RaisesWebhook(WebhookTrigger.FileUploaded, When = "on the request that completes the upload, without `file.created`")]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The part is stored and bytes are still missing; `uploaded` is false", typeof(UploadSessionResponseDto<int>))]
    [SwaggerResponse(201, "The last part has arrived and the file is stored; `uploaded` is true and `file` holds the file", typeof(UploadSessionResponseDto<int>))]
    [SwaggerResponse(400, "The request has no `File` part")]
    [SwaggerResponse(402, "The part is larger than `chunkUploadSize`, or storing the file would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "A file that is not a PDF is stored in a form-filling room")]
    [SwaggerResponse(404, "No open session with the specified ID in this folder: it never existed, was finalized or aborted, has expired, or belongs to another folder")]
    [HttpPost("{folderId}/session/{sessionId}")]
    public async Task<UploadSessionResponseDto<T>> UploadSession(UploadSessionRequestDto<T> inDto)
    {
        // a multipart request without the part used to fail with a NullReferenceException, answered as 500
        if (inDto.File == null)
        {
            throw new ArgumentException("The request has no file part", nameof(inDto.File));
        }

        var resumedSession = await fileUploader.UploadChunkAsync<T>(inDto.SessionId, inDto.File.OpenReadStream(),  inDto.File.Length);
        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);

        var transferredBytes = await fileUploader.GetTransferredBytesCountAsync(resumedSession);
        if (transferredBytes == resumedSession.BytesTotal || !resumedSession.UseChunks)
        {
            if (resumedSession.UseChunks)
            {
                resumedSession = await fileUploader.FinalizeUploadSessionAsync<T>(inDto.SessionId);
            }
            else
            {
                // A small session stored the file with this part, so it ends here as a large one does when finalized.
                await chunkedUploadSessionHolder.RemoveSessionAsync(resumedSession);
            }

            await fileUploader.DeleteLinkAndMarkAsync(resumedSession.File);

            await filesMessageService.SendAsync(resumedSession.File.Version > 1
                ? MessageAction.FileUploadedWithOverwriting
                : MessageAction.FileUploaded, resumedSession.File, resumedSession.File.Title);

            this.HttpContext.Response.StatusCode = 201;
            await webhookManager.PublishAsync(WebhookTrigger.FileUploaded, resumedSession.File);

            await socketManager.CreateFileAsync(resumedSession.File);
            if (resumedSession.File.Version <= 1)
            {
                var folderDao = daoFactory.GetFolderDao<T>();
                var room = await folderDao.GetParentFoldersAsync(resumedSession.FolderId).FirstOrDefaultAsync(f => f.IsRoom);
                if (room != null)
                {
                    var data = room.Id is int rId && resumedSession.File.Id is int fId
                        ? new RoomNotifyIntegrationData<int> { RoomId = rId, FileId = fId }
                        : null;

                    var thirdPartyData = room.Id is string srId && resumedSession.File.Id is string sfId
                        ? new RoomNotifyIntegrationData<string> { RoomId = srId, FileId = sfId }
                        : null;

                    var evt = new RoomNotifyIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant().Id) { Data = data, ThirdPartyData = thirdPartyData };

                    await eventBus.PublishAsync(evt);
                }
            }

            return new UploadSessionResponseDto<T>
            {
                ID = resumedSession.File.Id,
                FolderId = resumedSession.File.ParentId,
                Version = resumedSession.File.Version,
                Title = resumedSession.File.Title,
                ProviderKey = resumedSession.File.ProviderKey,
                Uploaded = true,
                File = await _fileDtoHelper.GetAsync(resumedSession.File)
            };
        }

        return new UploadSessionResponseDto<T>
        {
            ID = resumedSession.File.Id,
            FolderId = resumedSession.File.ParentId,
            Version = resumedSession.File.Version,
            Title = resumedSession.File.Title,
            ProviderKey = resumedSession.File.ProviderKey,
            File = await _fileDtoHelper.GetAsync(resumedSession.File)
        };
    }

    /// <remarks>
    /// Uploads one part of a file to a chunked upload session under the number in `chunkNumber`. The session keeps
    /// parts by number. So you can resend a failed part with the same number. Without `chunkNumber`, the call numbers
    /// parts in the order they arrive. Split the file into parts of at most `chunkUploadSize` from
    /// `GET api/2.0/files/settings`. Any signed-in user with the session ID can call it. The call ignores the folder
    /// in the path. It changes data. A repeated part replaces the part with that number. The call returns the
    /// session, not the file. After the last part, store the file with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. A session below `chunkUploadSize` stores the file
    /// on this call but still needs that call. To store the file with the last part, use
    /// `POST api/2.0/files/{folderId}/session/{sessionId}`. The call returns 400 if the request has no `File` part,
    /// or `chunkNumber` is below 1. It returns 402 if the part is over `chunkUploadSize`, or the file is over a quota
    /// or size limit. If the session does not exist, is finalized, or has expired, the call returns 404. If the file
    /// is not a PDF and the room is a form-filling room, the call returns 403.
    /// </remarks>
    /// <summary>Upload a numbered chunk</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}/upload</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The session with its progress after the part was stored", typeof(ChunkedUploadSessionDto<int>))]
    [SwaggerResponse(400, "The request has no `File` part, or `chunkNumber` is 0 or negative")]
    [SwaggerResponse(402, "The part is larger than `chunkUploadSize`, or a session below that size would exceed a storage quota or size limit when storing the file")]
    [SwaggerResponse(403, "A session below `chunkUploadSize` stores a file that is not a PDF in a form-filling room")]
    [SwaggerResponse(404, "No open session with the specified ID in this folder: it never existed, was finalized or aborted, has expired, or belongs to another folder")]
    [HttpPost("{folderId}/session/{sessionId}/upload")]
    public async Task<ChunkedUploadSessionDto<T>> UploadAsyncSession(UploadSessionAsyncRequestDto<T> inDto)
    {
        // a multipart request without the part used to fail with a NullReferenceException, answered as 500
        if (inDto.File == null)
        {
            throw new ArgumentException("The request has no file part", nameof(inDto.File));
        }

        var resumedSession = await fileUploader.UploadChunkAsync<T>(inDto.SessionId, inDto.File.OpenReadStream(), inDto.File.Length, inDto.ChunkNumber);
        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);
        return await chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession);
    }

    /// <remarks>
    /// Stores the file of a chunked upload session from its parts and closes the session. A session from
    /// `POST api/2.0/files/file/{fileId}/edit_session` replaces the content of that file and keeps its version. A
    /// session for a folder creates a file, or adds a new version to the file with the same name. A PDF form loses
    /// its form-filling state. Any signed-in user with the session ID can call it. The call returns 201 with the
    /// file. It changes data and deletes the session with its parts. A repeated call returns 404. If the parts do not
    /// add up to the size of the session, the call returns 400. Then send the missing parts and repeat the call. It
    /// returns 402 if the file is over a quota or size limit. A missing, finalized, or expired session returns 404. If the file is not a PDF and the room is a form-filling room, the call returns 403.
    /// </remarks>
    /// <summary>Finalize an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}/finalize</path>
    [RaisesWebhook(WebhookTrigger.FileUploaded, When = "without `file.created`")]
    [Tags("Files / Operations")]
    [SwaggerResponse(201, "The assembled file and the identifiers of the closed session", typeof(UploadSessionResponseDto<int>))]
    [SwaggerResponse(400, "The parts received so far do not add up to the size the session was opened for")]
    [SwaggerResponse(402, "Storing the file would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "A file that is not a PDF is stored in a form-filling room")]
    [SwaggerResponse(404, "No open session with the specified ID in this folder: it never existed, was finalized or aborted, has expired, or belongs to another folder")]
    [HttpPut("{folderId}/session/{sessionId}/finalize")]
    public async Task<UploadSessionResponseDto<T>> FinalizeSession(FinalizeSessionDto<T> inDto)
    {
        var session = await chunkedUploadSessionHolder.GetSessionAsync<T>(inDto.SessionId);
        if (session.UseChunks)
        {
            session = await fileUploader.FinalizeUploadSessionAsync<T>(inDto.SessionId);
        }
        else
        {
            // Left open, a small session could be aborted, finalized again or written to after this call.
            await chunkedUploadSessionHolder.RemoveSessionAsync(session);
        }

        await fileUploader.DeleteLinkAndMarkAsync(session.File);

        await filesMessageService.SendAsync(session.File.Version > 1
            ? MessageAction.FileUploadedWithOverwriting
            : MessageAction.FileUploaded, session.File, session.File.Title);

        await webhookManager.PublishAsync(WebhookTrigger.FileUploaded, session.File);

        if (session.File.Version <= 1)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var parents = await folderDao.GetParentFoldersAsync(session.FolderId).ToListAsync();
            var room = parents.FirstOrDefault(f => f.IsRoom);
            if (room != null)
            {
                var data = room.Id is int rId && session.File.Id is int fId
                    ? new RoomNotifyIntegrationData<int> { RoomId = rId, FileId = fId }
                    : null;

                var thirdPartyData = room.Id is string srId && session.File.Id is string sfId
                    ? new RoomNotifyIntegrationData<string> { RoomId = srId, FileId = sfId }
                    : null;

                var evt = new RoomNotifyIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant().Id) { Data = data, ThirdPartyData = thirdPartyData };

                await eventBus.PublishAsync(evt);
            }
        }

        await socketManager.CreateFileAsync(session.File);
        this.HttpContext.Response.StatusCode = 201;

        return new UploadSessionResponseDto<T>
        {
            ID = session.File.Id,
            FolderId = session.File.ParentId,
            Version = session.File.Version,
            Title = session.File.Title,
            ProviderKey = session.File.ProviderKey,
            Uploaded = true,
            File = await _fileDtoHelper.GetAsync(session.File)
        };
    }

    /// <remarks>
    /// Opens a chunked upload session that replaces the content of an existing file. WebDAV apps save over a document
    /// this way. No content is stored yet. Send the parts to `POST api/2.0/files/{folderId}/session/{sessionId}/upload`,
    /// with the folder of the file as `folderId`. Then store the content with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. The new content replaces the current version and
    /// adds no version. The file also loses its encrypted state and its converted copy. Requires editing access to the
    /// file. Other callers get 403. A guest also gets 403. The call changes data. A repeated call opens another
    /// session. A
    /// `fileSize` of -1 means the size is unknown, as for a WebDAV body without Content-Length. If the file does not
    /// exist, the call returns 404. The call returns 400 if `fileSize` is below -1. It returns 402 if `fileSize` is
    /// over the portal limit for chunked uploads. It also returns 403 if the file is locked, open in the editor, in
    /// the trash, or encrypted.
    /// </remarks>
    /// <summary>Create the editing session</summary>
    /// <path>api/2.0/files/file/{fileId}/edit_session</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created editing session, wrapped in the success envelope", typeof(ChunkedUploadSessionResultDto<int>))]
    [SwaggerResponse(400, "`fileSize` is below -1")]
    [SwaggerResponse(402, "The declared `fileSize` exceeds the portal limit for chunked uploads")]
    [SwaggerResponse(403, "The caller cannot edit this file, or the file is locked, open in the editor, in the trash or encrypted")]
    [SwaggerResponse(404, "No file with the specified ID")]
    [HttpPost("file/{fileId}/edit_session")]
    public async Task<ChunkedUploadSessionResultDto<T>> CreateEditSession(CreateEditSessionRequestDto<T> inDto)
    {
        var data = await filesControllerHelper.CreateEditSessionAsync(inDto.FileId, inDto.FileSize);
        return new ChunkedUploadSessionResultDto<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <remarks>
    /// Returns the names from `filesTitle` that a file in the folder already has. Use it before an upload to choose
    /// between a new version and another name. The match ignores case. The call skips encrypted files, since an
    /// upload cannot replace them. Requires the right to create content in the folder. Other callers get 403. In a
    /// room, its owner, room managers, and content creators have it. A guest has it there only as a content creator.
    /// In My documents, the owner and anyone with read and write access to the folder have it. An archived room, the
    /// Archive or room templates root, and an AI agent or AI project root also return 403. The call is read-only. The
    /// list has no repeats and no order. An empty list means that every name is free. A missing folder returns 404.
    /// The call returns 400 if `filesTitle` is missing. An empty `filesTitle` returns an empty list. In a form-filling
    /// room template, a name without the `.pdf` extension returns 403.
    /// </remarks>
    /// <summary>Check for upload conflicts</summary>
    /// <path>api/2.0/files/{folderId}/upload/check</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The submitted titles that already belong to a file in the folder", typeof(HashSet<string>))]
    [SwaggerResponse(400, "The request body cannot be read or has no `filesTitle`")]
    [SwaggerResponse(403, "The caller cannot add content to the folder, or a title that is not a PDF is checked against a form-filling room template")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [HttpPost("{folderId}/upload/check")]
    public async Task<HashSet<string>> CheckUploadAsync(CheckUploadRequestDto<T> model)
    {
        var folderId = model.FolderId;
        var filesTitle = model.Check?.FilesTitle;

        if (filesTitle == null)
        {
            throw new ArgumentNullException(nameof(filesTitle));
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var toFolder = await folderDao.GetFolderAsync(folderId);

        if (toFolder == null)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }


        if (!await fileSecurity.CanCreateAsync(toFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        if (toFolder.FolderType == FolderType.FillingFormsRoom && toFolder.RootFolderType == FolderType.RoomTemplates && filesTitle.Any(r => FileUtility.GetFileExtension(r) != ".pdf"))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_UploadToFormRoom);
        }

        var result = new HashSet<string>();

        foreach (var title in filesTitle)
        {
            var file = await fileDao.GetFileAsync(folderId, title);
            if (file is { Encrypted: false })
            {
                result.Add(title);
            }
        }

        return result;
    }

    /// <remarks>
    /// Uploads a file to the folder in one request and names it by `title`. The content can be a multipart part or
    /// the raw request body. To name the file by the uploaded part, use `POST api/2.0/files/{folderId}/upload`.
    /// Requires the right to create content in the folder. Other callers get 403. In a room, its owner, room managers,
    /// and content creators have it. A guest has it there only as a content creator. In My documents, the owner and
    /// anyone with read and write access to the folder have it. A section root, an archived or private room, and an
    /// AI agent or AI project root also return 403. The call changes data. It removes characters that a title cannot
    /// have and shortens a long name. If the folder has a file with this name, `createNewIfExist` true stores a copy
    /// with a numeric suffix. With false, the call adds a new version to that file if the caller can edit it.
    /// Otherwise, it stores a second file with the same name. A guest never adds a version this way. So a repeated
    /// call adds another version or another file. A `version` above 1 in the response means that the call added a
    /// version. The portal converts some formats in the background after the upload. To read that result with
    /// `GET api/2.0/files/file/{fileId}/checkconversion`, pass `keepConvertStatus`. A missing folder returns 404.
    /// The call returns 402 if the file is over the upload limit or a storage quota. If the installation does not
    /// allow uploads with this extension, the call returns 415. If the file is not a PDF and the room is a
    /// form-filling room, the call returns 403. To upload a large file, use `POST api/2.0/files/{folderId}/session`.
    /// </remarks>
    /// <summary>Insert a file</summary>
    /// <path>api/2.0/files/{folderId}/insert</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file with its id and the title it was actually saved under; a `version` above 1 means the content was added to a file that already carried this name instead of creating a new one", typeof(FileDto<int>))]
    [SwaggerResponse(402, "The file exceeds the single-request upload limit, or storing it would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "The caller cannot add content to this folder, the folder is a section root or in a private room, or a file that is not a PDF is stored in a form-filling room")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("{folderId}/insert", Order = 1)]
    public async Task<FileDto<T>> InsertFile(InsertWithFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.InsertFileAsync(inDto.FolderId, inDto.InsertFile.Stream, inDto.InsertFile.Title, inDto.InsertFile.CreateNewIfExist, inDto.InsertFile.KeepConvertStatus);
    }


    /// <remarks>
    /// Uploads a file to the folder in one multipart request and names it by the uploaded part. To give the name
    /// separately or send a raw body, use `POST api/2.0/files/{folderId}/insert`. Requires the right to create
    /// content in the folder. Other callers get 403. In a room, its owner, room managers, and content creators have
    /// it. A guest has it there only as a content creator. In My documents, the owner and anyone with read and write
    /// access to the folder have it. A section root, an archived or private room, and an AI agent or AI project root
    /// also return 403. The call changes data. If the folder has a file with this name, `createNewIfExist` true stores
    /// a copy with a numeric suffix. With false, the call adds a new version to that file if the caller can edit it.
    /// Otherwise, it stores a second file with the same name. A guest never adds a version this way. So a repeated
    /// call adds another version or another file. The response is a list with exactly one file. The call saves
    /// `storeOriginalFile` as a setting of the caller, like `PUT api/2.0/files/storeoriginal`. It saves the setting
    /// even when the upload then fails. Later uploads use it too. The call returns 400 if the request has no file
    /// part. A missing folder returns 404. It returns 402 if the file is over the upload limit or a storage quota. If
    /// the installation does not allow uploads with this extension, the call returns 415. If the file is not a PDF
    /// and the room is a form-filling room, the call returns 403.
    /// </remarks>
    /// <summary>Upload a file</summary>
    /// <path>api/2.0/files/{folderId}/upload</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file, as a list with one element", typeof(List<FileDto<int>>))]
    [SwaggerResponse(400, "The request has no file part")]
    [SwaggerResponse(402, "The file exceeds the single-request upload limit, or storing it would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "The caller cannot add content to this folder, the folder is a section root or in a private room, or a file that is not a PDF is stored in a form-filling room")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("{folderId}/upload", Order = 1)]
    public async Task<List<FileDto<T>>> UploadFile(UploadWithFolderRequestDto<T> inDto)
    {
        return await filesControllerHelper.UploadFileAsync(inDto.FolderId, inDto);
    }
}

public class UploadControllerCommon(GlobalFolderHelper globalFolderHelper,
        UploadControllerHelper filesControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Stores one file in the Common section of the portal and returns it. Send the content as a
    /// `multipart/form-data` part or as the raw request body. The call names the file by `title`, not by the name
    /// of the part. Requires the right to create files in the Common section. Other callers get 403. The call
    /// changes data. By default, a file with the same title gets a new version. If that file is locked or open in
    /// the editor, the call stores a second file with the same title. With `createNewIfExist` set to true, the call
    /// adds a numeric suffix to the new title. Do not repeat the call. Each call adds a version or a file. The
    /// portal queues the conversion of the formats in `extsMustConvert` of `GET api/2.0/files/settings`. If the
    /// Common section is not found, the call returns 404.
    /// </remarks>
    /// <summary>Insert a file to the "Common" section</summary>
    /// <path>api/2.0/files/@common/insert</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded, When = "for a new file and for a new version of an existing file")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPost("@common/insert")]
    public async Task<FileDto<int>> InsertFileToCommonFromBody([FromForm][ModelBinder(BinderType = typeof(InsertFileModelBinder))] InsertFileRequestDto inDto)
    {
        return await filesControllerHelper.InsertFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Stream, inDto.Title, inDto.CreateNewIfExist, inDto.KeepConvertStatus);
    }

    /// <remarks>
    /// Stores one file in My documents of the caller and returns it. Send the content as a `multipart/form-data` part
    /// or as the raw request body. The call names the file by `title`, not by the name of the part. Any signed-in
    /// user can call it, except a guest. A guest has no My documents, so the call returns 404. If the account cannot
    /// create files there, the call returns 403. The call changes data. By default, a file with the same title gets a
    /// new version. If that file is locked or open in the editor, the call stores a second file with the same title.
    /// With `createNewIfExist` set to true, the call adds a numeric suffix to the new title. Do not repeat the call.
    /// Each call adds a version or a file. The portal queues the conversion of the formats in `extsMustConvert` of
    /// `GET api/2.0/files/settings`. The call returns 402 if the file is over the size limit of one request or the
    /// storage quota. Send a large file through `POST api/2.0/files/{folderId}/upload/create_session`. If the
    /// installation does not allow the file extension, the call returns 415. To store a file in another folder, use
    /// `POST api/2.0/files/{folderId}/insert`.
    /// </remarks>
    /// <summary>Insert a file into My documents</summary>
    /// <path>api/2.0/files/@my/insert</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file, with the identifier, version and title it was saved under", typeof(FileDto<int>))]
    [SwaggerResponse(402, "The file exceeds the single-request upload limit, or storing it would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "Creating a file in the personal section is not allowed for this account")]
    [SwaggerResponse(404, "The caller has no personal section, so there is nothing to store the file in")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("@my/insert")]
    public async Task<FileDto<int>> InsertFileToMyFromBody([FromForm][ModelBinder(BinderType = typeof(InsertFileModelBinder))] InsertFileRequestDto inDto)
    {
        return await filesControllerHelper.InsertFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Stream, inDto.Title, inDto.CreateNewIfExist, inDto.KeepConvertStatus);
    }

    /// <remarks>
    /// Uploads one file to the Common section of the portal and returns it in an array with one item. Send it as a
    /// `multipart/form-data` part. The file takes the name of the part. Requires the right to create files in the
    /// Common section. Other callers get 403. The call changes data. By default, a file with the same title gets a
    /// new version. If that file is locked or open in the editor, the call stores a second file with the same title.
    /// With `createNewIfExist` set to true, the call adds a numeric suffix to the new title. Do not repeat the call.
    /// Each call adds a version or a file. The `storeOriginalFile` flag also changes an account setting for later
    /// uploads, like `PUT api/2.0/files/storeoriginal`. The portal queues the conversion of the formats in
    /// `extsMustConvert` of `GET api/2.0/files/settings`. If the Common section is not found, the call returns 404.
    /// </remarks>
    /// <summary>Upload a file to the "Common" section</summary>
    /// <path>api/2.0/files/@common/upload</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded, When = "for a new file and for a new version of an existing file")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Uploaded file(s)", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("@common/upload")]
    public async Task<List<FileDto<int>>> UploadFileToCommon(UploadRequestDto inDto)
    {
        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderCommonAsync, inDto);
    }

    /// <remarks>
    /// Uploads one file to My documents of the caller and returns it in an array with one item. Send it as a
    /// `multipart/form-data` part. The file takes the name of the part. To set another name, use
    /// `POST api/2.0/files/@my/insert`. Any signed-in user can call it, except a guest. A guest has no My documents,
    /// so the call returns 404. If the account cannot create files there, the call returns 403. The call changes
    /// data. By default, a file with the same title gets a new version. If that file is locked or open in the editor,
    /// the call stores a second file with the same title. With `createNewIfExist` set to true, the call adds a
    /// numeric suffix to the new title. Do not repeat the call. Each call adds a version or a file. The
    /// `storeOriginalFile` flag also changes an account setting for later uploads, like
    /// `PUT api/2.0/files/storeoriginal`. The portal queues the conversion of the formats in `extsMustConvert` of
    /// `GET api/2.0/files/settings`. If the request has no file part, the call returns 400. The call returns 402 if
    /// the file is over the size limit of one request or the storage quota. Send a large file through
    /// `POST api/2.0/files/{folderId}/upload/create_session`. If the installation does not allow the file extension,
    /// the call returns 415.
    /// </remarks>
    /// <summary>Upload a file to My documents</summary>
    /// <path>api/2.0/files/@my/upload</path>
    [RaisesWebhook(WebhookTrigger.FileCreated, WebhookTrigger.FileUploaded, When = "for a new file and for a new version of an existing file")]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "An array holding the single uploaded file", typeof(List<FileDto<int>>))]
    [SwaggerResponse(400, "The request has no file part")]
    [SwaggerResponse(402, "The file exceeds the single-request upload limit, or storing it would exceed a storage quota or size limit")]
    [SwaggerResponse(403, "Uploading a file to the personal section is not allowed for this account")]
    [SwaggerResponse(404, "The caller has no personal section, so there is nothing to store the file in")]
    [SwaggerResponse(415, "The installation restricts uploadable formats and the file extension is not among them")]
    [HttpPost("@my/upload")]
    public async Task<List<FileDto<int>>> UploadFileToMy(UploadRequestDto inDto)
    {
        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderMyAsync, inDto);
    }
}
