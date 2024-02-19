// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Helpers;

public class FilesControllerHelper(IServiceProvider serviceProvider,
        FilesSettingsHelper filesSettingsHelper,
        FileUploader fileUploader,
        SocketManager socketManager,
        FileDtoHelper fileDtoHelper,
        ApiContext apiContext,
        FileStorageService fileStorageService,
        FolderContentDtoHelper folderContentDtoHelper,
        IHttpContextAccessor httpContextAccessor,
        FolderDtoHelper folderDtoHelper,
        ILogger<FilesControllerHelper> logger,
        ApiDateTimeHelper apiDateTimeHelper,
        UserManager userManager,
        DisplayUserSettingsHelper displayUserSettingsHelper,
        FileConverter fileConverter,
        PathProvider pathProvider)
    : FilesHelperBase(filesSettingsHelper,
            fileUploader,
            socketManager,
            fileDtoHelper,
            apiContext,
            fileStorageService,
            folderContentDtoHelper,
            httpContextAccessor,
            folderDtoHelper)
    {
    private readonly ILogger _logger = logger;

    public async IAsyncEnumerable<FileDto<T>> ChangeHistoryAsync<T>(T fileId, int version, bool continueVersion)
    {
        var pair = await _fileStorageService.CompleteVersionAsync(fileId, version, continueVersion);
        var history = pair.Value;

        await foreach (var e in history)
        {
            yield return await _fileDtoHelper.GetAsync(e);
        }
    }

    public async Task<string> GetPresignedUri<T>(T fileId)
    {
        var file = await _fileStorageService.GetFileAsync(fileId, -1);
        return await pathProvider.GetFileStreamUrlAsync(file);
    }

    public async IAsyncEnumerable<ConversationResultDto> CheckConversionAsync<T>(CheckConversionRequestDto<T> checkConversionRequestDto)
    {
        var checkConversation = _fileStorageService.CheckConversionAsync([checkConversionRequestDto], checkConversionRequestDto.Sync);

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
                    o.File = await GetFileInfoAsync(jResult.Id, jResult.Version);
                }
                catch (Exception e)
                {
                    o.File = r.Result;
                    _logger.ErrorCheckConversion(e);
                }
            }

            yield return o;
        }
    }

    public async Task<FileDto<T>> CreateFileAsync<T>(T folderId, string title, JsonElement templateId, int formId, bool enableExternalExt = false)
    {
        File<T> file;

        if (templateId.ValueKind == JsonValueKind.Number)
        {
            file = await _fileStorageService.CreateNewFileAsync(new FileModel<T, int> { ParentId = folderId, Title = title, TemplateId = templateId.GetInt32() }, enableExternalExt);
        }
        else if (templateId.ValueKind == JsonValueKind.String)
        {
            file = await _fileStorageService.CreateNewFileAsync(new FileModel<T, string> { ParentId = folderId, Title = title, TemplateId = templateId.GetString() }, enableExternalExt);
        }
        else
        {
            file = await _fileStorageService.CreateNewFileAsync(new FileModel<T, int> { ParentId = folderId, Title = title, TemplateId = 0, FormId = formId }, enableExternalExt);
        }

        return await _fileDtoHelper.GetAsync(file);
    }

    public async Task<FileDto<T>> CreateHtmlFileAsync<T>(T folderId, string title, string content, bool updateIfExist)
    {
        ArgumentNullException.ThrowIfNull(title);

        return await CreateFileAsync(folderId, title, content, ".html", updateIfExist);
    }

    public async Task<FileDto<T>> CreateTextFileAsync<T>(T folderId, string title, string content, bool updateIfExist)
    {
        ArgumentNullException.ThrowIfNull(title);

        //Try detect content
        var extension = ".txt";
        if (!string.IsNullOrEmpty(content) && Regex.IsMatch(content, @"<([^\s>]*)(\s[^<]*)>"))
        {
                extension = ".html";
            }

        return await CreateFileAsync(folderId, title, content, extension, updateIfExist);
    }

    private async Task<FileDto<T>> CreateFileAsync<T>(T folderId, string title, string content, string extension, bool updateIfExist)
    {
        using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = await _fileUploader.ExecAsync(folderId,
                          title.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? title : (title + extension),
                          memStream.Length, memStream, updateIfExist);

        return await _fileDtoHelper.GetAsync(file);
    }

    public async Task<EditHistoryDataDto> GetEditDiffUrlAsync<T>(T fileId, int version = 0, string doc = null)
    {
        return await _fileStorageService.GetEditDiffUrlAsync(fileId, version, doc);
    }

    public async IAsyncEnumerable<EditHistoryDto> GetEditHistoryAsync<T>(T fileId, string doc = null)
    {
        await foreach (var f in _fileStorageService.GetEditHistoryAsync(fileId, doc))
        {
            yield return new EditHistoryDto(f, apiDateTimeHelper, userManager, displayUserSettingsHelper);
        }
    }

    public async IAsyncEnumerable<FileDto<T>> GetFileVersionInfoAsync<T>(T fileId)
    {
        await foreach (var e in _fileStorageService.GetFileHistoryAsync(fileId))
        {
            yield return await _fileDtoHelper.GetAsync(e);
        }
    }

    public async Task<FileDto<T>> LockFileAsync<T>(T fileId, bool lockFile)
    {
        var result = await _fileStorageService.LockFileAsync(fileId, lockFile);

        return await _fileDtoHelper.GetAsync(result);
    }

    public async IAsyncEnumerable<EditHistoryDto> RestoreVersionAsync<T>(T fileId, int version = 0, string url = null, string doc = null)
    {
        await foreach (var e in _fileStorageService.RestoreVersionAsync(fileId, version, url, doc))
        {
            yield return new EditHistoryDto(e, apiDateTimeHelper, userManager, displayUserSettingsHelper);
        }
    }

    public IAsyncEnumerable<ConversationResultDto> StartConversionAsync<T>(CheckConversionRequestDto<T> cheqConversionRequestDto)
    {
        cheqConversionRequestDto.StartConvert = true;

        return CheckConversionAsync(cheqConversionRequestDto);
    }

    public async Task<string> UpdateCommentAsync<T>(T fileId, int version, string comment)
    {
        return await _fileStorageService.UpdateCommentAsync(fileId, version, comment);
    }

    public async Task<FileDto<T>> UpdateFileAsync<T>(T fileId, string title, int lastVersion)
    {
        File<T> file = null;

        if (!string.IsNullOrEmpty(title))
        {
            file = await _fileStorageService.FileRenameAsync(fileId, title);
        }

        if (lastVersion <= 0)
        {
            return await GetFileInfoAsync(file!.Id);
        }

        var result = await _fileStorageService.UpdateToVersionAsync(fileId, lastVersion);
        file = result.Key;

        return await GetFileInfoAsync(file.Id);
    }

    public async Task<FileDto<T>> UpdateFileStreamAsync<T>(Stream file, T fileId, string fileExtension, bool encrypted = false, bool forcesave = false)
    {
        try
        {
            var resultFile = await _fileStorageService.UpdateFileStreamAsync(fileId, file, fileExtension, encrypted, forcesave);

            return await _fileDtoHelper.GetAsync(resultFile);
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException("File not found", e);
        }
    }

    public async Task<FileDto<TTemplate>> CopyFileAsAsync<T, TTemplate>(T fileId, TTemplate destFolderId, string destTitle, string password = null)
    {
        var service = serviceProvider.GetService<FileStorageService>();
        var file = await _fileStorageService.GetFileAsync(fileId, -1);
        var ext = FileUtility.GetFileExtension(file.Title);
        var destExt = FileUtility.GetFileExtension(destTitle);

        if (ext == destExt)
        {
            var newFile = await service.CreateNewFileAsync(new FileModel<TTemplate, T> { ParentId = destFolderId, Title = destTitle, TemplateId = fileId }, true);

            return await _fileDtoHelper.GetAsync(newFile);
        }

        await using var fileStream = await fileConverter.ExecAsync(file, destExt, password);
        var controller = serviceProvider.GetService<FilesControllerHelper>();
            return await controller.InsertFileAsync(destFolderId, fileStream, destTitle, true);
        }
    }
