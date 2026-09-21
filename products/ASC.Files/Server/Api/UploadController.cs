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
    /// Deprecated in favour of `POST api/2.0/files/{folderId}/session`, which opens the same session and returns it
    /// without the success envelope used here; new callers should go there. Reserves a chunked upload of a file in
    /// the folder named by the path: the title comes from `fileName`, the declared payload size from `fileSize`, and
    /// the answer carries the session id every later call quotes, the address of the standalone chunk handler, the
    /// moment an idle session is dropped and the reserved byte count. No content is stored yet. Send the payload as
    /// multipart parts to `POST api/2.0/files/{folderId}/session/{sessionId}/upload`, keeping each part within
    /// `chunkUploadSize` from `GET api/2.0/files/settings`, then close the session with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. The caller needs the right to add content to the
    /// target folder, which room managers and content creators have and readers, editors and guests do not: they get
    /// 403, as does a section root such as Rooms or Archive, while an unknown folder is answered as missing. A
    /// payload above the portal limit for chunked uploads is refused before the session exists.
    /// </remarks>
    /// <summary>Chunked upload</summary>
    /// <path>api/2.0/files/{folderId}/upload/create_session</path>
    [Obsolete]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The created session, wrapped in the success envelope", typeof(ChunkedUploadSessionResponseWrapper<int>))]
    [SwaggerResponse(403, "The caller cannot add content to the target folder")]
    [HttpPost("{folderId}/upload/create_session")]
    public async Task<ChunkedUploadSessionResponseWrapper<T>> CreateUploadSession(SessionRequestDto<T> inDto)
    {
        var data =  await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);

        return new ChunkedUploadSessionResponseWrapper<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <remarks>
    /// Opens a chunked upload session for a file in the folder named by the path and returns the session itself,
    /// which is the difference from the deprecated `POST api/2.0/files/{folderId}/upload/create_session` and its
    /// success envelope. The answer gives `id`, quoted by every later call, `location` for the standalone chunk
    /// handler used by clients that bypass this API, `expired`, and `bytes_total` echoing the reserved size. Whether
    /// parts are really needed follows from `fileSize`: below `chunkUploadSize` from `GET api/2.0/files/settings` the
    /// whole payload goes in one `POST api/2.0/files/{folderId}/session/{sessionId}`, which stores the file and
    /// answers 201, and above it the parts go one by one to
    /// `POST api/2.0/files/{folderId}/session/{sessionId}/upload` and the file appears only after
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. The caller must be allowed to add content to the
    /// folder, so readers, editors and guests are refused, a section root is refused as well, and an unknown folder
    /// is answered as missing. Nothing is written until the parts arrive, and an abandoned session disappears twelve
    /// hours later.
    /// </remarks>
    /// <summary>Create an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The created upload session", typeof(ChunkedUploadSessionResponse<int>))]
    [HttpPost("{folderId}/session")]
    public async Task<ChunkedUploadSessionResponse<T>> CreateUploadSessionInFolder(SessionRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);
    }

    /// <remarks>
    /// Cancels a chunked upload opened with `POST api/2.0/files/{folderId}/session` and discards the parts already
    /// received, so nothing of it reaches the folder. The session is found by the id in the path alone: the folder
    /// segment is not matched against it, and neither is the account that opened it, which makes the id the only
    /// secret protecting the transfer. The call is destructive and is not safe to repeat, because the record is gone
    /// afterwards: a second attempt, a session already closed by
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize` and a session that expired after twelve hours of
    /// silence all fail rather than answer as missing. Finalizing removes the session too, so there is nothing left
    /// to abort once the file exists. The answer carries no body. An upload that is simply abandoned needs no call at
    /// all, since the session and its buffered parts are dropped when it expires.
    /// </remarks>
    /// <summary>Abort an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The session and the parts received so far have been discarded")]
    [HttpDelete("{folderId}/session/{sessionId}")]
    public async Task AbortUploadSession(AbortSessionRequestDto<T> inDto)
    {
        await fileUploader.AbortUploadAsync<T>(inDto.SessionId);
    }

    //
    // [Tags("Files / Operations")]
    // [SwaggerResponse(200, "Information about created session")]
    // [SwaggerResponse(403, "You don't have enough permission to create")]
    // [HttpPut("{folderId}/session/initiate")]
    // public async Task<ChunkedUploadSessionResponse<T>> InitiateUploadSession(InitiateSessionRequestDto<T> inDto)
    // {
    //     var createdSession =  await fileUploader.InitiateUploadAsync(inDto.FolderId, inDto.FileId, inDto.FileName, inDto.FileSize, inDto.Encrypted);
    //     return await chunkedUploadSessionHelper.ToResponseObjectAsync(createdSession, true);
    // }
    //

    /// <remarks>
    /// Sends the next part of a file into the session opened for it, as the multipart `File` field, and lets the
    /// server keep count: parts are appended in the order they arrive, so two of these calls must never run in
    /// parallel on one session. While bytes are still missing the answer describes the session and `uploaded` is
    /// false; when the last part completes the declared size the file is written, its upload links are cleared, it is
    /// marked as new for the room, and the answer comes back with 201, `uploaded` true and the whole file in `file`.
    /// A session created for a payload smaller than `chunkUploadSize` from `GET api/2.0/files/settings` finishes on
    /// the first such call and needs no separate finalize step. A part larger than that limit is refused. The first
    /// part of a PDF is inspected, and a PDF that is not a fillable form is refused when the session targets a
    /// form-filling room. The session is addressed by its id, and the folder in the path is not matched against it.
    /// </remarks>
    /// <summary>Upload the next chunk</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The progress of the session, or the stored file once the last part has arrived", typeof(UploadSessionResponseDto<int>))]
    [HttpPost("{folderId}/session/{sessionId}")]
    public async Task<UploadSessionResponseDto<T>> UploadSession(UploadSessionRequestDto<T> inDto)
    {
        var resumedSession = await fileUploader.UploadChunkAsync<T>(inDto.SessionId, inDto.File.OpenReadStream(),  inDto.File.Length);
        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);

        var transferredBytes = await fileUploader.GetTransferredBytesCountAsync(resumedSession);
        if (transferredBytes == resumedSession.BytesTotal || !resumedSession.UseChunks)
        {
            if (resumedSession.UseChunks)
            {
                resumedSession = await fileUploader.FinalizeUploadSessionAsync<T>(inDto.SessionId);
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
    /// Stores one part of a file under the number given in `chunkNumber`, which is what the ordinary chunked flow
    /// uses: parts are kept by their number rather than by arrival, so a part that failed can be resent under the
    /// same number without restarting the session. Numbering starts at 1, and leaving the number out makes the server
    /// count the parts itself. The answer is always the session, never the file, and this call never completes the
    /// upload: the file appears only after `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`. Use
    /// `POST api/2.0/files/{folderId}/session/{sessionId}` instead when the parts go strictly in order and the upload
    /// should complete by itself. A part bigger than `chunkUploadSize` from `GET api/2.0/files/settings` is refused,
    /// so that value is also the size to split the payload by. The first part of a PDF is inspected, and a PDF that
    /// is not a fillable form is refused when the session targets a form-filling room. The session is found by its id
    /// alone.
    /// </remarks>
    /// <summary>Upload a numbered chunk</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}/upload</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The session with its progress after the part was stored", typeof(ChunkedUploadSessionResponse<int>))]
    [HttpPost("{folderId}/session/{sessionId}/upload")]
    public async Task<ChunkedUploadSessionResponse<T>> UploadAsyncSession(UploadSessionAsyncRequestDto<T> inDto)
    {
        var resumedSession = await fileUploader.UploadChunkAsync<T>(inDto.SessionId, inDto.File.OpenReadStream(), inDto.File.Length, inDto.ChunkNumber);
        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);
        return await chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession);
    }

    /// <remarks>
    /// Assembles the parts received so far into the file the session was opened for and closes the session. What
    /// comes out depends on how the session started: one opened against an existing file through
    /// `POST api/2.0/files/file/{fileId}/edit_session` replaces that content in place and keeps the version number,
    /// while one opened against a folder either creates the file or, when a file of the same name was taken over,
    /// stores the content as its next version. A form loses its filling state on the way in. The answer arrives with
    /// 201 and carries the identifiers of the file together with the file itself. The call ends the session: the
    /// record and the buffered parts are removed, so it cannot be repeated and there is nothing left to abort
    /// afterwards. Running it before all the declared bytes have arrived assembles whatever is there, so read the
    /// progress from the chunk calls first. An unknown, already closed or expired session id fails instead of
    /// answering as missing.
    /// </remarks>
    /// <summary>Finalize an upload session</summary>
    /// <path>api/2.0/files/{folderId}/session/{sessionId}/finalize</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "The assembled file and the identifiers of the closed session", typeof(UploadSessionResponseDto<int>))]
    [HttpPut("{folderId}/session/{sessionId}/finalize")]
    public async Task<UploadSessionResponseDto<T>> FinalizeSession(FinalizeSessionDto<T> inDto)
    {
        var session = await chunkedUploadSessionHolder.GetSessionAsync<T>(inDto.SessionId);
        if (session.UseChunks)
        {
            session = await fileUploader.FinalizeUploadSessionAsync<T>(inDto.SessionId);
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
    /// Opens a chunked session that replaces the content of an existing file, which is how WebDAV clients save over a
    /// document. The answer carries the session id the later calls quote, the address of the standalone chunk
    /// handler, the expiry and the reserved size, and nothing is written until the parts reach
    /// `POST api/2.0/files/{folderId}/session/{sessionId}/upload` and the session is closed with
    /// `PUT api/2.0/files/{folderId}/session/{sessionId}/finalize`, where `folderId` is the folder the file lives in.
    /// Unlike an upload into a folder, the finished content does not become a new version: it overwrites the current
    /// one, and the file loses its encrypted flag and its stored conversion result in the process. The caller must be
    /// allowed to edit the file, as the owner, a room manager and a member invited with editing rights are; a reader
    /// and a guest get 403. A file that does not exist is answered as missing, and a payload above the portal limit
    /// for chunked uploads is refused before the session is created.
    /// </remarks>
    /// <summary>Create the editing session</summary>
    /// <path>api/2.0/files/file/{fileId}/edit_session</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The created editing session, wrapped in the success envelope", typeof(ChunkedUploadSessionResponseWrapper<int>))]
    [SwaggerResponse(403, "The caller cannot edit this file")]
    [HttpPost("file/{fileId}/edit_session")]
    public async Task<ChunkedUploadSessionResponseWrapper<T>> CreateEditSession(CreateEditSessionRequestDto<T> inDto)
    {
        var data = await filesControllerHelper.CreateEditSessionAsync(inDto.FileId, inDto.FileSize);
        return new ChunkedUploadSessionResponseWrapper<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <remarks>
    /// Reports which of the submitted titles already belong to a file in the folder, so an upload can decide in
    /// advance whether to overwrite or to ask for another name. Only the clashing titles come back, unordered and
    /// without repetitions, and an empty array means every name is free. Matching is by title and ignores case, so a
    /// name that differs only in capitalisation is still reported; an existing file that is encrypted is left out,
    /// because an upload cannot take it over. The call changes nothing. It needs the same right as the upload itself,
    /// the right to add content to the folder, which room managers and content creators have and readers, editors and
    /// guests do not; an archived room, a section root and a folder the caller cannot write to are all refused, while
    /// an unknown folder is answered as missing. A request without `filesTitle` is rejected as an invalid request, an
    /// empty list is accepted and answers with an empty array.
    /// </remarks>
    /// <summary>Check for upload conflicts</summary>
    /// <path>api/2.0/files/{folderId}/upload/check</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The submitted titles that already belong to a file in the folder", typeof(HashSet<string>))]
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
    /// Stores a file in the folder named by the path in a single request, taking its name from `title` rather than
    /// from the uploaded part, which is what separates it from `POST api/2.0/files/{folderId}/upload`. The content
    /// may arrive either as a multipart part or as the raw request body. The name is stripped of characters a title
    /// cannot hold and truncated, and `createNewIfExist` settles the clash: false adds a new version to the file that
    /// already carries the name, true keeps both by giving the new one a numeric suffix. The caller needs the right
    /// to add content to the folder, so a reader, an editor and a guest get 403, a section root and an archived room
    /// are refused as well, and an unknown folder gives 404. Formats the portal converts are converted afterwards in
    /// the background; pass `keepConvertStatus` to keep the outcome readable through
    /// `GET api/2.0/files/file/{fileId}/checkconversion`. The answer is the stored file. A large payload belongs in a
    /// chunked session instead.
    /// </remarks>
    /// <summary>Insert a file</summary>
    /// <path>api/2.0/files/{folderId}/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "The caller cannot add content to this folder")]
    [SwaggerResponse(404, "No folder with the specified ID")]
    [HttpPost("{folderId}/insert", Order = 1)]
    public async Task<FileDto<T>> InsertFile(InsertWithFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.InsertFileAsync(inDto.FolderId, inDto.InsertFile.Stream, inDto.InsertFile.Title, inDto.InsertFile.CreateNewIfExist, inDto.InsertFile.KeepConvertStatus);
    }


    /// <remarks>
    /// Stores a file in the folder named by the path in a single multipart request, taking its name from the uploaded
    /// part; use `POST api/2.0/files/{folderId}/insert` when the name has to be given separately or the content is
    /// sent as a raw body. The answer is a list that always holds exactly one file. `createNewIfExist` settles the
    /// clash: false adds a new version to the file that already carries the name, true keeps both by giving the new
    /// one a numeric suffix. `storeOriginalFile` reaches further than this call, because it saves the setting on the
    /// calling account, the same one `PUT api/2.0/files/storeoriginal` writes, and it stays in force for later
    /// uploads. The caller needs the right to add content to the folder, so a reader, an editor and a guest get 403,
    /// a section root and an archived room are refused as well, and an unknown folder gives 404. A request without a
    /// file is rejected as invalid, and a payload above the portal upload limit is refused.
    /// </remarks>
    /// <summary>Upload a file</summary>
    /// <path>api/2.0/files/{folderId}/upload</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file, as a list with one element", typeof(List<FileDto<int>>))]
    [SwaggerResponse(403, "The caller cannot add content to this folder")]
    [SwaggerResponse(404, "No folder with the specified ID")]
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
    /// Inserts a file specified in the request to the "Common" section by single file uploading.
    /// </remarks>
    /// <summary>Insert a file to the "Common" section</summary>
    /// <path>api/2.0/files/@common/insert</path>
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
    /// Stores one file in the caller's own My documents section, the personal storage every portal member has, and
    /// returns the stored file. The destination takes no identifier: it is resolved from the calling account and
    /// created on first use, while a guest account has none and is answered as missing (404). Send the content as a
    /// `multipart/form-data` part or as the raw request body, and name it with `title`, which wins over the name of
    /// the uploaded part and has invalid characters replaced before storing. The call is not idempotent: by default a
    /// file of the same title is overwritten as a new version, while `createNewIfExist=true` stores a separate copy
    /// under a title made unique with a numeric suffix; a title held by a file that is locked or open in the editor
    /// cannot be overwritten either, and a second file appears under the same title. Formats listed in
    /// `extsMustConvert` of `GET api/2.0/files/settings` are converted after the response is sent;
    /// `keepConvertStatus=true` keeps that result readable through `GET api/2.0/files/file/{fileId}/checkconversion`,
    /// which otherwise drops it. Files over the single-request size limit or the account's storage quota are refused:
    /// send those through `POST api/2.0/files/{folderId}/upload/create_session`, and use
    /// `POST api/2.0/files/{folderId}/insert` for any other destination.
    /// </remarks>
    /// <summary>Insert a file into My documents</summary>
    /// <path>api/2.0/files/@my/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "The stored file, with the identifier, version and title it was saved under", typeof(FileDto<int>))]
    [SwaggerResponse(403, "Creating a file in the personal section is not allowed for this account")]
    [SwaggerResponse(404, "The caller has no personal section, so there is nothing to store the file in")]
    [HttpPost("@my/insert")]
    public async Task<FileDto<int>> InsertFileToMyFromBody([FromForm][ModelBinder(BinderType = typeof(InsertFileModelBinder))] InsertFileRequestDto inDto)
    {
        return await filesControllerHelper.InsertFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Stream, inDto.Title, inDto.CreateNewIfExist, inDto.KeepConvertStatus);
    }

    /// <remarks>
    /// Uploads a file specified in the request to the "Common" section by single file uploading or standart multipart/form-data method.
    /// </remarks>
    /// <summary>Upload a file to the "Common" section</summary>
    /// <remarks>
    /// <![CDATA[
    ///  You can upload files in two different ways:
    ///  <ol>
    /// <li>Using single file upload. You should set the Content-Type and Content-Disposition headers to specify a file name and content type, and send the file to the request body.</li>
    /// <li>Using standart multipart/form-data method.</li>
    /// </ol>]]>
    /// </remarks>
    /// <path>api/2.0/files/@common/upload</path>
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
    /// Uploads one file into the caller's own My documents section and returns it inside a single-element array; one
    /// request stores exactly one file. The destination takes no identifier: it is resolved from the calling account
    /// and created on first use, while a guest account has none and is answered as missing (404). The body has to be
    /// `multipart/form-data` carrying the file part; a request without it is rejected as invalid, and the stored name
    /// comes from that part, since unlike `POST api/2.0/files/@my/insert` there is no separate title. The call is not
    /// idempotent: by default a file of the same title is overwritten as a new version, while `createNewIfExist=true`
    /// stores a separate copy under a title made unique with a numeric suffix. `storeOriginalFile` is not a
    /// per-request switch: it writes the same account setting as `PUT api/2.0/files/storeoriginal`, which decides
    /// what happens to the formats listed in `extsMustConvert` of `GET api/2.0/files/settings` when they are
    /// converted after the response - false replaces the uploaded file with the converted one, true keeps both;
    /// `keepConvertStatus=true` keeps that conversion result readable through
    /// `GET api/2.0/files/file/{fileId}/checkconversion`. Files over the single-request size limit or the account's
    /// storage quota are refused; send those through `POST api/2.0/files/{folderId}/upload/create_session`.
    /// </remarks>
    /// <summary>Upload a file to My documents</summary>
    /// <path>api/2.0/files/@my/upload</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "An array holding the single uploaded file", typeof(List<FileDto<int>>))]
    [SwaggerResponse(403, "Uploading a file to the personal section is not allowed for this account")]
    [SwaggerResponse(404, "The caller has no personal section, so there is nothing to store the file in")]
    [HttpPost("@my/upload")]
    public async Task<List<FileDto<int>>> UploadFileToMy(UploadRequestDto inDto)
    {
        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderMyAsync, inDto);
    }
}
