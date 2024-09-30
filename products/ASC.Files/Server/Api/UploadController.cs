// (c) Copyright Ascensio System SIA 2009-2024
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
public class UploadControllerInternal(UploadControllerHelper filesControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : UploadController<int>(filesControllerHelper,
        folderDtoHelper,
    fileDtoHelper);

public class UploadControllerThirdparty(UploadControllerHelper filesControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : UploadController<string>(filesControllerHelper, folderDtoHelper, fileDtoHelper);

public abstract class UploadController<T>(UploadControllerHelper filesControllerHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
    {
    /// <summary>
    /// Creates a session to upload large files in multiple chunks to the folder with the ID specified in the request.
    /// </summary>
    /// <short>Chunked upload</short>
    /// <remarks>
    /// <![CDATA[
    /// Each chunk can have different length but the length should be multiple of <b>512</b> and greater or equal to <b>10 mb</b>. Last chunk can have any size.
    /// After the initial response to the request with the <b>200 OK</b> status, you must get the <em>location</em> field value from the response. Send all your chunks to this location.
    /// Each chunk must be sent in the exact order the chunks appear in the file.
    /// After receiving each chunk, the server will respond with the current information about the upload session if no errors occurred.
    /// When the number of bytes uploaded is equal to the number of bytes you sent in the initial request, the server responds with the <b>201 Created</b> status and sends you information about the uploaded file.
    /// ]]>
    /// </remarks>
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
    /// <path>api/2.0/files/{folderId}/upload/create_session</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Information about created session", typeof(object))]
    [HttpPost("{folderId}/upload/create_session")]
    public async Task<object> CreateUploadSessionAsync(SessionRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateUploadSessionAsync(inDto.FolderId, inDto.Session.FileName, inDto.Session.FileSize, inDto.Session.RelativePath, inDto.Session.Encrypted, inDto.Session.CreateOn, inDto.Session.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a session to edit the existing file with multiple chunks (needed for WebDAV).
    /// </summary>
    /// <short>Create the editing session</short>
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
    /// <path>api/2.0/files/file/{fileId}/edit_session</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Information about created session", typeof(object))]
    [HttpPost("file/{fileId}/edit_session")]
    public async Task<object> CreateEditSession(CreateEditSessionRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateEditSessionAsync(inDto.FileId, inDto.FileSize);
    }

    /// <summary>
    /// Checks upload
    /// </summary>
    /// <path>api/2.0/files/{folderId}/upload/check</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(List<string>))]
    [HttpPost("{folderId}/upload/check")]
    public Task<List<string>> CheckUploadAsync(CheckUploadRequestDto<T> model)
    {
        return filesControllerHelper.CheckUploadAsync(model.FolderId, model.Check.FilesTitle);
    }

    /// <summary>
    /// Inserts a file specified in the request to the selected folder by single file uploading.
    /// </summary>
    /// <short>Insert a file</short>
    /// <path>api/2.0/files/{folderId}/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [HttpPost("{folderId}/insert", Order = 1)]
    public async Task<FileDto<T>> InsertFileAsync(InsertWithFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.InsertFileAsync(inDto.FolderId, inDto.InsertFile.Stream, inDto.InsertFile.Title, inDto.InsertFile.CreateNewIfExist, inDto.InsertFile.KeepConvertStatus);
    }


    /// <summary>
    /// Uploads a file specified in the request to the selected folder by single file uploading or standart multipart/form-data method.
    /// </summary>
    /// <short>Upload a file</short>
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
    [HttpPost("{folderId}/upload", Order = 1)]
    public async Task<object> UploadFileAsync(UploadWithFolderRequestDto<T> inDto)
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
    /// <summary>
    /// Inserts a file specified in the request to the "Common" section by single file uploading.
    /// </summary>
    /// <short>Insert a file to the "Common" section</short>
    /// <path>api/2.0/files/@common/insert</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [HttpPost("@common/insert")]
    public async Task<FileDto<int>> InsertFileToCommonFromBodyAsync([FromForm][ModelBinder(BinderType = typeof(InsertFileModelBinder))] InsertFileRequestDto inDto)
    {
        return await filesControllerHelper.InsertFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Stream, inDto.Title, inDto.CreateNewIfExist, inDto.KeepConvertStatus);
    }

    /// <summary>
    /// Inserts a file specified in the request to the "My documents" section by single file uploading.
    /// </summary>
    /// <short>Insert a file to the "My documents" section</short>
    /// <path>api/2.0/files/@my/insert</path>
    [Tags("Files / Folders")]
    [SwaggerResponse(200, "Inserted file", typeof(FileDto<int>))]
    [HttpPost("@my/insert")]
    public async Task<FileDto<int>> InsertFileToMyFromBodyAsync([FromForm][ModelBinder(BinderType = typeof(InsertFileModelBinder))] InsertFileRequestDto inDto)
    {
        return await filesControllerHelper.InsertFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Stream, inDto.Title, inDto.CreateNewIfExist, inDto.KeepConvertStatus);
    }

    /// <summary>
    /// Uploads a file specified in the request to the "Common" section by single file uploading or standart multipart/form-data method.
    /// </summary>
    /// <short>Upload a file to the "Common" section</short>
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
    [HttpPost("@common/upload")]
    public async Task<object> UploadFileToCommonAsync([ModelBinder(BinderType = typeof(UploadModelBinder))] UploadRequestDto inDto)
    {
        inDto.CreateNewIfExist = false;

        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderCommonAsync, inDto);
    }

    /// <summary>
    /// Uploads a file specified in the request to the "My documents" section by single file uploading or standart multipart/form-data method.
    /// </summary>
    /// <short>Upload a file to the "My documents" section</short>
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
    [HttpPost("@my/upload")]
    public async Task<object> UploadFileToMyAsync([ModelBinder(BinderType = typeof(UploadModelBinder))] UploadRequestDto inDto)
    {
        inDto.CreateNewIfExist = false;

        return await filesControllerHelper.UploadFileAsync(await globalFolderHelper.FolderMyAsync, inDto);
    }
}
