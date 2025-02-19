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
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("getHistory")]
    [EndpointSummary("Get file history")]
    [EndpointDescription("Get the list of actions performed on the file with the specified identifier")]
    [OpenApiResponse(typeof(IAsyncEnumerable<HistoryDto>), 200, "List of actions performed on the file")]
    [OpenApiResponse(403, "You don't have enough permission to perform the operation")]
    [OpenApiResponse(404, "The required file was not found")]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetHistoryAsync(HistoryRequestDto inDto)
    {
        return historyApiHelper.GetFileHistoryAsync(inDto.FileId, inDto.FromDate, inDto.ToDate);
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
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("changeHistory")]
    [EndpointSummary("Change version history")]
    [EndpointDescription("Changes the version history of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<FileDto<int>>), 200, "Updated information about file versions")]
    [OpenApiResponse(403, "You do not have enough permissions to edit the file")]
    [HttpPut("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> ChangeHistoryAsync(ChangeHistoryRequestDto<T> inDto)
    {
        return filesControllerHelper.ChangeHistoryAsync(inDto.FileId, inDto.File.Version, inDto.File.ContinueVersion);
    }

    /// <summary>
    /// Checks the conversion status of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get conversion status</short>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [EndpointName("checkConversion")]
    [EndpointSummary("Get conversion status")]
    [EndpointDescription("Checks the conversion status of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<ConversationResultDto>), 200, "Conversion result")]
    [HttpGet("file/{fileId}/checkconversion")]
    public async IAsyncEnumerable<ConversationResultDto> CheckConversionAsync(CheckConversionStatusRequestDto<T> inDto)
    {
        await foreach (var r in filesControllerHelper.CheckConversionAsync(new CheckConversionRequestDto<T>
        {
            FileId = inDto.FileId,
            StartConvert = inDto.Start
        }))
        {
            yield return r;
        }
    }

    /// <summary>
    /// Returns a link to download a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file download link</short>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    [Tags("Files / Files")]
    [EndpointName("getPresignedUri")]
    [EndpointSummary("Get file download link")]
    [EndpointDescription("Returns a link to download a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(string), 200, "File download link")]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetPresignedUri(inDto.FileId);
    }

    /// <summary>
    /// Checks if the PDF file is form or not.
    /// </summary>
    /// <short>Check the PDF file</short>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    [Tags("Files / Files")]
    [EndpointName("isFormPDF")]
    [EndpointSummary("Check if PDF file is form")]
    [EndpointDescription("Checks if the PDF file is form or not.")]
    [OpenApiResponse(typeof(bool), 200, "Boolean value: true - the PDF file is form, false - the PDF file is not a form")]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.isFormPDF(inDto.FileId);
    }

    /// <summary>
    /// Copies (and converts if possible) an existing file to the specified folder.
    /// </summary>
    /// <short>Copy a file</short>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    [Tags("Files / Files")]
    [EndpointName("copyFileAs")]
    [EndpointSummary("Copy a file")]
    [EndpointDescription("Copies (and converts if possible) an existing file to the specified folder.")]
    [OpenApiResponse(typeof(FileEntryDto), 200, "Copied file entry information")]
    [OpenApiResponse(400, "No file id or folder id toFolderId determine provider")]
    [OpenApiResponse(403, "You don't have enough permission to create")]
    [OpenApiResponse(404, "File not found")]
    [HttpPost("file/{fileId}/copyas")]
    public async Task<FileEntryDto> CopyFileAs(CopyAsRequestDto<T> inDto)
    {
        if (inDto.File.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            return await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetInt32(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm);
        }

        if (inDto.File.DestFolderId.ValueKind == JsonValueKind.String)
        {
            return await filesControllerHelper.CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetString(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm);
        }

        return null;
    }

    /// <summary>
    /// Creates a new file in the specified folder with the title specified in the request.
    /// </summary>
    /// <short>Create a file</short>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/{folderId}/file</path>
    [Tags("Files / Files")]
    [EndpointName("createFile")]
    [EndpointSummary("Create a file")]
    [EndpointDescription("Creates a new file in the specified folder with the title specified in the request.\n\n **Note**: If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [OpenApiResponse(403, "You don't have enough permission to create")]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFileAsync(CreateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.TemplateId, inDto.File.FormId, inDto.File.EnableExternalExt);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file</short>
    /// <path>api/2.0/files/{folderId}/html</path>
    [Tags("Files / Files")]
    [EndpointName("createHtmlFile")]
    [EndpointSummary("Create an HTML file")]
    [EndpointDescription("Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [OpenApiResponse(403, "You don't have enough permission to create")]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFileAsync(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateHtmlFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a text (.txt) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a txt file</short>
    /// <path>api/2.0/files/{folderId}/text</path>
    [Tags("Files / Files")]
    [EndpointName("createTextFile")]
    [EndpointSummary("Create a text file")]
    [EndpointDescription("Creates a text (.txt) file in the selected folder with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFileAsync(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateTextFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <summary>
    /// Deletes a file with the ID specified in the request.
    /// </summary>
    /// <short>Delete a file</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("deleteFile")]
    [EndpointSummary("Delete a file")]
    [EndpointDescription("Deletes a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<FileOperationDto>), 200, "List of file operations")]
    [HttpDelete("file/{fileId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFile(DeleteRequestDto<T> inDto)
    {
        await fileOperationsManager.PublishDelete(new List<T>(), new List<T> { inDto.FileId }, false, !inDto.File.DeleteAfter, inDto.File.Immediately);
        
        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Gets fill result
    /// </summary>
    /// <path>api/2.0/files/file/fillresult</path>
    [Tags("Files / Files")]
    [EndpointName("getFillResult")]
    [EndpointSummary("Get fill result")]
    [EndpointDescription("Gets the fill result for a form filling session.")]
    [OpenApiResponse(typeof(FillingFormResultDto<int>), 200, "Ok")]
    [AllowAnonymous]
    [HttpGet("file/fillresult")]
    public async Task<FillingFormResultDto<T>> GetFillResultAsync(GetFillResulteRequestDto inDto)
    {
        var completedFormId = await distributedCache.GetStringAsync(inDto.FillingSessionId);

        return await filesControllerHelper.GetFillResultAsync((T)Convert.ChangeType(completedFormId, typeof(T)));
    }

    /// <summary>
    /// Returns a URL to the changes of a file version specified in the request.
    /// </summary>
    /// <short>Get changes URL</short>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [EndpointName("getEditDiffUrl")]
    [EndpointSummary("Get changes URL")]
    [EndpointDescription("Returns a URL to the changes of a file version specified in the request.")]
    [OpenApiResponse(typeof(EditHistoryDataDto), 200, "File version history data")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync(EditDiffUrlRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetEditDiffUrlAsync(inDto.FileId, inDto.Version);
    }

    /// <summary>
    /// Returns the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get version history</short>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("getEditHistory")]
    [EndpointSummary("Get version history")]
    [EndpointDescription("Returns the version history of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<EditHistoryDto>), 200, "Version history data")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistoryAsync(FileIdRequestDto<T> inDto)
    {
        return filesControllerHelper.GetEditHistoryAsync(inDto.FileId);
    }

    /// <summary>
    /// Returns the detailed information about a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file information</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [EndpointName("getFileInfo")]
    [EndpointSummary("Get file information")]
    [EndpointDescription("Returns the detailed information about a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "File information")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfoAsync(FileInfoRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetFileInfoAsync(inDto.FileId, inDto.Version);
    }

    /// <summary>
    /// Returns the detailed information about all the available file versions with the ID specified in the request.
    /// </summary>
    /// <short>Get file versions</short>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [EndpointName("getFileVersionInfo")]
    [EndpointSummary("Get file versions")]
    [EndpointDescription("Returns the detailed information about all the available file versions with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<FileDto<int>>), 200, "Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfoAsync(FileIdRequestDto<T> inDto)
    {
        return filesControllerHelper.GetFileVersionInfoAsync(inDto.FileId);
    }

    /// <summary>
    /// Locks a file with the ID specified in the request.
    /// </summary>
    /// <short>Lock a file</short>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    [Tags("Files / Files")]
    [EndpointName("lockFile")]
    [EndpointSummary("Lock a file")]
    [EndpointDescription("Locks a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "Locked file information")]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFileAsync(LockFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.LockFileAsync(inDto.FileId, inDto.File.LockFile);
    }

    /// <summary>
    /// Restores a file version specified in the request.
    /// </summary>
    /// <short>Restore a file version</short>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("restoreVersion")]
    [EndpointSummary("Restore a file version")]
    [EndpointDescription("Restores a file version specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<EditHistoryDto>), 200, "Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version")]
    [OpenApiResponse(400, "No file id or folder id toFolderId determine provider")]
    [OpenApiResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreVersionAsync(RestoreVersionRequestDto<T> inDto)
    {
        return filesControllerHelper.RestoreVersionAsync(inDto.FileId, inDto.Version, inDto.Url);
    }

    /// <summary>
    /// Starts a conversion operation of a file with the ID specified in the request.
    /// </summary>
    /// <short>Start file conversion</short>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [EndpointName("startConversion")]
    [EndpointSummary("Start file conversion")]
    [EndpointDescription("Starts a conversion operation of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<ConversationResultDto>), 200, "Conversion result")]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartConversion(StartConversionRequestDto<T> inDto)
    {
        inDto.CheckConversion ??= new CheckConversionRequestDto<T>();
        inDto.CheckConversion.FileId = inDto.FileId;

        return filesControllerHelper.StartConversionAsync(inDto.CheckConversion);
    }

    /// <summary>
    /// Updates a comment in a file with the ID specified in the request.
    /// </summary>
    /// <short>Update a comment</short>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    [Tags("Files / Operations")]
    [EndpointName("updateComment")]
    [EndpointSummary("Update a comment")]
    [EndpointDescription("Updates a comment in a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(object), 200, "Updated comment")]
    [HttpPut("file/{fileId}/comment")]
    public async Task<object> UpdateCommentAsync(UpdateCommentRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateCommentAsync(inDto.FileId, inDto.File.Version, inDto.File.Comment);
    }

    /// <summary>
    /// Updates the information of the selected file with the parameters specified in the request.
    /// </summary>
    /// <short>Update a file</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [EndpointName("updateFile")]
    [EndpointSummary("Update a file")]
    [EndpointDescription("Updates the information of the selected file with the parameters specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "Updated file information")]
    [OpenApiResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFileAsync(UpdateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateFileAsync(inDto.FileId, inDto.File.Title, inDto.File.LastVersion);
    }

    /// <summary>
    /// Updates the contents of a file with the ID specified in the request.
    /// </summary>
    /// <short>Update file contents</short>
    /// <path>api/2.0/files/{fileId}/update</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [EndpointName("updateFileStreamFromForm")]
    [EndpointSummary("Update file contents")]
    [EndpointDescription("Updates the contents of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "Updated file information")]
    [OpenApiResponse(403, "You do not have enough permissions to edit the file")]
    [OpenApiResponse(404, "File not found")]
    [HttpPut("{fileId}/update")]
    public async Task<FileDto<T>> UpdateFileStreamFromFormAsync(FileStreamRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateFileStreamAsync(filesControllerHelper.GetFileFromRequest(inDto).OpenReadStream(), inDto.FileId, inDto.FileExtension, inDto.Encrypted, inDto.Forcesave);
    }

    /// <summary>
    /// Creates a primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Create primary external link</short>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [EndpointName("createPrimaryExternalLink")]
    [EndpointSummary("Create primary external link")]
    [EndpointDescription("Creates a primary external link by the identifier specified in the request.")]
    [OpenApiResponse(typeof(FileShareDto), 200, "File security information")]
    [OpenApiResponse(404, "Not Found")]
    [HttpPost("file/{id}/link")]
    public async Task<FileShareDto> CreatePrimaryExternalLinkAsync(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File, inDto.File.Access, expirationDate: inDto.File.ExpirationDate, requiredAuth: inDto.File.Internal, allowUnlimitedDate: true);
        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Returns the primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Get primary external link</short>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [EndpointName("getFilePrimaryExternalLink")]
    [EndpointSummary("Get primary external link")]
    [EndpointDescription("Returns the primary external link by the identifier specified in the request.")]
    [OpenApiResponse(typeof(FileShareDto), 200, "File security information")]
    [OpenApiResponse(404, "Not Found")]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLinkAsync(FilePrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Sets order of a file with ID specified in the request
    /// </summary>
    /// <path>api/2.0/files/{fileId}/order</path>
    [Tags("Files / Files")]
    [EndpointName("setOrderFile")]
    [EndpointSummary("Set order of a file")]
    [EndpointDescription("Sets order of a file with ID specified in the request")]
    [OpenApiResponse(200, "Order is set")]
    [OpenApiResponse(403, "You don't have enough permission to perform the operation")]
    [OpenApiResponse(404, "Not Found")]
    [HttpPut("{fileId}/order")]
    public async Task SetOrderFile(OrderFileRequestDto<T> inDto)
    {
        await fileStorageService.SetFileOrder(inDto.FileId, inDto.Order.Order);
    }

    /// <summary>
    /// Sets order
    /// </summary>
    /// <path>api/2.0/files/order</path>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Tags("Files / Files")]
    [EndpointName("setOrder")]
    [EndpointSummary("Set order")]
    [EndpointDescription("Sets order")]
    [OpenApiResponse(200, "Order is set")]
    [HttpPut("order")]
    public async Task SetOrder(OrdersRequestDto<T> inDto)
    {
        await fileStorageService.SetOrderAsync(inDto.Items);
    }

    /// <summary>
    /// Returns the external links of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file external links</short>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [EndpointName("getLinks")]
    [EndpointSummary("Get file external links")]
    [EndpointDescription("Returns the external links of a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(IAsyncEnumerable<FileShareDto>), 200, "File security information")]
    [HttpGet("file/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetLinksAsync(FilePrimaryIdRequestDto<T> inDto)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var totalCount = await fileStorageService.GetPureSharesCountAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in fileStorageService.GetPureSharesAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
        {
            yield return await fileShareDtoHelper.Get(ace);
        }
    }

    /// <summary>
    /// Sets an external link to a file with the ID specified in the request.
    /// </summary>
    /// <short>Set an external link</short>
    /// <path>api/2.0/files/file/{id}/links</path>
    [Tags("Files / Files")]
    [EndpointName("setExternalLink")]
    [EndpointSummary("Set an external link")]
    [EndpointDescription("Sets an external link to a file with the ID specified in the request.")]
    [OpenApiResponse(typeof(FileShareDto), 200, "File security information")]
    [HttpPut("file/{id}/links")]
    public async Task<FileShareDto> SetExternalLinkAsync(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.SetExternalLinkAsync(inDto.Id, FileEntryType.File, inDto.File.LinkId, null, inDto.File.Access, requiredAuth: inDto.File.Internal, 
            primary: inDto.File.Primary, expirationDate: inDto.File.ExpirationDate);

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <summary>
    /// Saves a file with the identifier specified in the request as a PDF document
    /// </summary>
    /// <short>Save as pdf</short>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    [Tags("Files / Files")]
    [EndpointName("saveAsPdf")]
    [EndpointSummary("Save as pdf")]
    [EndpointDescription("Saves a file with the identifier specified in the request as a PDF document")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [OpenApiResponse(404, "File not found")]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveAsPdf(SaveAsPdfRequestDto<T> inDto)
    {
        return await filesControllerHelper.SaveAsPdf(inDto.Id, inDto.File.FolderId, inDto.File.Title);
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
    [EndpointName("createFileMyDocuments")]
    [EndpointSummary("Create a file in the 'My documents' section")]
    [EndpointDescription("Creates a new file in the 'My documents' section with the title specified in the request.\n\n **Note**: If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileMyDocumentsAsync(CreateFile<JsonElement> inDto)
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
    [EndpointName("createHtmlFileInCommon")]
    [EndpointSummary("Create an HTML file in the 'Common' section")]
    [EndpointDescription("Creates an HTML (.html) file in the 'Common' section with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [OpenApiResponse(403, "You don't have enough permission to create")]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommonAsync(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "My documents" section</short>
    /// <path>api/2.0/files/@my/html</path>
    [Tags("Files / Files")]
    [EndpointName("createHtmlFileInMy")]
    [EndpointSummary("Create an HTML file in the 'My documents' section")]
    [EndpointDescription("Creates an HTML (.html) file in the 'My documents' section with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [OpenApiResponse(403, "You don't have enough permission to create")]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyAsync(CreateTextOrHtmlFile inDto)
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
    [EndpointName("createTextFileInCommon")]
    [EndpointSummary("Create a text file in the 'Common' section")]
    [EndpointDescription("Creates a text (.txt) file in the 'Common' section with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [HttpPost("@common/text")]
    public async Task<FileDto<int>> CreateTextFileInCommonAsync(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <summary>
    /// Creates a text (.txt) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a text file in the "My documents" section</short>
    /// <path>api/2.0/files/@my/text</path>
    [Tags("Files / Files")]
    [EndpointName("createTextFileInMy")]
    [EndpointSummary("Create a text file in the 'My documents' section")]
    [EndpointDescription("Creates a text (.txt) file in the 'My documents' section with the title and contents specified in the request.")]
    [OpenApiResponse(typeof(FileDto<int>), 200, "New file information")]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyAsync(CreateTextOrHtmlFile inDto)
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
    [EndpointName("createThumbnails")]
    [EndpointSummary("Create thumbnails")]
    [EndpointDescription("Creates thumbnails for the files with the IDs specified in the request.")]
    [OpenApiResponse(typeof(IEnumerable<JsonElement>), 200, "List of file IDs")]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(BaseBatchRequestDto inDto)
    {
        return await fileStorageService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}