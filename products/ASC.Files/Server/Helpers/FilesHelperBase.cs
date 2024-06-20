﻿// (c) Copyright Ascensio System SIA 2009-2024
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

public abstract class FilesHelperBase(
    FilesSettingsHelper filesSettingsHelper,
    FileUploader fileUploader,
    SocketManager socketManager,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    IHttpContextAccessor httpContextAccessor)
    {
    protected readonly FilesSettingsHelper _filesSettingsHelper = filesSettingsHelper;
    protected readonly FileUploader _fileUploader = fileUploader;
    protected readonly FileDtoHelper _fileDtoHelper = fileDtoHelper;
    protected readonly FileStorageService _fileStorageService = fileStorageService;
    protected readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<FileDto<T>> InsertFileAsync<T>(T folderId, Stream file, string title, bool createNewIfExist, bool keepConvertStatus = false)
    {
        try
        {
            var resultFile = await _fileUploader.ExecAsync(folderId, title, file.Length, file, !createNewIfExist, !keepConvertStatus);

            await socketManager.CreateFileAsync(resultFile);

            return await _fileDtoHelper.GetAsync(resultFile);
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException("File not found", e);
        }
        catch (DirectoryNotFoundException e)
        {
            throw new ItemNotFoundException("Folder not found", e);
        }
    }

    public IFormFile GetFileFromRequest(IModelWithFile model)
    {
        IEnumerable<IFormFile> files = _httpContextAccessor.HttpContext.Request.Form.Files;
        if (files.Any())
        {
            return files.First();
        }

        return model.File;
    }

    public async Task<FileDto<T>> GetFileInfoAsync<T>(T fileId, int version = -1)
    {
        var file = await _fileStorageService.GetFileAsync(fileId, version);
        file = file.NotFoundIfNull("File not found");

        return await _fileDtoHelper.GetAsync(file);
    }
}
