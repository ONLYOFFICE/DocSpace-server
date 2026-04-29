// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
    /// Creates the session to upload large files in multiple chunks to the folder with the ID specified in the request.
    /// </remarks>
    /// <summary>Chunked upload</summary>
    /// <remarks>
    /// <![CDATA[
    /// Each chunk can have different length but the length should be multiple of <b>512</b> and greater or equal to <b>10 mb</b>. Last chunk can have any size.
    /// After the initial response to the request with the <b>200 OK</b> status, you must get the <em>location</em> field value from the response. Send all your chunks to this location.
    /// Each chunk must be sent in the exact order the chunks appear in the file.
    /// After receiving each chunk, the server will respond with the current information about the upload session if no errors occurred.
    /// When the number of bytes uploaded is equal to the number of bytes you sent in the initial request, the server responds with the <b>201 Created</b> status and sends you information about the uploaded file.
    /// Information about created session which includes:
    /// <ul>
    /// <li><b>id:</b> unique ID of this upload session,</li>
    /// <li><b>created:</b> UTC time when the session was created,</li>
    /// <li><b>expired:</b> UTC time when the session will expire if no chunks are sent before that time,</li>
    /// <li><b>location:</b> URL where you should send your next chunk,</li>
    /// <li><b>bytes_uploaded:</b> number of bytes uploaded for the specific upload ID,</li>
    /// <li><b>bytes_total:</b> total number of bytes which will be uploaded.</li>
    /// </ul>
    /// ]]>
    /// </remarks>
    /// <path>api/2.0/files/{folderId}/upload/create_session</path>
    [Obsolete]
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Information about created session")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
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

    /// <summary>Creates a session for uploading a file to a specific folder in chunks.</summary>
    /// <remarks>
    /// The session allows the user to upload a file in smaller chunks to the folder identified by its ID.
    /// The file information, such as name, size, and additional metadata, must be provided in the request.
    /// This method facilitates large file upload scenarios by enabling chunked file uploads.
    /// </remarks>
    /// <param name="inDto">The request object containing the folder ID and session details, including file name, size, relative path, and additional upload settings.</param>
    /// <returns>A response containing details about the created upload session, such as session ID, expiration information, and upload progress data.</returns>
    [Tags("Files / Operations")]
    [SwaggerResponse(200)]
    [HttpPost("{folderId}/session")]
    public async Task<ChunkedUploadSessionResponse<T>> CreateUploadSessionInFolder(SessionRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);
    }

    /// <summary>Aborts an in-progress file upload session.</summary>
    /// <remarks>
    /// This method allows users to cancel an ongoing upload session identified by the session ID.
    /// Once the session is aborted, the associated resources will be cleaned up, and the session will no longer accept further uploads.
    /// </remarks>
    /// <param name="inDto">A request object containing the session ID of the upload session to be aborted.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [Tags("Files / Operations")]
    [SwaggerResponse(200)]
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

    /// <summary>Resumes an ongoing file upload session for uploading additional chunks of data.</summary>
    /// <remarks>
    /// This method allows continuing an interrupted or partially completed file upload session by uploading subsequent data chunks.
    /// The server will validate each uploaded chunk, update the session state, and respond with the status of the current upload. Once
    /// the total bytes uploaded match the total file size, the file upload process is finalized and related events are triggered.
    /// If the file is newly uploaded, the server responds with a "201 Created" status upon completion. If it overwrites an existing file,
    /// versioning information is updated accordingly. The method also triggers associated webhooks and socket notifications to reflect
    /// the updated file state.
    /// </remarks>
    /// <param name="inDto">
    /// Contains information about the ongoing upload session, including the session ID, the file chunk data, and its size.
    /// </param>
    /// <returns>
    /// A DTO containing information about the current state of the upload session, including the uploaded file's metadata (e.g., file ID, folder ID,
    /// version, title, and additional details), as well as whether the upload process has been completed.
    /// </returns>
    [Tags("Files / Operations")]
    [SwaggerResponse(200)]
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

    /// <summary>Handles the upload of a chunk for an existing upload session.</summary>
    /// <remarks>
    /// This method allows the caller to upload a specific chunk of a file to an ongoing upload session.
    /// The session is identified by the session ID provided in the request. The chunk can be of any size
    /// within the limits allowed during the session initialization. Each chunk must be uploaded in the
    /// correct order for the server to process it appropriately.
    /// The server updates the upload session status and stores the progress information after processing
    /// each chunk. The updated session details are returned in the response.
    /// </remarks>
    /// <param name="inDto">
    /// An object containing the necessary parameters for uploading a chunk, including:
    /// <b>SessionId</b>: The unique identifier for the upload session.
    /// <b>ChunkNumber</b>: The sequence number of the current chunk being uploaded (optional).
    /// <b>File</b>: The file stream for the chunk that is being uploaded.
    /// </param>
    /// <returns>
    /// A response object containing updated session information, including the current progress and
    /// details about the upload session.
    /// </returns>
    [Tags("Files / Operations")]
    [SwaggerResponse(200)]
    [HttpPost("{folderId}/session/{sessionId}/upload")]
    public async Task<ChunkedUploadSessionResponse<T>> UploadAsyncSession(UploadSessionAsyncRequestDto<T> inDto)
    {
        var resumedSession = await fileUploader.UploadChunkAsync<T>(inDto.SessionId, inDto.File.OpenReadStream(), inDto.File.Length, inDto.ChunkNumber);
        await chunkedUploadSessionHolder.StoreSessionAsync(resumedSession);
        return await chunkedUploadSessionHelper.ToResponseObjectAsync(resumedSession);
    }

    /// <summary>Finalize an upload session</summary>
    /// <remarks>
    /// Finalizes the upload session by processing the uploaded file chunks and marking the upload as complete.
    /// This method consolidates chunked uploads into a complete file if required, sends notifications about the upload event,
    /// and performs any additional cleanup or related actions, such as socket updates and webhook publishing.
    /// </remarks>
    /// <returns>An object containing details about the completed upload session, including file metadata and upload status.</returns>
    [Tags("Files / Operations")]
    [SwaggerResponse(200)]
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
    /// Creates a session to edit the existing file with multiple chunks (needed for WebDAV).
    /// </remarks>
    /// <summary>Create the editing session</summary>
    /// <remarks>
    /// <![CDATA[
    /// Information about created session which includes:
    /// <ul>
    /// <li><b>id:</b> unique ID of this upload session,</li>
    /// <li><b>created:</b> UTC time when the session was created,</li>
    /// <li><b>expired:</b> UTC time when the session will expire if no chunks are sent before that time,</li>
    /// <li><b>location:</b> URL where you should send your next chunk,</li>
    /// <li><b>bytes_uploaded:</b> number of bytes uploaded for the specific upload ID,</li>
    /// <li><b>bytes_total:</b> total number of bytes which will be uploaded.</li>
    /// </ul>
    /// ]]>
    /// </remarks>
    /// <path>api/2.0/files/file/{fileId}/edit_session</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Information about created session")]
    [SwaggerResponse(403, "You don't have enough permission to edit the file")]
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
    /// Checks the file uploads to the folder with the ID specified in the request.
    /// </remarks>
    /// <summary>Check file uploads</summary>
    /// <path>api/2.0/files/{folderId}/upload/check</path>
    /// <collection>list</collection>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(List<string>))]
    [HttpPost("{folderId}/upload/check")]
    public async Task<List<string>> CheckUploadAsync(CheckUploadRequestDto<T> model)
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
            throw new Exception(FilesCommonResource.ErrorMessage_UploadToFormRoom);
        }

        var result = new List<string>();

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
    /// Inserts a file specified in the request to the selected folder by single file uploading.
    /// </remarks>
    /// <summary>Insert a file</summary>
    /// <path>api/2.0/files/{folderId}/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPost("{folderId}/insert", Order = 1)]
    public async Task<FileDto<T>> InsertFile(InsertWithFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.InsertFileAsync(inDto.FolderId, inDto.InsertFile.Stream, inDto.InsertFile.Title, inDto.InsertFile.CreateNewIfExist, inDto.InsertFile.KeepConvertStatus);
    }


    /// <remarks>
    /// Uploads a file specified in the request to the selected folder by single file uploading or standart multipart/form-data method.
    /// </remarks>
    /// <summary>Upload a file</summary>
    /// <remarks>
    /// <![CDATA[
    ///  You can upload files in two different ways:
    ///  <ol>
    /// <li>Using single file upload. You should set the Content-Type and Content-Disposition headers to specify a file name and content type, and send the file to the request body.</li>
    /// <li>Using standart multipart/form-data method.</li>
    /// </ol>]]>
    /// </remarks>
    /// <path>api/2.0/files/{folderId}/upload</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "Folder not found")]
    [HttpPost("{folderId}/upload", Order = 1)]
    public async Task<List<FileDto<T>>> UploadFile(UploadWithFolderRequestDto<T> inDto)
    {
        return await filesControllerHelper.UploadFileAsync(inDto.FolderId, inDto.UploadData);
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
    /// Inserts a file specified in the request to the "My documents" section by single file uploading.
    /// </remarks>
    /// <summary>Insert a file to the "My documents" section</summary>
    /// <path>api/2.0/files/@my/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "Folder not found")]
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
    public async Task<List<FileDto<int>>> UploadFileToCommon([ModelBinder(BinderType = typeof(UploadModelBinder))] UploadRequestDto inDto)
    {
        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderCommonAsync, inDto);
    }

    /// <remarks>
    /// Uploads a file specified in the request to the "My documents" section by single file uploading or standart multipart/form-data method.
    /// </remarks>
    /// <summary>Upload a file to the "My documents" section</summary>
    /// <remarks>
    /// <![CDATA[
    ///  You can upload files in two different ways:
    ///  <ol>
    /// <li>Using single file upload. You should set the Content-Type and Content-Disposition headers to specify a file name and content type, and send the file to the request body.</li>
    /// <li>Using standart multipart/form-data method.</li>
    /// </ol>]]>
    /// </remarks>
    /// <path>api/2.0/files/@my/upload</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Uploaded file(s)", typeof(object))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("@my/upload")]
    public async Task<List<FileDto<int>>> UploadFileToMy([ModelBinder(BinderType = typeof(UploadModelBinder))] UploadRequestDto inDto)
    {
        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderMyAsync, inDto);
    }
}
