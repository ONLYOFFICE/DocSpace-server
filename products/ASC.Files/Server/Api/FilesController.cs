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

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class FilesControllerInternal(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper)
    : FilesController<int>(filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper)
{
    /// <summary>
    /// Get the list of actions performed on the file with the specified identifier
    /// </summary>
    /// <short>
    /// Get file history
    /// </short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" name="fileId" example="1234">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.HistoryDto, ASC.Files.Core">List of actions performed on the file</returns>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of actions performed on the file", typeof(HistoryDto))]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetHistoryAsync(int fileId)
    {
        return historyApiHelper.GetFileHistoryAsync(fileId);
    }
}

public class FilesControllerThirdparty(
    FilesControllerHelper filesControllerHelper,
    FileStorageService fileStorageService,
    FileOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper)
    : FilesController<string>(filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper);

public abstract class FilesController<T>(FilesControllerHelper filesControllerHelper,
        FileStorageService fileStorageService,
        FileOperationsManager fileOperationsManager,
        FileOperationDtoHelper fileOperationDtoHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        FileShareDtoHelper fileShareDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Change version history</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.ChangeHistoryRequestDto, ASC.Files.Core" name="inDto">Request parameters for changing version history</param>
    /// <category>Files</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Updated information about file versions</returns>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated information about file versions", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> ChangeHistoryAsync(T fileId, ChangeHistoryRequestDto inDto)
    {
        return filesControllerHelper.ChangeHistoryAsync(fileId, inDto.Version, inDto.ContinueVersion);
    }

    /// <summary>
    /// Checks the conversion status of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get conversion status</short>
    /// <category>Operations</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Boolean, System" name="start" example="true">Specifies if a conversion operation is started or not</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.ConversationResultDto, ASC.Files.Core">Conversion result</returns>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(ConversationResultDto))]
    [HttpGet("file/{fileId}/checkconversion")]
    public async IAsyncEnumerable<ConversationResultDto> CheckConversionAsync(T fileId, bool start)
    {
        await foreach (var r in filesControllerHelper.CheckConversionAsync(new CheckConversionRequestDto<T>
        {
            FileId = fileId,
            StartConvert = start
        }))
        {
            yield return r;
        }
    }

    /// <summary>
    /// Returns a link to download a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file download link</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <returns type="System.String, System">File download link</returns>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    /// <httpMethod>GET</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File download link", typeof(string))]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(T fileId)
    {
        return await filesControllerHelper.GetPresignedUri(fileId);
    }

    /// <summary>
    /// Checks if the PDF file is form or not.
    /// </summary>
    /// <short>Check the PDF file</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <returns type="System.Boolean, System">Boolean value: true - the PDF file is form, false - the PDF file is not a form</returns>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    /// <httpMethod>GET</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true - the PDF file is form, false - the PDF file is not a form", typeof(bool))]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(T fileId)
    {
        return await filesControllerHelper.isFormPDF(fileId);
    }

    /// <summary>
    /// Copies (and converts if possible) an existing file to the specified folder.
    /// </summary>
    /// <short>Copy a file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CopyAsRequestDto{System.Text.Json.JsonElement}, ASC.Files.Core" name="inDto">Request parameters for copying a file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileEntryDto, ASC.Files.Core">Copied file entry information</returns>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Copied file entry information", typeof(FileEntryDto))]
    [HttpPost("file/{fileId}/copyas")]
    public async Task<FileEntryDto> CopyFileAs(T fileId, CopyAsRequestDto<JsonElement> inDto)
    {
        if (inDto.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            return await filesControllerHelper.CopyFileAsAsync(fileId, inDto.DestFolderId.GetInt32(), inDto.DestTitle, inDto.Password, inDto.ToForm);
        }

        if (inDto.DestFolderId.ValueKind == JsonValueKind.String)
        {
            return await filesControllerHelper.CopyFileAsAsync(fileId, inDto.DestFolderId.GetString(), inDto.DestTitle, inDto.Password, inDto.ToForm);
        }

        return null;
    }

    /// <summary>
    /// Creates a new file in the specified folder with the title specified in the request.
    /// </summary>
    /// <short>Create a file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateFileRequestDto{System.Text.Json.JsonElement}, ASC.Files.Core" name="inDto">Request parameters for creating a file</param>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/{folderId}/file</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFileAsync(T folderId, CreateFileRequestDto<JsonElement> inDto)
    {
        return await filesControllerHelper.CreateFileAsync(folderId, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating an HTML file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/{folderId}/html</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFileAsync(T folderId, CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelper.CreateHtmlFileAsync(folderId, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a text (.txt) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a txt file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a text file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/{folderId}/text</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFileAsync(T folderId, CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelper.CreateTextFileAsync(folderId, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Deletes a file with the ID specified in the request.
    /// </summary>
    /// <short>Delete a file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.DeleteRequestDto, ASC.Files.Core" name="inDto">Request parameters for deleting a file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileOperationDto, ASC.Files.Core">List of file operations</returns>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <httpMethod>DELETE</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file operations", typeof(FileOperationDto))]
    [HttpDelete("file/{fileId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFile(T fileId, [FromBody] DeleteRequestDto inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T>(), new List<T> { fileId }, false, !inDto.DeleteAfter, inDto.Immediately);
        
        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Returns a URL to the changes of a file version specified in the request.
    /// </summary>
    /// <short>Get changes URL</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Int32, System" name="version" example="1234">File version</param>
    /// <returns type="ASC.Files.Core.EditHistoryDataDto, ASC.Files.Core">File version history data</returns>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File version history data", typeof(EditHistoryDataDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync(T fileId, int version = 0)
    {
        return await filesControllerHelper.GetEditDiffUrlAsync(fileId, version);
    }

    /// <summary>
    /// Returns the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get version history</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.EditHistoryDto, ASC.Files.Core">Version history data</returns>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data", typeof(EditHistoryDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistoryAsync(T fileId)
    {
        return filesControllerHelper.GetEditHistoryAsync(fileId);
    }

    /// <summary>
    /// Returns the detailed information about a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file information</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Int32, System" name="version">File version</param>
    /// <category>Files</category>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">File information</returns>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File information", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfoAsync(T fileId, int version = -1)
    {
        return await filesControllerHelper.GetFileInfoAsync(fileId, version);
    }


    /// <summary>
    /// Returns the detailed information about all the available file versions with the ID specified in the request.
    /// </summary>
    /// <short>Get file versions</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility</returns>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfoAsync(T fileId)
    {
        return filesControllerHelper.GetFileVersionInfoAsync(fileId);
    }

    /// <summary>
    /// Locks a file with the ID specified in the request.
    /// </summary>
    /// <short>Lock a file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.LockFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for locking a file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Locked file information</returns>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    /// <httpMethod>PUT</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Locked file information", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFileAsync(T fileId, LockFileRequestDto inDto)
    {
        return await filesControllerHelper.LockFileAsync(fileId, inDto.LockFile);
    }

    /// <summary>
    /// Restores a file version specified in the request.
    /// </summary>
    /// <short>Restore a file version</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Int32, System" name="version" example="1234">File version</param>
    /// <param type="System.String, System" name="url" example="some text">File version URL</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.EditHistoryDto, ASC.Files.Core">Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version</returns>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <httpMethod>GET</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version", typeof(EditHistoryDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreVersionAsync(T fileId, int version = 0, string url = null)
    {
        return filesControllerHelper.RestoreVersionAsync(fileId, version, url);
    }

    /// <summary>
    /// Starts a conversion operation of a file with the ID specified in the request.
    /// </summary>
    /// <short>Start file conversion</short>
    /// <category>Operations</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CheckConversionRequestDto, ASC.Files.Core" name="inDto">Request parameters for starting file conversion</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.ConversationResultDto, ASC.Files.Core">Conversion result</returns>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <httpMethod>PUT</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(ConversationResultDto))]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartConversion(T fileId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CheckConversionRequestDto<T> inDto)
    {
        inDto ??= new CheckConversionRequestDto<T>();
        inDto.FileId = fileId;

        return filesControllerHelper.StartConversionAsync(inDto);
    }

    /// <summary>
    /// Updates a comment in a file with the ID specified in the request.
    /// </summary>
    /// <short>Update a comment</short>
    /// <category>Operations</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateCommentRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating a comment</param>
    /// <returns type="System.Object, System">Updated comment</returns>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    /// <httpMethod>PUT</httpMethod>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Updated comment", typeof(object))]
    [HttpPut("file/{fileId}/comment")]
    public async Task<object> UpdateCommentAsync(T fileId, UpdateCommentRequestDto inDto)
    {
        return await filesControllerHelper.UpdateCommentAsync(fileId, inDto.Version, inDto.Comment);
    }

    /// <summary>
    /// Updates the information of the selected file with the parameters specified in the request.
    /// </summary>
    /// <short>Update a file</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating a file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Updated file information</returns>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <httpMethod>PUT</httpMethod>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFileAsync(T fileId, UpdateFileRequestDto inDto)
    {
        return await filesControllerHelper.UpdateFileAsync(fileId, inDto.Title, inDto.LastVersion);
    }

    /// <summary>
    /// Updates the contents of a file with the ID specified in the request.
    /// </summary>
    /// <short>Update file contents</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.FileStreamRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating file contents</param>
    /// <path>api/2.0/files/{fileId}/update</path>
    /// <httpMethod>PUT</httpMethod>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">Updated file information</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [HttpPut("{fileId}/update")]
    public async Task<FileDto<T>> UpdateFileStreamFromFormAsync(T fileId, [FromForm] FileStreamRequestDto inDto)
    {
        return await filesControllerHelper.UpdateFileStreamAsync(filesControllerHelper.GetFileFromRequest(inDto).OpenReadStream(), fileId, inDto.FileExtension, inDto.Encrypted, inDto.Forcesave);
    }

    /// <summary>
    /// Returns the primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Get primary external link</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">File security information</returns>
    /// <path>api/2.0/files/file/{id}/link</path>
    /// <httpMethod>GET</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetPrimaryExternalLinkAsync(T id)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(id, FileEntryType.File);

        return await fileShareDtoHelper.Get(linkAce);
    }

    [Tags("Files / Files")]
    [HttpPut("{fileId}/order")]
    public async Task SetOrder(T fileId, OrderRequestDto inDto)
    {
        await fileStorageService.SetFileOrder(fileId, inDto.Order);
    }

    /// <summary>
    /// Returns the external links of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file external links</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">File security information</returns>
    /// <path>api/2.0/files/file/{fileId}/links</path>
    /// <httpMethod>GET</httpMethod>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [HttpGet("file/{fileId}/links")]
    public async IAsyncEnumerable<FileShareDto> GetLinksAsync(T fileId)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var totalCount = await fileStorageService.GetPureSharesCountAsync(fileId, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in fileStorageService.GetPureSharesAsync(fileId, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <summary>
    /// Sets an external link to a file with the ID specified in the request.
    /// </summary>
    /// <short>Set an external link</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.FileLinkRequestDto, ASC.Files.Core" name="inDto">External link request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileShareDto, ASC.Files.Core">File security information</returns>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <httpMethod>PUT</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [HttpPut("file/{id}/links")]
    public async Task<FileShareDto> SetExternalLinkAsync(T id, FileLinkRequestDto inDto)
    {
        var linkAce = await fileStorageService.SetExternalLinkAsync(id, FileEntryType.File, inDto.LinkId, null, inDto.Access, requiredAuth: inDto.Internal, 
            primary: inDto.Primary, expirationDate: inDto.ExpirationDate);

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <summary>
    /// Saves a file with the identifier specified in the request as a PDF document
    /// </summary>
    /// <short>Save as pdf</short>
    /// <category>Files</category>
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.SaveAsPdfRequestDto, ASC.Files.Core" name="inDto">Request parameters</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveAsPdf(T id, SaveAsPdfRequestDto<T> inDto)
    {
        return await filesControllerHelper.SaveAsPdf(id, inDto.FolderId, inDto.Title);
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
    /// <summary>
    /// Creates a new file in the "My documents" section with the title specified in the request.
    /// </summary>
    /// <short>Create a file in the "My documents" section</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateFileRequestDto{System.Text.Json.JsonElement}, ASC.Files.Core" name="inDto">Request parameters for creating a file</param>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/@my/file</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileAsync(CreateFileRequestDto<JsonElement> inDto)
    {
        return await filesControllerHelperInternal.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "Common" section</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating an HTML file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/@common/html</path>
    /// <httpMethod>POST</httpMethod>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommonAsync(CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "My documents" section</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating an HTML file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/@my/html</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyAsync(CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a text (.txt) file in the "Common" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a text file in the "Common" section</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a text file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/@common/text</path>
    /// <httpMethod>POST</httpMethod>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@common/text")]
    public async Task<FileDto<int>> CreateTextFileInCommonAsync(CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a text (.txt) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a text file in the "My documents" section</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.CreateTextOrHtmlFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for creating a text file</param>
    /// <returns type="ASC.Files.Core.ApiModels.ResponseDto.FileDto, ASC.Files.Core">New file information</returns>
    /// <path>api/2.0/files/@my/text</path>
    /// <httpMethod>POST</httpMethod>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyAsync(CreateTextOrHtmlFileRequestDto inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates thumbnails for the files with the IDs specified in the request.
    /// </summary>
    /// <short>Create thumbnails</short>
    /// <category>Files</category>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.BaseBatchRequestDto, ASC.Files.Core" name="inDto">Base batch request parameters</param>
    /// <returns type="System.Text.Json.JsonElement, System.Text.Json">List of file IDs</returns>
    /// <path>api/2.0/files/thumbnails</path>
    /// <httpMethod>POST</httpMethod>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file IDs", typeof(JsonElement))]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(BaseBatchRequestDto inDto)
    {
        return await fileStorageService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}