// (c) Copyright Ascensio System SIA 2009-2025
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
    DocumentProcessingService documentProcessingService,
    SharingService sharingService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    HistoryApiHelper historyApiHelper,
    IFusionCache hybridCache,
    ApiDateTimeHelper apiDateTimeHelper,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EntriesOrderService entriesOrderService,
    FileService fileService,
    FillingFormResultDtoHelper fillingFormResultDtoHelper,
    FormService formService,
    FileChecker fileChecker,
    PathProvider pathProvider,
    ILogger<FilesController<int>> logger,
    FileUploader fileUploader,
    FileConverter fileConverter)
    : FilesController<int>(
        documentProcessingService,
        sharingService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache,
        apiDateTimeHelper,
        userManager,
        displayUserSettingsHelper,
        entriesOrderService,
        fileService,
        fillingFormResultDtoHelper,
        formService, 
        fileChecker, 
        pathProvider,
        logger,
        fileUploader,
        fileConverter)
{
    /// <summary>
    /// Returns the list of actions performed on the file with the specified identifier.
    /// </summary>
    /// <short>
    /// Get file history
    /// </short>
    /// <path>api/2.0/files/file/{fileId}/log</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of actions performed on the file", typeof(IAsyncEnumerable<HistoryDto>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "The required file was not found")]
    [HttpGet("file/{fileId:int}/log")]
    public IAsyncEnumerable<HistoryDto> GetFileHistoryAsync(HistoryRequestDto inDto)
    {
        return historyApiHelper.GetFileHistoryAsync(inDto.FileId, inDto.FromDate, inDto.ToDate);
    }
}

public class FilesControllerThirdparty(
    DocumentProcessingService documentProcessingService,
    SharingService sharingService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    ApiDateTimeHelper apiDateTimeHelper,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EntriesOrderService entriesOrderService,
    FileService fileService,
    FillingFormResultDtoHelper fillingFormResultDtoHelper,
    FormService formService,
    FileChecker fileChecker,
    PathProvider pathProvider,
    ILogger<FilesController<string>> logger,
    FileUploader fileUploader,
    FileConverter fileConverter)
    : FilesController<string>(
        documentProcessingService,
        sharingService,
        fileOperationsManager,
        fileOperationDtoHelper,
        folderDtoHelper,
        fileDtoHelper,
        apiContext,
        fileShareDtoHelper,
        hybridCache, 
        apiDateTimeHelper,
        userManager,
        displayUserSettingsHelper, 
        entriesOrderService,
        fileService,
        fillingFormResultDtoHelper,
        formService, 
        fileChecker, 
        pathProvider,
        logger,
        fileUploader,
        fileConverter);

