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

using ASC.Files.Core.ApiModels.ResponseDto;

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
    HistoryApiHelper historyApiHelper,
    IDistributedCache distributedCache)
    : FilesController<int>(filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        distributedCache)
{
    /// <summary>
    /// Get the list of actions performed on the file with the specified identifier
    /// </summary>
    /// <short>
    /// Get file history
    /// </short>
    /// <param type="System.Int32, System" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of actions performed on the file", typeof(HistoryDto))]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFileHistoryAsync(int fileId)
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
    FileShareDtoHelper fileShareDtoHelper,
    IDistributedCache distributedCache)
    : FilesController<string>(filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        distributedCache);

public abstract class FilesController<T>(FilesControllerHelper filesControllerHelper,
        FileStorageService fileStorageService,
        FileOperationsManager fileOperationsManager,
        FileOperationDtoHelper fileOperationDtoHelper,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        FileShareDtoHelper fileShareDtoHelper,
        IDistributedCache distributedCache)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Change version history</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/history</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Boolean, System" name="start" example="true">Specifies if a conversion operation is started or not</param>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
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
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
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
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/{folderId}/file</path>
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
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <path>api/2.0/files/{folderId}/html</path>
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
    /// <param type="System.Int32, System" method="url" name="folderId" example="1234">Folder ID</param>
    /// <path>api/2.0/files/{folderId}/text</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}</path>
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

    [Tags("Files / Files")]
    [SwaggerResponse(200, "Ok", typeof(FillingFormResultDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/fillresult")]
    public async Task<FillingFormResultDto<T>> GetFillResultAsync(string fillingSessionId)
    {
        var completedFormId = await distributedCache.GetStringAsync(fillingSessionId);

        return await filesControllerHelper.GetFillResultAsync((T)Convert.ChangeType(completedFormId, typeof(T)));
    }

    /// <summary>
    /// Returns a URL to the changes of a file version specified in the request.
    /// </summary>
    /// <short>Get changes URL</short>
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Int32, System" name="version" example="1234">File version</param>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
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
    /// <path>api/2.0/files/file/{fileId}</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/history</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="System.Int32, System" name="version" example="1234">File version</param>
    /// <param type="System.String, System" name="url" example="some text">File version URL</param>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <param type="ASC.Files.Core.ApiModels.RequestDto.UpdateFileRequestDto, ASC.Files.Core" name="inDto">Request parameters for updating a file</param>
    /// <path>api/2.0/files/file/{fileId}</path>
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
    /// <param type="System.Int32, System" method="url" name="fileId" example="1234">File ID</param>
    /// <path>api/2.0/files/{fileId}/update</path>
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
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLinkAsync(T id)
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
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [HttpGet("file/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetLinksAsync(T id)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var totalCount = await fileStorageService.GetPureSharesCountAsync(id, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in fileStorageService.GetPureSharesAsync(id, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <summary>
    /// Sets an external link to a file with the ID specified in the request.
    /// </summary>
    /// <short>Set an external link</short>
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{id}/links</path>
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
    /// <param type="System.Int32, System" method="url" name="id" example="1234">File ID</param>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
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
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/@my/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileMyDocumentsAsync(CreateFileRequestDto<JsonElement> inDto)
    {
        return await filesControllerHelperInternal.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "Common" section</short>
    /// <path>api/2.0/files/@common/html</path>
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
    /// <path>api/2.0/files/@my/html</path>
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
    /// <path>api/2.0/files/@common/text</path>
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
    /// <path>api/2.0/files/@my/text</path>
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
    /// <path>api/2.0/files/thumbnails</path>
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