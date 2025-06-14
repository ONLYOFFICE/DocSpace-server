﻿// (c) Copyright Ascensio System SIA 2009-2025
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

[Scope]
public class UploadControllerHelper(
    FilesSettingsHelper filesSettingsHelper,
    FileUploader fileUploader,
    SocketManager socketManager,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    IHttpContextAccessor httpContextAccessor,
    FilesLinkUtility filesLinkUtility,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    TenantManager tenantManager,
    IHttpClientFactory httpClientFactory,
    SecurityContext securityContext,
    IDaoFactory daoFactory,
    FileSecurity fileSecurity,
    FileChecker fileChecker,
    WebhookManager webhookManager,
    IEventBus eventBus,
    AuthContext authContext,
    Global global)
    : FilesHelperBase(
        filesSettingsHelper,
        fileUploader,
        socketManager,
        fileDtoHelper,
        fileStorageService,
        fileChecker,
        httpContextAccessor,
        webhookManager,
        daoFactory,
        eventBus,
        tenantManager,
        authContext)
    {
    public async Task<object> CreateEditSessionAsync<T>(T fileId, long fileSize)
    {
        var file = await _fileUploader.VerifyChunkedUploadForEditing(fileId, fileSize);

        return await CreateUploadSessionAsync(file, false, null, true);
    }

    public async Task<List<string>> CheckUploadAsync<T>(T folderId, IEnumerable<string> filesTitle)
    {
        var folderDao = _daoFactory.GetFolderDao<T>();
        var fileDao = _daoFactory.GetFileDao<T>();
        var toFolder = await folderDao.GetFolderAsync(folderId);
        if (toFolder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }
        if (!await fileSecurity.CanCreateAsync(toFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
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

    public async Task<object> CreateUploadSessionAsync<T>(T folderId, string fileName, long fileSize, string relativePath, bool encrypted, ApiDateTime createOn, bool createNewIfExist, bool keepVersion = false)
    {
        fileName = await global.GetAvailableTitleAsync(fileName, folderId, _daoFactory.GetFileDao<T>().IsExistAsync, FileEntryType.File);
        var file = await _fileUploader.VerifyChunkedUploadAsync(folderId, fileName, fileSize, !createNewIfExist, relativePath);
        return await CreateUploadSessionAsync(file, encrypted, createOn, keepVersion);
    }

    private async Task<object> CreateUploadSessionAsync<T>(File<T> file, bool encrypted, ApiDateTime createOn, bool keepVersion = false)
    {
        if (filesLinkUtility.IsLocalFileUploader)
        {
            var session = await _fileUploader.InitiateUploadAsync(file.ParentId, file.Id ?? default, file.Title, file.ContentLength, encrypted, keepVersion, createOn);

            var responseObject = await chunkedUploadSessionHelper.ToResponseObjectAsync(session, true);

            return new
            {
                success = true,
                data = responseObject
            };
        }

        var createSessionUrl = await filesLinkUtility.GetInitiateUploadSessionUrlAsync(_tenantManager.GetCurrentTenantId(), file.ParentId, file.Id, file.Title, file.ContentLength, encrypted, securityContext);

        var httpClient = httpClientFactory.CreateClient();

        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(createSessionUrl),
            Method = HttpMethod.Post
        };

        // hack for uploader.onlyoffice.com in api requests
        //var rewriterHeader = _httpContextAccessor.HttpContext.Request.Headers[HttpRequestExtensions.UrlRewriterHeader];
        //if (!string.IsNullOrEmpty(rewriterHeader))
        //{
        //    request.Headers.Add(HttpRequestExtensions.UrlRewriterHeader, rewriterHeader.ToString());
        //}

        using var response = await httpClient.SendAsync(request);
        var responseAsString = await response.Content.ReadAsStringAsync();
        var jObject = JObject.Parse(responseAsString); //result is json string

        var result = new
        {
            success = jObject["success"].ToString(),
            data = new
            {
                id = jObject["data"]["id"].ToString(),
                path = jObject["data"]["path"].Values().Select(x => (T)Convert.ChangeType(x, typeof(T))),
                created = jObject["data"]["created"].Value<DateTime>(),
                expired = jObject["data"]["expired"].Value<DateTime>(),
                location = jObject["data"]["location"].ToString(),
                bytes_uploaded = jObject["data"]["bytes_uploaded"].Value<long>(),
                bytes_total = jObject["data"]["bytes_total"].Value<long>()
            }
        };

        return result;
    }

    public async Task<object> UploadFileAsync<T>(T folderId, UploadRequestDto uploadModel)
    {
        if (uploadModel.StoreOriginalFileFlag.HasValue)
        {
            await _filesSettingsHelper.SetStoreOriginalFiles(uploadModel.StoreOriginalFileFlag.Value);
        }

        IEnumerable<IFormFile> files = _httpContextAccessor.HttpContext?.Request.Form.Files;
        if (!files.Any())
        {
            files = uploadModel.Files;
        }

        if (files != null && files.Any())
        {
            if (files.Count() == 1)
            {
                //Only one file. return it
                var postedFile = files.First();

                return await InsertFileAsync(folderId, postedFile.OpenReadStream(), postedFile.FileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus);
            }

            //For case with multiple files
            var result = new List<object>();

            foreach (var postedFile in uploadModel.Files)
            {
                result.Add(await InsertFileAsync(folderId, postedFile.OpenReadStream(), postedFile.FileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus));
            }

            return result;
        }

        if (uploadModel.File != null)
        {
            var fileName = "file" + MimeMapping.GetExtention(uploadModel.ContentType.MediaType);
            if (uploadModel.ContentDisposition != null)
            {
                fileName = uploadModel.ContentDisposition.FileName;
            }

            return new List<FileDto<T>>
            {
                await InsertFileAsync(folderId, uploadModel.File.OpenReadStream(), fileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus)
            };
        }

        throw new InvalidOperationException("No input files");
    }
}