public abstract class FilesController<T>(
    DocumentProcessingService documentProcessingService,
    SharingService sharingService,
    FileDeleteOperationsManager fileOperationsManager,
    FileOperationDtoHelper fileOperationDtoHelper,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    ApiContext apiContext,
    FileShareDtoHelper fileShareDtoHelper,
    IFusionCache hybridCache,
    ApiDateTimeHelper apiDateTimeHelper,
    UserManager userManager,
    DisplayUserSettingsHelper displayUserSettingsHelper,
    EntriesOrderService entriesOrderService,
    FileService fileService,
    FillingFormResultDtoHelper fillingFormResultDtoHelper,
    FormService formService,
    FileChecker fileChecker,
    PathProvider pathProvider,
    ILogger<FilesController<T>> logger,
    FileUploader fileUploader,
    FileConverter fileConverter)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <summary>
    /// Changes the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Change version history</short>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated information about file versions", typeof(IAsyncEnumerable<FileDto<int>>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpPut("file/{fileId}/history")]
    public async IAsyncEnumerable<FileDto<T>> ChangeHistoryAsync(ChangeHistoryRequestDto<T> inDto)
    {
        var pair = await fileService.CompleteVersionAsync(inDto.FileId, inDto.File.Version, inDto.File.ContinueVersion);
        var history = pair.Value;

        await foreach (var e in history)
        {
            yield return await _fileDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Checks the conversion status of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get conversion status</short>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [HttpGet("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> CheckConversionAsync(CheckConversionStatusRequestDto<T> inDto)
    {
        return CheckConversionAsync(new CheckConversionRequestDto<T>
        {
            FileId = inDto.FileId,
            StartConvert = inDto.Start
        });
    }

    /// <summary>
    /// Returns a pre-signed URL to download a file with the specified ID.
    /// This temporary link provides secure access to the file.
    /// </summary>
    /// <short>Get file download link</short>
    /// <path>api/2.0/files/file/{fileId}/presigneduri</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File download link", typeof(string))]
    [HttpGet("file/{fileId}/presigneduri")]
    public async Task<string> GetPresignedUri(FileIdRequestDto<T> inDto)
    {        
        var file = await fileService.GetFileAsync(inDto.FileId, -1);
        return pathProvider.GetFileStreamUrl(file);
    }

    /// <summary>
    /// Checks if the PDF file is a form or not.
    /// </summary>
    /// <short>Check the PDF file</short>
    /// <path>api/2.0/files/file/{fileId}/isformpdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Boolean value: true - the PDF file is form, false - the PDF file is not a form", typeof(bool))]
    [HttpGet("file/{fileId}/isformpdf")]
    public async Task<bool> isFormPDF(FileIdRequestDto<T> inDto)
    {
        var file = await fileService.GetFileAsync(inDto.FileId, -1);
        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

        if (fileType == FileType.Pdf)
        {
            return await fileChecker.CheckExtendedPDF(file);
        }
        return false;
    }

    /// <summary>
    /// Copies (and converts if possible) an existing file to the specified folder.
    /// </summary>
    /// <short>Copy a file</short>
    /// <path>api/2.0/files/file/{fileId}/copyas</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Copied file entry information", typeof(FileEntryDto))]
    [SwaggerResponse(400, "No file id or folder id toFolderId determine provider")]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("file/{fileId}/copyas")]
    public async Task<FileEntryDto> CopyFileAs(CopyAsRequestDto<T> inDto)
    {
        if (inDto.File.DestFolderId.ValueKind == JsonValueKind.Number)
        {
            return await _fileDtoHelper.GetAsync(await CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetInt32(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm));
        }

        if (inDto.File.DestFolderId.ValueKind == JsonValueKind.String)
        {
            return await _fileDtoHelper.GetAsync(await CopyFileAsAsync(inDto.FileId, inDto.File.DestFolderId.GetString(), inDto.File.DestTitle, inDto.File.Password, inDto.File.ToForm));
        }

        return null;
        
        async Task<File<TTemplate>> CopyFileAsAsync<TTemplate>(T fileId, TTemplate destFolderId, string destTitle, string password = null, bool toForm = false)
        {
            var file = await fileService.GetFileAsync(fileId, -1);
            var ext = FileUtility.GetFileExtension(file.Title);
            var destExt = FileUtility.GetFileExtension(destTitle);

            if (ext == destExt)
            {
                var newFile = await fileService.CreateNewFileAsync(new FileModel<TTemplate, T> { ParentId = destFolderId, Title = destTitle, TemplateId = fileId }, true);

                return newFile;
            }

            await using var fileStream = await fileConverter.ExecAsync(file, destExt, password, toForm);
            return await fileUploader.InsertFileAsync(destFolderId, fileStream, destTitle, true);
        }
    }

    /// <summary>
    /// Creates a new file in the specified folder with the title specified in the request.
    /// </summary>
    /// <short>Create a file</short>
    /// <remarks>If a file extension is different from DOCX/XLSX/PPTX and refers to one of the known text, spreadsheet, or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not specified or is unknown, the DOCX extension will be added to the file title.</remarks>
    /// <path>api/2.0/files/{folderId}/file</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/file")]
    public async Task<FileDto<T>> CreateFileAsync(CreateFileRequestDto<T> inDto)
    {
        var file = await fileService.CreateFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.TemplateId, inDto.File.FormId, inDto.File.EnableExternalExt);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file</short>
    /// <path>api/2.0/files/{folderId}/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("{folderId}/html")]
    public async Task<FileDto<T>> CreateHtmlFileAsync(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        var file = await fileUploader.ExecAsync(inDto.FolderId, inDto.File.Title, ".html", inDto.File.Content, !inDto.File.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates a text (.txt) file in the selected folder with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a text file</short>
    /// <path>api/2.0/files/{folderId}/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("{folderId}/text")]
    public async Task<FileDto<T>> CreateTextFileAsync(CreateTextOrHtmlFileRequestDto<T> inDto)
    {
        var file = await fileUploader.CreateTextFileAsync(inDto.FolderId, inDto.File.Title, inDto.File.Content, !inDto.File.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Deletes a file with the ID specified in the request.
    /// </summary>
    /// <short>Delete a file</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file operations", typeof(IAsyncEnumerable<FileOperationDto>))]
    [HttpDelete("file/{fileId}")]
    public async IAsyncEnumerable<FileOperationDto> DeleteFile(DeleteRequestDto<T> inDto)
    {
        await fileOperationsManager.Publish([], [inDto.FileId], false, !inDto.File.DeleteAfter, inDto.File.Immediately);
        
        foreach (var e in await fileOperationsManager.GetOperationResults())
        {
            yield return await fileOperationDtoHelper.GetAsync(e);
        }
    }

    /// <summary>
    /// Retrieves the result of a form-filling session.
    /// </summary>
    /// <short>
    /// Get form-filling result
    /// </short>
    /// <path>api/2.0/files/file/fillresult</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Ok", typeof(FillingFormResultDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/fillresult")]
    public async Task<FillingFormResultDto<T>> GetFillResultAsync(GetFillResultRequestDto inDto)
    {
        var completedFormId = await hybridCache.GetOrDefaultAsync<T>(inDto.FillingSessionId);

        return completedFormId != null ? await fillingFormResultDtoHelper.GetAsync(completedFormId) : null;
    }

    /// <summary>
    /// Returns a URL to the changes of a file version specified in the request.
    /// </summary>
    /// <short>Get changes URL</short>
    /// <path>api/2.0/files/file/{fileId}/edit/diff</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File version history data", typeof(EditHistoryDataDto))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/diff")]
    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync(EditDiffUrlRequestDto<T> inDto)
    {
        return await documentProcessingService.GetEditDiffUrlAsync(inDto.FileId, inDto.Version);
    }

    /// <summary>
    /// Returns the version history of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get version history</short>
    /// <path>api/2.0/files/file/{fileId}/edit/history</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/edit/history")]
    public IAsyncEnumerable<EditHistoryDto> GetEditHistoryAsync(FileIdRequestDto<T> inDto)
    {
        return documentProcessingService.GetEditHistoryAsync(inDto.FileId).Select(f => new EditHistoryDto(f, apiDateTimeHelper, userManager, displayUserSettingsHelper));
    }

    /// <summary>
    /// Returns the detailed information about a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file information</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File information", typeof(FileDto<int>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}")]
    public async Task<FileDto<T>> GetFileInfoAsync(FileInfoRequestDto<T> inDto)
    {
        var file = await fileService.GetFileAsync(inDto.FileId, inDto.Version);
        file = file.NotFoundIfNull("File not found");

        return await _fileDtoHelper.GetAsync(file);
    }


    /// <summary>
    /// Returns the detailed information about all the available file versions with the ID specified in the request.
    /// </summary>
    /// <short>Get file versions</short>
    /// <path>api/2.0/files/file/{fileId}/history</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Information about file versions: folder ID, version, version group, content length, pure content length, file status, URL to view a file, web URL, file type, file extension, comment, encrypted or not, thumbnail URL, thumbnail status, locked or not, user ID who locked a file, denies file downloading or not, denies file sharing or not, file accessibility", typeof(IAsyncEnumerable<FileDto<int>>))]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/history")]
    public IAsyncEnumerable<FileDto<T>> GetFileVersionInfoAsync(FileIdRequestDto<T> inDto)
    {
        return fileService.GetFileHistoryAsync(inDto.FileId).SelectAwait(async e => await _fileDtoHelper.GetAsync(e));
    }

    /// <summary>
    /// Locks a file with the ID specified in the request.
    /// </summary>
    /// <short>Lock a file</short>
    /// <path>api/2.0/files/file/{fileId}/lock</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Locked file information", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/lock")]
    public async Task<FileDto<T>> LockFileAsync(LockFileRequestDto<T> inDto)
    {       
        var result = await fileService.LockFileAsync(inDto.FileId, inDto.File.LockFile);

        return await _fileDtoHelper.GetAsync(result);
    }

    /// <summary>
    /// Sets the Custom Filter editing mode to a file with the ID specified in the request.
    /// </summary>
    /// <short>Set the Custom Filter editing mode</short>
    /// <path>api/2.0/files/file/{fileId}/customfilter</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File information", typeof(FileDto<int>))]
    [HttpPut("file/{fileId}/customfilter")]
    public async Task<FileDto<T>> SetCustomFilterTagAsync(FileCustomFilterRequestDto<T> inDto)
    {        
        var result = await fileService.SetCustomFilterTagAsync(inDto.FileId, inDto.Parameters.Enabled);

        return await _fileDtoHelper.GetAsync(result);
    }

    /// <summary>
    /// Restores a file version specified in the request.
    /// </summary>
    /// <short>Restore a file version</short>
    /// <path>api/2.0/files/file/{fileId}/restoreversion</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Version history data: file ID, key, file version, version group, a user who updated a file, creation time, history changes in the string format, list of history changes, server version", typeof(IAsyncEnumerable<EditHistoryDto>))]
    [SwaggerResponse(400, "No file id or folder id toFolderId determine provider")]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpGet("file/{fileId}/restoreversion")]
    public IAsyncEnumerable<EditHistoryDto> RestoreVersionAsync(RestoreVersionRequestDto<T> inDto)
    {
        return fileService.RestoreVersionAsync(inDto.FileId, inDto.Version, inDto.Url).Select(e => new EditHistoryDto(e, apiDateTimeHelper, userManager, displayUserSettingsHelper));
    }

    /// <summary>
    /// Starts a conversion operation of a file with the ID specified in the request.
    /// </summary>
    /// <short>Start file conversion</short>
    /// <path>api/2.0/files/file/{fileId}/checkconversion</path>
    /// <collection>list</collection>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Conversion result", typeof(IAsyncEnumerable<ConversationResultDto>))]
    [HttpPut("file/{fileId}/checkconversion")]
    public IAsyncEnumerable<ConversationResultDto> StartConversion(StartConversionRequestDto<T> inDto)
    {
        inDto.CheckConversion ??= new CheckConversionRequestDto<T>();
        inDto.CheckConversion.FileId = inDto.FileId;
        inDto.CheckConversion.StartConvert = true;

        return CheckConversionAsync(inDto.CheckConversion);
    }

    /// <summary>
    /// Updates a comment in a file with the ID specified in the request.
    /// </summary>
    /// <short>Update a comment</short>
    /// <path>api/2.0/files/file/{fileId}/comment</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Updated comment", typeof(string))]
    [HttpPut("file/{fileId}/comment")]
    public async Task<string> UpdateCommentAsync(UpdateCommentRequestDto<T> inDto)
    {
        return await fileService.UpdateCommentAsync(inDto.FileId, inDto.File.Version, inDto.File.Comment);
    }

    /// <summary>
    /// Updates the information of the selected file with the parameters specified in the request.
    /// </summary>
    /// <short>Update a file</short>
    /// <path>api/2.0/files/file/{fileId}</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [AllowAnonymous]
    [HttpPut("file/{fileId}")]
    public async Task<FileDto<T>> UpdateFileAsync(UpdateFileRequestDto<T> inDto)
    {
        var (fileId, title, lastVersion) = (inDto.FileId, inDto.File.Title, inDto.File.LastVersion);
        File<T> file = null;

        if (!string.IsNullOrEmpty(title))
        {
            file = await fileService.FileRenameAsync(fileId, title);
        }

        if (lastVersion <= 0)
        {        
            return await _fileDtoHelper.GetAsync(file);
        }

        var result = await fileService.UpdateToVersionAsync(fileId, lastVersion);
        file = result.Key;

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Updates the contents of a file with the ID specified in the request.
    /// </summary>
    /// <short>Update file contents</short>
    /// <path>api/2.0/files/{fileId}/update</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [SwaggerResponse(404, "File not found")]
    [HttpPut("{fileId}/update")]
    public async Task<FileDto<T>> UpdateFileStreamFromFormAsync(FileStreamRequestDto<T> inDto)
    {
        IEnumerable<IFormFile> files = Request.Form.Files;
        var file = files.Any() ? files.First() : inDto.File;

        try
        {
            var resultFile = await fileService.UpdateFileStreamAsync(inDto.FileId, file.OpenReadStream(), inDto.FileExtension, inDto.Encrypted, inDto.Forcesave);

            return await _fileDtoHelper.GetAsync(resultFile);
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound, e);
        }
    }

    /// <summary>
    /// Creates a primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Create primary external link</short>
    /// <path>api/2.0/files/file/{id}/link</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [HttpPost("file/{id}/link")]
    public async Task<FileShareDto> CreatePrimaryExternalLinkAsync(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await sharingService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File, inDto.File.Access, expirationDate: inDto.File.ExpirationDate, requiredAuth: inDto.File.Internal, allowUnlimitedDate: true);
        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Returns the primary external link by the identifier specified in the request.
    /// </summary>
    /// <short>Get primary external link</short>
    /// <path>api/2.0/files/file/{id}/link</path>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [SwaggerResponse(404, "Not Found")]
    [AllowAnonymous]
    [HttpGet("file/{id}/link")]
    public async Task<FileShareDto> GetFilePrimaryExternalLinkAsync(FilePrimaryIdRequestDto<T> inDto)
    {
        var linkAce = await sharingService.GetPrimaryExternalLinkAsync(inDto.Id, FileEntryType.File);

        return await fileShareDtoHelper.Get(linkAce);
    }

    /// <summary>
    /// Sets order of the file with ID specified in the request.
    /// </summary>
    /// <short>
    /// Set file order
    /// </short>
    /// <path>api/2.0/files/{fileId}/order</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to perform the operation")]
    [SwaggerResponse(404, "Not Found")]
    [HttpPut("{fileId}/order")]
    public async Task<FileDto<T>> SetOrderFile(OrderFileRequestDto<T> inDto)
    {
        var file = await entriesOrderService.SetFileOrder(inDto.FileId, inDto.Order.Order);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Sets order of the files.
    /// </summary>
    /// <short>
    /// Set order of files
    /// </short>
    /// <path>api/2.0/files/order</path>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated file entries information", typeof(IAsyncEnumerable<FileDto<int>>))]
    [HttpPut("order")]
    public IAsyncEnumerable<FileEntryDto<T>> SetFilesOrder(OrdersRequestDto<T> inDto)
    {
        return entriesOrderService.SetOrderAsync(inDto.Items).SelectAwait<FileEntry<T>, FileEntryDto<T>>(
            async e => e.FileEntryType == FileEntryType.Folder ? 
                await _folderDtoHelper.GetAsync(e as Folder<T>) : 
                await _fileDtoHelper.GetAsync(e as File<T>));
    }

    /// <summary>
    /// Returns the external links of a file with the ID specified in the request.
    /// </summary>
    /// <short>Get file external links</short>
    /// <path>api/2.0/files/file/{id}/links</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "File security information", typeof(IAsyncEnumerable<FileShareDto>))]
    [HttpGet("file/{id}/links")]
    public async IAsyncEnumerable<FileShareDto> GetLinksAsync(FilePrimaryIdRequestDto<T> inDto)
    {
        var offset = Convert.ToInt32(apiContext.StartIndex);
        var count = Convert.ToInt32(apiContext.Count);

        var totalCount = await sharingService.GetPureSharesCountAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null);

        apiContext.SetCount(Math.Min(totalCount - offset, count)).SetTotalCount(totalCount);

        await foreach (var ace in sharingService.GetPureSharesAsync(inDto.Id, FileEntryType.File, ShareFilterType.ExternalLink, null, offset, count))
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
    [SwaggerResponse(200, "File security information", typeof(FileShareDto))]
    [HttpPut("file/{id}/links")]
    public async Task<FileShareDto> SetExternalLinkAsync(FileLinkRequestDto<T> inDto)
    {
        var linkAce = await sharingService.SetExternalLinkAsync(inDto.Id, FileEntryType.File, inDto.File.LinkId, null, inDto.File.Access, requiredAuth: inDto.File.Internal, 
            primary: inDto.File.Primary, expirationDate: inDto.File.ExpirationDate);

        return linkAce is not null ? await fileShareDtoHelper.Get(linkAce) : null;
    }

    /// <summary>
    /// Saves a file with the identifier specified in the request as a PDF document.
    /// </summary>
    /// <short>Save a file as PDF</short>
    /// <path>api/2.0/files/file/{id}/saveaspdf</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(404, "File not found")]
    [HttpPost("file/{id}/saveaspdf")]
    public async Task<FileDto<T>> SaveAsPdf(SaveAsPdfRequestDto<T> inDto)
    {
        try
        {
            var resultFile = await formService.SaveAsPdf(inDto.Id, inDto.File.FolderId, inDto.File.Title);
            return await _fileDtoHelper.GetAsync(resultFile);
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound, e);
        }
    }

    /// <summary>
    /// Saves the form role mapping.
    /// </summary>
    /// <short>Save form role mapping</short>
    /// <path>api/2.0/files/file/{fileId}/formrolemapping</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Updated information about form role mappings", typeof(FormRole))]
    [SwaggerResponse(403, "You do not have enough permissions to edit the file")]
    [HttpPost("file/{fileId}/formrolemapping")]
    public async Task SaveFormRoleMapping(SaveFormRoleMappingDto<T> inDto)
    {
        await formService.SaveFormRoleMapping(inDto.FormId, inDto.Roles);
    }

    /// <summary>
    /// Returns all roles for the specified form.
    /// </summary>
    /// <short>Get form roles</short>
    /// <path>api/2.0/files/file/{fileId}/formroles</path>
    /// <collection>list</collection>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Successfully retrieved all roles for the form", typeof(IEnumerable<FormRole>))]
    [SwaggerResponse(403, "You do not have enough permissions to view the form roles")]
    [HttpGet("file/{fileId}/formroles")]
    public IAsyncEnumerable<FormRoleDto> GetAllFormRoles(FileIdRequestDto<T> inDto)
    {
        return formService.GetAllFormRoles(inDto.FileId);
    }

    /// <summary>
    /// Performs the specified form filling action.
    /// </summary>
    /// <short>Perform form filling action</short>
    /// <path>api/2.0/files/file/{fileId}/manageformfilling</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Successfully processed the form filling action")]
    [SwaggerResponse(403, "You do not have enough permissions to perform this action")]
    [HttpPut("file/{fileId}/manageformfilling")]
    public async Task ManageFormFilling(ManageFormFillingDto<T> inDto)
    {
        await formService.ManageFormFilling(inDto.FormId, inDto.Action);
    }
    
    private async IAsyncEnumerable<ConversationResultDto> CheckConversionAsync(CheckConversionRequestDto<T> checkConversionRequestDto)
    {
        var checkConversation = documentProcessingService.CheckConversionAsync([checkConversionRequestDto], checkConversionRequestDto.Sync);

        await foreach (var r in checkConversation)
        {
            var o = new ConversationResultDto
            {
                Id = r.Id,
                Error = r.Error,
                OperationType = r.OperationType,
                Processed = r.Processed,
                Progress = r.Progress,
                Source = r.Source
            };

            if (!string.IsNullOrEmpty(r.Result))
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true
                    };

                    var jResult = JsonSerializer.Deserialize<FileJsonSerializerData<T>>(r.Result, options);
                    o.File = await GetFileInfoAsync(new FileInfoRequestDto<T> { FileId = jResult.Id, Version = jResult.Version});
                }
                catch (Exception e)
                {
                    o.File = r.Result;
                    logger.ErrorCheckConversion(e);
                }
            }

            yield return o;
        }
    }
}

public class FilesControllerCommon(
    GlobalFolderHelper globalFolderHelper,
    FileService fileService,
    FolderDtoHelper folderDtoHelper,
    FileDtoHelper fileDtoHelper,
    FileUploader fileUploader)
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
    public async Task<FileDto<int>> CreateFileMyDocumentsAsync(CreateFile<JsonElement> inDto)
    {        
        var file = await fileService.CreateFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.TemplateId, inDto.FormId, inDto.EnableExternalExt);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "Common" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "Common" section</short>
    /// <path>api/2.0/files/@common/html</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@common/html")]
    public async Task<FileDto<int>> CreateHtmlFileInCommonAsync(CreateTextOrHtmlFile inDto)
    {        
        var file = await fileUploader.ExecAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, ".html", inDto.Content, !inDto.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates an HTML (.html) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create an HTML file in the "My documents" section</short>
    /// <path>api/2.0/files/@my/html</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [SwaggerResponse(403, "You don't have enough permission to create")]
    [HttpPost("@my/html")]
    public async Task<FileDto<int>> CreateHtmlFileInMyAsync(CreateTextOrHtmlFile inDto)
    { 
        var file = await fileUploader.ExecAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, ".html", inDto.Content, !inDto.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
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
    public async Task<FileDto<int>> CreateTextFileInCommonAsync(CreateTextOrHtmlFile inDto)
    {
        var file = await fileUploader.CreateTextFileAsync(await globalFolderHelper.FolderCommonAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates a text (.txt) file in the "My documents" section with the title and contents specified in the request.
    /// </summary>
    /// <short>Create a text file in the "My documents" section</short>
    /// <path>api/2.0/files/@my/text</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "New file information", typeof(FileDto<int>))]
    [HttpPost("@my/text")]
    public async Task<FileDto<int>> CreateTextFileInMyAsync(CreateTextOrHtmlFile inDto)
    {
        var file = await fileUploader.CreateTextFileAsync(await globalFolderHelper.FolderMyAsync, inDto.Title, inDto.Content, !inDto.CreateNewIfExist);
        return await _fileDtoHelper.GetAsync(file);
    }

    /// <summary>
    /// Creates thumbnails for the files with the IDs specified in the request.
    /// </summary>
    /// <short>Create file thumbnails</short>
    /// <path>api/2.0/files/thumbnails</path>
    /// <collection>list</collection>
    /// <requiresAuthorization>false</requiresAuthorization>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "List of file IDs", typeof(IEnumerable<JsonElement>))]
    [AllowAnonymous]
    [HttpPost("thumbnails")]
    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(BaseBatchRequestDto inDto)
    {
        return await fileService.CreateThumbnailsAsync(inDto.FileIds.ToList());
    }
}