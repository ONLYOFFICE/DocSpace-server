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

namespace ASC.Web.Files.Utils;

[Scope]
public class FileUploader(
    FileUtility fileUtility,
    UserManager userManager,
    TenantManager tenantManager,
    TenantLogoManager tenantLogoManager,
    AuthContext authContext,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    FileMarker fileMarker,
    FileConverter fileConverter,
    IDaoFactory daoFactory,
    Global global,
    FilesLinkUtility filesLinkUtility,
    FilesMessageService filesMessageService,
    FileSecurity fileSecurity,
    LockerManager lockerManager,
    IServiceProvider serviceProvider,
    ChunkedUploadSessionHolder chunkedUploadSessionHolder,
    FileTrackerHelper fileTracker,
    SocketManager socketManager,
    FileChecker fileChecker,
    TempStream tempStream,
    WebhookManager webhookManager,
    IEventBus eventBus,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    IHttpClientFactory clientFactory,
    SecurityContext securityContext,
    FilesSettingsHelper filesSettingsHelper,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<File<T>> InsertFileAsync<T>(T folderId, Stream file, string title, bool createNewIfExist, bool keepConvertStatus = false)
    {
        try
        {
            var resultFile = await ExecAsync(folderId, title, file.Length, file, !createNewIfExist, !keepConvertStatus);

            await socketManager.CreateFileAsync(resultFile);

            await webhookManager.PublishAsync(WebhookTrigger.FileUploaded, resultFile);

            var folderDao = daoFactory.GetFolderDao<T>();
            var room = await folderDao.GetParentFoldersAsync(folderId).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));
            if (room != null)
            {
                var data = room.Id is int rId && resultFile.Id is int fId
                    ? new RoomNotifyIntegrationData<int> { RoomId = rId, FileId = fId }
                    : null;

                var thirdPartyData = room.Id is string srId && resultFile.Id is string sfId
                    ? new RoomNotifyIntegrationData<string> { RoomId = srId, FileId = sfId }
                    : null;

                var evt = new RoomNotifyIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant().Id)
                {
                    Data = data,
                    ThirdPartyData = thirdPartyData
                };

                await eventBus.PublishAsync(evt);
            }

            return resultFile;
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound, e);
        }
        catch (DirectoryNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound, e);
        }
    }
    
    public async Task<File<T>> CreateTextFileAsync<T>(T folderId, string title, string content, bool updateIfExist)
    {
        //Try detect content
        var extension = ".txt";
        if (!string.IsNullOrEmpty(content) && Regex.IsMatch(content, @"<([^\s>]*)(\s[^<]*)>"))
        {
            extension = ".html";
        }

        return await ExecAsync(folderId, title, extension, content, updateIfExist);
    }
    
    public async Task<File<T>> ExecAsync<T>(T folderId, string title, string extension, string content, bool updateIfExist)
    {
        title = title.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? title : (title + extension);
        using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return await ExecAsync(folderId, title, memStream.Length, memStream, updateIfExist);
    }
    
    public async Task<File<T>> ExecAsync<T>(T folderId, string title, long contentLength, Stream data, bool createNewIfExist, bool deleteConvertStatus = true)
    {
        if (contentLength <= 0)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_EmptyFile);
        }

        var file = await VerifyFileUploadAsync(folderId, title, contentLength, !createNewIfExist);

        var dao = daoFactory.GetFileDao<T>();
        file = await dao.SaveFileAsync(file, data);

        var linkDao = daoFactory.GetLinkDao<T>();
        await linkDao.DeleteAllLinkAsync(file.Id);

        await fileMarker.MarkAsNewAsync(file);

        if (fileConverter.EnableAsUploaded && fileConverter.MustConvert(file))
        {
            await fileConverter.ExecAsynchronouslyAsync(file, deleteConvertStatus, !createNewIfExist);
        }

        await webhookManager.PublishAsync(WebhookTrigger.FileCreated, file);

        return file;
    }

    private async Task<File<T>> VerifyFileUploadAsync<T>(T folderId, string fileName, bool updateIfExists, string relativePath = null)
    {
        fileName = Global.ReplaceInvalidCharsAndTruncate(fileName);

        if (global.EnableUploadFilter && !fileUtility.ExtsUploadable.Contains(FileUtility.GetFileExtension(fileName)))
        {
            throw new NotSupportedException(FilesCommonResource.ErrorMessage_NotSupportedFormat);
        }

        folderId = await GetFolderIdAsync(folderId, string.IsNullOrEmpty(relativePath) ? null : relativePath.Split('/').ToList());

        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(folderId, fileName);

        if (updateIfExists && await CanEditAsync(file))
        {
            file.Title = fileName;
            file.ConvertedType = null;
            file.Comment = FilesCommonResource.CommentUpload;
            file.Version++;
            file.VersionGroup++;
            file.Encrypted = false;
            file.ThumbnailStatus = Thumbnail.Waiting;

            return file;
        }

        var newFile = serviceProvider.GetService<File<T>>();
        newFile.ParentId = folderId;
        newFile.Title = fileName;

        return newFile;
    }

    private async Task<File<T>> VerifyFileUploadAsync<T>(T folderId, string fileName, long fileSize, bool updateIfExists)
    {
        if (fileSize <= 0)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_EmptyFile);
        }

        var maxUploadSize = await GetMaxFileSizeAsync(folderId);

        if (fileSize > maxUploadSize)
        {
            throw FileSizeComment.GetFileSizeException(maxUploadSize);
        }

        var file = await VerifyFileUploadAsync(folderId, fileName, updateIfExists);
        file.ContentLength = fileSize;

        return file;
    }

    private async Task<bool> CanEditAsync<T>(File<T> file)
    {
        return file != null
               && await fileSecurity.CanEditAsync(file)
               && !await userManager.IsGuestAsync(authContext.CurrentAccount.ID)
               && !await lockerManager.FileLockedForMeAsync(file.Id)
               && !await fileTracker.IsEditingAsync(file.Id)
               && file.RootFolderType != FolderType.TRASH
               && !file.Encrypted;
    }

    private async Task<T> GetFolderIdAsync<T>(T folderId, IList<string> relativePath)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var folder = await folderDao.GetFolderAsync(folderId);

        if (folder == null)
        {
            throw new DirectoryNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (folder.FolderType == FolderType.VirtualRooms || folder.FolderType == FolderType.Archive || folder.FolderType == FolderType.RoomTemplates || !await fileSecurity.CanCreateAsync(folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        if (relativePath is { Count: > 0 })
        {
            var subFolderTitle = Global.ReplaceInvalidCharsAndTruncate(relativePath.FirstOrDefault());

            if (!string.IsNullOrEmpty(subFolderTitle))
            {
                folder = await folderDao.GetFolderAsync(subFolderTitle, folder.Id);

                if (folder == null)
                {
                    var newFolder = serviceProvider.GetService<Folder<T>>();
                    newFolder.Title = subFolderTitle;
                    newFolder.ParentId = folderId;

                    folderId = await folderDao.SaveFolderAsync(newFolder);
                    folder = await folderDao.GetFolderAsync(folderId);
                    await socketManager.CreateFolderAsync(folder);
                    await filesMessageService.SendAsync(MessageAction.FolderCreated, folder, folder.Title);
                }

                folderId = folder.Id;

                relativePath.RemoveAt(0);
                folderId = await GetFolderIdAsync(folderId, relativePath);
            }
        }

        return folderId;
    }

    #region chunked upload

    public async Task<File<T>> VerifyChunkedUploadAsync<T>(T folderId, string fileName, long fileSize, bool updateIfExists, string relativePath = null)
    {
        var maxUploadSize = await GetMaxFileSizeAsync(folderId, true);

        if (fileSize > maxUploadSize)
        {
            throw FileSizeComment.GetFileSizeException(maxUploadSize);
        }

        var file = await VerifyFileUploadAsync(folderId, fileName, updateIfExists, relativePath);
        file.ContentLength = fileSize;

        return file;
    }

    public async Task<File<T>> VerifyChunkedUploadForEditing<T>(T fileId, long fileSize)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new FileNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var maxUploadSize = await GetMaxFileSizeAsync(file.ParentId, true);

        if (fileSize > maxUploadSize)
        {
            throw FileSizeComment.GetFileSizeException(maxUploadSize);
        }

        if (!await CanEditAsync(file))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        file.ConvertedType = null;
        file.Comment = FilesCommonResource.CommentUpload;
        file.Encrypted = false;
        file.ThumbnailStatus = Thumbnail.Waiting;

        file.ContentLength = fileSize;

        return file;
    }

    public async Task<ChunkedUploadSession<T>> InitiateUploadAsync<T>(T folderId, T fileId, string fileName, long contentLength, bool encrypted, bool keepVersion = false, ApiDateTime createOn = null)
    {
        var file = serviceProvider.GetService<File<T>>();
        file.Id = fileId;
        file.ParentId = folderId;
        file.Title = fileName;
        file.ContentLength = contentLength;
        file.CreateOn = createOn;

        var dao = daoFactory.GetFileDao<T>();
        var uploadSession = await dao.CreateUploadSessionAsync(file, contentLength);

        uploadSession.Expired = uploadSession.Created + ChunkedUploadSessionHolder.SlidingExpiration;
        uploadSession.Location = filesLinkUtility.GetUploadChunkLocationUrl(uploadSession.Id);
        uploadSession.TenantId = tenantManager.GetCurrentTenantId();
        uploadSession.UserId = authContext.CurrentAccount.ID;
        uploadSession.FolderId = folderId;
        uploadSession.CultureName = CultureInfo.CurrentUICulture.Name;
        uploadSession.Encrypted = encrypted;
        uploadSession.KeepVersion = keepVersion;
        
        await chunkedUploadSessionHolder.StoreSessionAsync(uploadSession);

        return uploadSession;
    }

    public async Task<ChunkedUploadSession<T>> UploadChunkAsync<T>(string uploadId, Stream stream, long chunkLength, int? chunkNumber = null)
    {
        var uploadSession = await chunkedUploadSessionHolder.GetSessionAsync<T>(uploadId);
        uploadSession.Expired = DateTime.UtcNow + ChunkedUploadSessionHolder.SlidingExpiration;

        if (chunkLength <= 0)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_EmptyFile);
        }
        if (chunkLength > setupInfo.ChunkUploadSize)
        {
            throw FileSizeComment.GetFileSizeException(await setupInfo.MaxUploadSize(tenantManager, maxTotalSizeStatistic));
        }

        var fileType = FileUtility.GetFileTypeByFileName(uploadSession.File.Title);
        var dao = daoFactory.GetFileDao<T>();

        if (fileType == FileType.Pdf)
        {
            var isFirstChunk = false;
            if (!chunkNumber.HasValue)
            {
                int.TryParse(uploadSession.GetItemOrDefault<string>("ChunkNumber"), out var number);
                if (number == 0)
                {
                    isFirstChunk = true;
                }
                number++;
                uploadSession.Items["ChunkNumber"] = number.ToString();
            }
            else if (chunkNumber == 1)
            {
                isFirstChunk = true;
            }

            if (isFirstChunk)
            {
                var folderDao = daoFactory.GetFolderDao<T>();
                var currentFolder = await folderDao.GetFolderAsync(uploadSession.File.FolderIdDisplay);
                var (roomId, _) = await folderDao.GetParentRoomInfoFromFileEntryAsync(currentFolder);

                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                bool isForm;
                var cloneStreamForCheck = await tempStream.CloneMemoryStream(memoryStream, 300);
                try
                {
                    isForm = await fileChecker.CheckExtendedPDFstream(cloneStreamForCheck);
                }
                finally
                {
                    await cloneStreamForCheck.DisposeAsync();
                }

                uploadSession.File.Category = isForm ? (int)FilterType.PdfForm : (int)FilterType.Pdf;

                if (int.TryParse(roomId?.ToString(), out var curRoomId) && curRoomId != -1)
                {
                    var currentRoom = await folderDao.GetFolderAsync(roomId);
                    if (currentRoom.FolderType == FolderType.FillingFormsRoom && !isForm)
                    {
                        var logoText = await tenantLogoManager.GetLogoTextAsync();

                        throw new Exception(string.Format(FilesCommonResource.ErrorMessage_UploadToFormRoom, logoText));
                    }
                }

                var cloneStreamForSave = await tempStream.CloneMemoryStream(memoryStream);
                try
                {
                    await dao.UploadChunkAsync(uploadSession, cloneStreamForSave, chunkLength, chunkNumber);
                }
                finally
                {
                    await memoryStream.DisposeAsync();
                    await cloneStreamForSave.DisposeAsync();
                }

                return uploadSession;
            }
        }


        await dao.UploadChunkAsync(uploadSession, stream, chunkLength, chunkNumber);

        return uploadSession;
    }
    
    public async Task<ChunkedUploadSession<T>> FinalizeUploadSessionAsync<T>(string uploadId)
    {
        var uploadSession = await chunkedUploadSessionHolder.GetSessionAsync<T>(uploadId);
        var dao = daoFactory.GetFileDao<T>();

        uploadSession.File = await dao.FinalizeUploadSessionAsync(uploadSession);
        
        await chunkedUploadSessionHolder.RemoveSessionAsync(uploadSession);

        return uploadSession;
    }

    public async Task DeleteLinkAndMarkAsync<T>(File<T> file)
    {
        var linkDao = daoFactory.GetLinkDao<T>();
        
        var t1 = linkDao.DeleteAllLinkAsync(file.Id);
        var t2 = fileMarker.MarkAsNewAsync(file).AsTask();

        await Task.WhenAll(t1, t2);
    }

    public async Task AbortUploadAsync<T>(string uploadId)
    {
        await AbortUploadAsync(await chunkedUploadSessionHolder.GetSessionAsync<T>(uploadId));
    }
    
    public Task<long> GetTransferredBytesCountAsync<T>(ChunkedUploadSession<T> uploadSession)
    {
        var dao = daoFactory.GetFileDao<T>();

        return dao.GetTransferredBytesCountAsync(uploadSession);
    }

    public async Task<object> CreateUploadSessionAsync<T>(File<T> file, bool encrypted, ApiDateTime createOn, bool keepVersion = false)
    {
        if (filesLinkUtility.IsLocalFileUploader)
        {
            var session = await InitiateUploadAsync(file.ParentId, file.Id ?? default, file.Title, file.ContentLength, encrypted, keepVersion, createOn);

            var responseObject = await chunkedUploadSessionHelper.ToResponseObjectAsync(session, true);

            return new
            {
                success = true,
                data = responseObject
            };
        }

        var createSessionUrl = await filesLinkUtility.GetInitiateUploadSessionUrlAsync(tenantManager.GetCurrentTenantId(), file.ParentId, file.Id, file.Title, file.ContentLength, encrypted, securityContext);

        var httpClient = clientFactory.CreateClient();

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
    
    public async Task<List<string>> CheckUploadAsync<T>(T folderId, IEnumerable<string> filesTitle)
    {
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
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
    
    public async Task<List<File<T>>> UploadFileAsync<T>(T folderId, UploadRequestDto uploadModel)
    {
        if (uploadModel.StoreOriginalFileFlag.HasValue)
        {
            await filesSettingsHelper.SetStoreOriginalFiles(uploadModel.StoreOriginalFileFlag.Value);
        }

        IEnumerable<IFormFile> files = httpContextAccessor.HttpContext?.Request.Form.Files;
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

                return [await InsertFileAsync(folderId, postedFile.OpenReadStream(), postedFile.FileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus)];
            }

            //For case with multiple files
            var result = new List<File<T>>();

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

            return [await InsertFileAsync(folderId, uploadModel.File.OpenReadStream(), fileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus)];
        }

        throw new InvalidOperationException("No input files");
    }
    
    private async Task AbortUploadAsync<T>(ChunkedUploadSession<T> uploadSession)
    {
        await daoFactory.GetFileDao<T>().AbortUploadSessionAsync(uploadSession);

        await chunkedUploadSessionHolder.RemoveSessionAsync(uploadSession);
    }

    private async Task<long> GetMaxFileSizeAsync<T>(T folderId, bool chunkedUpload = false)
    {
        var folderDao = daoFactory.GetFolderDao<T>();

        return await folderDao.GetMaxUploadSizeAsync(folderId, chunkedUpload);
    }

    #endregion
}
