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
    EditHistoryMapper editHistoryMapper)
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
        editHistoryMapper)
{
    /// <remarks>
    /// Returns the list of actions performed on the file with the specified identifier.
    /// </remarks>
    /// <summary>
    /// Get file history
    /// </summary>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of actions performed on the file", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The required file was not found")]
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
    EditHistoryMapper editHistoryMapper)
    : FilesController<string>(filesControllerHelper,
        fileStorageService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        editHistoryMapper);

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
    EditHistoryMapper editHistoryMapper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Changes the version history of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Change version history</summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated information about file versions", typeof(IAsyncEnumerable<FileDto<int>>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpPut("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> ChangeVersionHistory(ChangeHistoryRequestDto<T> inDto)
    {
        return filesControllerHelper.ChangeHistoryAsync(inDto.FileId, inDto.File.Version, inDto.File.ContinueVersion);
    }

    /// <remarks>
    /// Checks the conversion status of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get conversion status</summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(IAsyncEnumerable<ConversationResultDto>))]
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
    /// Returns a pre-signed URL to download a file with the specified ID.
    /// This temporary link provides secure access to the file.
    /// </remarks>
    /// <summary>Get file download link</summary>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File download link", typeof(string))]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetPresignedUri(inDto.FileId);
    }

    /// <remarks>
    /// Checks if the PDF file is a form or not.
    /// </remarks>
    /// <summary>Check the PDF file</summary>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true - the PDF file is form, false - the PDF file is not a form", typeof(bool))]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(FileIdRequestDto<T> inDto)
    {
        return await filesControllerHelper.isFormPDF(inDto.FileId);
    }

    /// <remarks>
    /// Copies (and converts if possible) an existing file to the specified folder.
    /// </remarks>
    /// <summary>Copy a file</summary>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Copied file entry information", typeof(FileEntryBaseDto))]
    [SwaggerResponse(400, "No file id or folder id toFolderId determine provider")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "File not found")]
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
    /// Creates a new file in the specified folder with the title specified in the request.
    /// </remarks>
    /// <summary>Create a file</summary>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/{folderId}/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFile(CreateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.TemplateId, inDto.File.FormId, inDto.File.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create an HTML file</summary>
    /// <path>api/2.0/files/{folderId}/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateHtmlFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text (.txt) file in the selected folder with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create a text file</summary>
    /// <path>api/2.0/files/{folderId}/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFile(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.CreateTextFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
    }

    /// <remarks>
    /// Deletes a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Delete a file</summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file operations", typeof(IAsyncEnumerable<FileOperationDto>))]
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
    /// Retrieves the result of a form-filling session.
    /// </remarks>
    /// <summary>
    /// Get form-filling result
    /// </summary>
    /// <path>api/2.0/files/file/fillresult</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Ok", typeof(FillingFormResultDto<int>))]
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
    /// Returns a URL to the changes of a file version specified in the request.
    /// </remarks>
    /// <summary>Get changes URL</summary>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File version history data", typeof(EditHistoryDataDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrl(EditDiffUrlRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetEditDiffUrlAsync(inDto.FileId, inDto.Version);
    }

    /// <remarks>
    /// Returns the version history of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get version history</summary>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistory(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetEditHistoryAsync(inDto.FileId).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Returns the detailed information about a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get file information</summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File information", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfo(FileInfoRequestDto<T> inDto)
    {
        return await filesControllerHelper.GetFileInfoAsync(inDto.FileId, inDto.Version);
    }


    /// <remarks>
    /// Returns the detailed information about all the available file versions with the ID specified in the request.
    /// </remarks>
    /// <summary>Get file versions</summary>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility", typeof(IAsyncEnumerable<FileDto<int>>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfo(FileIdRequestDto<T> inDto)
    {
        return filesControllerHelper.GetFileVersionInfoAsync(inDto.FileId);
    }

    /// <remarks>
    /// Locks a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Lock a file</summary>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Locked file information", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFile(LockFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.LockFileAsync(inDto.FileId, inDto.File.LockFile);
    }

    /// <remarks>
    /// Sets the Custom Filter editing mode to a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Set the Custom Filter editing mode</summary>
    /// <path>api/2.0/files/file/{fileId}/customfilter</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File information", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/customfilter")]
    public async Task<FileDto<T>> SetCustomFilterTag(FileCustomFilterRequestDto<T> inDto)
    {
        var result = await fileStorageService.SetCustomFilterTagAsync(inDto.FileId, inDto.Parameters.Enabled);

        return await _fileDtoHelper.GetAsync(result);
    }

    /// <remarks>
    /// Restores a file version specified in the request.
    /// </remarks>
    /// <summary>Restore a file version</summary>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [SwaggerResponse(400, "No file id or folder id toFolderId determine provider")]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpPost("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreFileVersion(RestoreVersionRequestDto<T> inDto)
    {
        return fileStorageService.RestoreVersionAsync(inDto.FileId, inDto.Version, inDto.Url).Select(editHistoryMapper.MapToDto);
    }

    /// <remarks>
    /// Starts a conversion operation of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Start file conversion</summary>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartFileConversion(StartConversionRequestDto<T> inDto)
    {
        inDto.CheckConversion ??= new CheckConversionRequestDto<T>();
        inDto.CheckConversion.FileId = inDto.FileId;

        return filesControllerHelper.StartConversionAsync(inDto.CheckConversion);
    }

    /// <remarks>
    /// Updates a comment in a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Update a comment</summary>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Updated comment", typeof(string))]
    [HttpPut("file/{fileId}/comment")]
    public async Task<string> UpdateFileComment(UpdateCommentRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateCommentAsync(inDto.FileId, inDto.File.Version, inDto.File.Comment);
    }

    /// <remarks>
    /// Updates the information of the selected file with the parameters specified in the request.
    /// </remarks>
    /// <summary>Update a file</summary>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFile(UpdateFileRequestDto<T> inDto)
    {
        return await filesControllerHelper.UpdateFileAsync(inDto.FileId, inDto.File.Title, inDto.File.LastVersion);
    }

    /// <remarks>
    /// Updates the contents of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Update file contents</summary>
    /// <path>api/2.0/files/{fileId}/update</path>
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
    /// Creates a primary external link by the identifier specified in the request.
    /// </remarks>
    /// <summary>Create primary external link</summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
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
            password: inDto.File.Password);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Returns the primary external link by the identifier specified in the request.
    /// </remarks>
    /// <summary>Get primary external link</summary>
    /// <path>api/2.0/files/file/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLink(FilePrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await fileStorageService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File, allowUnlimitedDate: true);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <remarks>
    /// Returns the external links of a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Get file external links</summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(IAsyncEnumerable<FileShareDto>))]
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
    /// Sets an external link to a file with the ID specified in the request.
    /// </remarks>
    /// <summary>Set an external link</summary>
    /// <path>api/2.0/files/file/{id}/links</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
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
    /// Sets the order of the file with the ID specified in the request.
    /// </remarks>
    /// <summary>
    /// Set file order
    /// </summary>
    /// <path>api/2.0/files/{fileId}/order</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Not Found")]
    [HttpPut("{fileId}/order")]
    public async Task<FileDto<T>> SetFileOrder(OrderFileRequestDto<T> inDto)
    {
        var file = await fileStorageService.SetFileOrder(inDto.FileId, inDto.Order.Order);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Sets the order of the files specified in the request.
    /// </remarks>
    /// <summary>
    /// Set order of files
    /// </summary>
    /// <path>api/2.0/files/order</path>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file entries information", typeof(IAsyncEnumerable<FileEntryDto<int>>))]
    [HttpPut("order")]
    public IAsyncEnumerable<FileEntryDto<T>> SetFilesOrder(OrdersRequestDto<T> inDto)
    {
        return fileStorageService.SetOrderAsync(inDto.Items).Select<FileEntry<T>, FileEntryDto<T>>(
            async (e, _) => e.FileEntryType == FileEntryType.Folder ?
                await _folderDtoHelper.GetAsync(e as Folder<T>) :
                await _fileDtoHelper.GetAsync(e as File<T>));
    }

    /// <remarks>
    /// Saves a file with the identifier specified in the request as a PDF document.
    /// </remarks>
    /// <summary>Save a file as PDF</summary>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveFileAsPdf(SaveAsPdfRequestDto<T> inDto)
    {
        return await filesControllerHelper.SaveAsPdf(inDto.Id, inDto.File.FolderId, inDto.File.Title);
    }

    /// <remarks>
    /// Saves the form role mapping.
    /// </remarks>
    /// <summary>Save form role mapping</summary>
    /// <path>api/2.0/files/file/{fileId}/formrolemapping</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated information about form role mappings")]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpPost("file/{fileId}/formrolemapping")]
    public async Task SaveFormRoleMapping(SaveFormRoleMappingDto<T> inDto)
    {
        await fileStorageService.SaveFormRoleMapping(inDto.FormId, inDto.Roles);
    }

    /// <remarks>
    /// Returns all roles for the specified form.
    /// </remarks>
    /// <summary>Get form roles</summary>
    /// <path>api/2.0/files/file/{fileId}/formroles</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Successfully retrieved all roles for the form", typeof(IEnumerable<FormRoleDto>))]
    [SwaggerResponse(403, "You do not have enough permissions to view the form roles")]
    [HttpGet("file/{fileId}/formroles")]
    public IAsyncEnumerable<FormRoleDto> GetAllFormRoles(FileIdRequestDto<T> inDto)
    {
        return fileStorageService.GetAllFormRoles(inDto.FileId);
    }

    /// <remarks>
    /// Performs the specified form filling action.
    /// </remarks>
    /// <summary>Perform form filling action</summary>
    /// <path>api/2.0/files/file/{fileId}/manageformfilling</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Successfully processed the form filling action")]
    [SwaggerResponse(403, "You do not have enough permissions to perform this action")]
    [HttpPut("file/{fileId}/manageformfilling")]
    public async Task ManageFormFilling(ManageFormFillingDto<T> inDto)
    {
        await fileStorageService.ManageFormFilling(inDto.FormId, inDto.Action);
    }

    /// <remarks>
    /// Returns the results of form submissions.
    /// </remarks>
    /// <summary>Get form submission results</summary>
    /// <path>api/2.0/files/file/{fileId}/submissions</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Form submission results were successfully retrieved")]
    [SwaggerResponse(403, "You do not have enough permissions to perform this action")]
    [HttpGet("file/{fileId}/submissions")]
    public Task<FormSubmissionsDto> GetFormSubmissions(FileIdRequestDto<int> inDto)
    {
        return fileStorageService.GetSubmissionsByFormId(inDto.FileId);
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
    /// Creates a new file in the "My documents" section with the title specified in the request.
    /// </remarks>
    /// <summary>Create a file in the "My documents" section</summary>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/@my/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/file")]
    public async Task<FileDto<int>> CreateFileInMyDocuments(CreateFile<JsonElement> inDto)
    {
        return await filesControllerHelperInternal.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
    }

    /// <remarks>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create an HTML file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/html</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommon(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates an HTML (.html) file in the "My documents" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create an HTML file in the "My documents" section</summary>
    /// <path>api/2.0/files/@my/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyDocuments(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateHtmlFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text (.txt) file in the "Common" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create a text file in the "Common" section</summary>
    /// <path>api/2.0/files/@common/text</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@common/text")]
    public async Task<FileDto<int>> CreateTextFileInCommon(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates a text (.txt) file in the "My documents" section with the title and contents specified in the request.
    /// </remarks>
    /// <summary>Create a text file in the "My documents" section</summary>
    /// <path>api/2.0/files/@my/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyDocuments(CreateTextOrHtmlFile inDto)
    {
        return await filesControllerHelperInternal.CreateTextFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
    }

    /// <remarks>
    /// Creates thumbnails for the files with the IDs specified in the request.
    /// </remarks>
    /// <summary>Create file thumbnails</summary>
    /// <path>api/2.0/files/thumbnails</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file IDs", typeof(IEnumerable<JsonElement>))]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnails(BaseBatchRequestDto inDto)
    {
        return await fileStorageService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}
