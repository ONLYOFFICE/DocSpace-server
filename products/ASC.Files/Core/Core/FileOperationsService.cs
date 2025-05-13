namespace ASC.Files.Core;

public class FileOperationsService(
    Global global,
    GlobalStore globalStore,
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    FileUtility fileUtility,
    FileSecurity fileSecurity,
    SocketManager socketManager,
    IDaoFactory daoFactory,
    FileMarker fileMarker,
    EntryManager entryManager,
    FilesMessageService filesMessageService,
    FileSharing fileSharing,
    NotifyClient notifyClient,
    IServiceProvider serviceProvider,
    FileTrackerHelper fileTracker,
    EntryStatusManager entryStatusManager,
    OFormRequestManager oFormRequestManager,
    ThumbnailSettings thumbnailSettings,
    TempStream tempStream,
    FileChecker fileChecker,
    WebhookManager webhookManager,
    ILogger<FileOperationsService> logger)
{
    /// <summary>
    /// Retrieves a file with the specified ID and version
    /// </summary>
    /// <typeparam name="T">Type of file ID</typeparam>
    /// <param name="fileId">File ID</param>
    /// <param name="version">File version</param>
    /// <returns>File entry</returns>
    public async Task<File<T>> GetFileAsync<T>(T fileId, int version)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        await fileDao.InvalidateCacheAsync(fileId);

        var file = version > 0
            ? await fileDao.GetFileAsync(fileId, version)
            : await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        await entryStatusManager.SetFileStatusAsync(file);

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

        if (fileType == FileType.Pdf)
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            var parent = await folderDao.GetFolderAsync(file.ParentId);

            if (parent?.FolderType == FolderType.FillingFormsRoom)
            {
                var ace = await fileSharing.GetPureSharesAsync(parent, new List<Guid> { authContext.CurrentAccount.ID }).FirstOrDefaultAsync();
                if (ace is { Access: FileShare.FillForms })
                {
                    var properties = await daoFactory.GetFileDao<T>().GetProperties(file.Id);
                    if (properties == null || !properties.FormFilling.StartFilling)
                    {
                        return null;
                    }
                }
            }
        }

        return file;
    }

    /// <summary>
    /// Creates a new file based on the provided file model
    /// </summary>
    /// <typeparam name="T">Type of file ID</typeparam>
    /// <typeparam name="TTemplate">Type of template ID</typeparam>
    /// <param name="fileWrapper">File model wrapper</param>
    /// <param name="enableExternalExt">Whether to enable external extensions</param>
    /// <returns>Created file entry</returns>
    public async ValueTask<File<T>> CreateNewFileAsync<T, TTemplate>(FileModel<T, TTemplate> fileWrapper, bool enableExternalExt = false)
    {
        if (string.IsNullOrEmpty(fileWrapper.Title) || fileWrapper.ParentId == null)
        {
            throw new ArgumentException();
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var folderDao = daoFactory.GetFolderDao<T>();

        Folder<T> folder = null;
        if (!EqualityComparer<T>.Default.Equals(fileWrapper.ParentId, default))
        {
            folder = await folderDao.GetFolderAsync(fileWrapper.ParentId);
            var canCreate = await fileSecurity.CanCreateAsync(folder) && folder.FolderType != FolderType.VirtualRooms
                                                                      && folder.FolderType != FolderType.RoomTemplates
                                                                      && folder.FolderType != FolderType.Archive;

            if (!canCreate)
            {
                folder = null;
            }
        }

        folder ??= await folderDao.GetFolderAsync(await globalFolderHelper.GetFolderMyAsync<T>());

        var file = serviceProvider.GetService<File<T>>();
        file.ParentId = folder.Id;
        file.Comment = FilesCommonResource.CommentCreate;

        if (string.IsNullOrEmpty(fileWrapper.Title))
        {
            fileWrapper.Title = UserControlsCommonResource.NewDocument + ".docx";
        }

        var title = fileWrapper.Title;
        var fileExt = FileUtility.GetFileExtension(title);
        if (!enableExternalExt && fileExt != fileUtility.MasterFormExtension)
        {
            fileExt = fileUtility.GetInternalExtension(title);
            if (!fileUtility.InternalExtension.ContainsValue(fileExt))
            {
                fileExt = fileUtility.InternalExtension[FileType.Document];
                file.Title = title + fileExt;
            }
            else
            {
                file.Title = FileUtility.ReplaceFileExtension(title, fileExt);
            }
        }
        else
        {
            file.Title = FileUtility.ReplaceFileExtension(title, fileExt);
        }

        if (fileWrapper.FormId != 0)
        {
            await using var stream = await oFormRequestManager.Get(fileWrapper.FormId);
            file.ContentLength = stream.Length;

            if (FileUtility.GetFileTypeByExtention(fileExt) == FileType.Pdf)
            {
                var (cloneStreamForCheck, cloneStreamForSave) = await GetCloneMemoryStreams(stream);
                try
                {
                    file.Category = await fileChecker.CheckExtendedPDFstream(cloneStreamForCheck) ? (int)FilterType.PdfForm : (int)FilterType.Pdf;
                    file = await fileDao.SaveFileAsync(file, cloneStreamForSave);
                }
                finally
                {
                    await cloneStreamForCheck.DisposeAsync();
                    await cloneStreamForSave.DisposeAsync();
                }
            }
            else
            {
                file = await fileDao.SaveFileAsync(file, stream);
            }
        }
        else if (EqualityComparer<TTemplate>.Default.Equals(fileWrapper.TemplateId, default))
        {
            var culture = (await userManager.GetUsersAsync(authContext.CurrentAccount.ID)).GetCulture();
            var storeTemplate = await globalStore.GetStoreTemplateAsync();
            var pathNew = await globalStore.GetNewDocTemplatePath(storeTemplate, fileExt, culture);

            try
            {
                file.ThumbnailStatus = Thumbnail.Creating;

                if (!enableExternalExt)
                {
                    await using var stream = await storeTemplate.GetReadStreamAsync("", pathNew, 0);
                    file.ContentLength = stream.CanSeek ? stream.Length : await storeTemplate.GetFileSizeAsync(pathNew);

                    if (FileUtility.GetFileTypeByExtention(fileExt) == FileType.Pdf)
                    {
                        var (cloneStreamForCheck, cloneStreamForSave) = await GetCloneMemoryStreams(stream);
                        try
                        {
                            file.Category = await fileChecker.CheckExtendedPDFstream(cloneStreamForCheck) ? (int)FilterType.PdfForm : (int)FilterType.Pdf;
                            file = await fileDao.SaveFileAsync(file, cloneStreamForSave);
                        }
                        finally
                        {
                            await cloneStreamForCheck.DisposeAsync();
                            await cloneStreamForSave.DisposeAsync();
                        }
                    }
                    else
                    {
                        file = await fileDao.SaveFileAsync(file, stream);
                    }
                }
                else
                {
                    file = await fileDao.SaveFileAsync(file, null);
                }

                var counter = 0;

                var path = pathNew.Replace(Path.GetFileName(pathNew), string.Empty);

                foreach (var size in thumbnailSettings.Sizes)
                {
                    var pathThumb = $"{path}{fileExt.Trim('.')}.{size.Width}x{size.Height}.{global.ThumbnailExtension}";

                    if (!await storeTemplate.IsFileAsync("", pathThumb))
                    {
                        break;
                    }

                    await using (var streamThumb = await storeTemplate.GetReadStreamAsync("", pathThumb, 0))
                    {
                        await (await globalStore.GetStoreAsync()).SaveAsync(fileDao.GetUniqThumbnailPath(file, size.Width, size.Height), streamThumb);
                    }

                    counter++;
                }

                if (thumbnailSettings.Sizes.Count() == counter)
                {
                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.Created);

                    file.ThumbnailStatus = Thumbnail.Created;
                }
                else
                {
                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.NotRequired);

                    file.ThumbnailStatus = Thumbnail.NotRequired;
                }
            }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }
        else
        {
            var fileTemlateDao = daoFactory.GetFileDao<TTemplate>();
            var template = await fileTemlateDao.GetFileAsync(fileWrapper.TemplateId);

            if (template == null)
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
            }

            if (!await fileSecurity.CanReadAsync(template))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
            }

            file.ThumbnailStatus = template.ThumbnailStatus == Thumbnail.Created ? Thumbnail.Creating : Thumbnail.Waiting;

            try
            {
                await using (var stream = await fileTemlateDao.GetFileStreamAsync(template))
                {
                    file.ContentLength = template.ContentLength;

                    if (FileUtility.GetFileTypeByExtention(fileExt) == FileType.Pdf)
                    {
                        var (cloneStreamForCheck, cloneStreamForSave) = await GetCloneMemoryStreams(stream);
                        try
                        {
                            file.Category = await fileChecker.CheckExtendedPDFstream(cloneStreamForCheck) ? (int)FilterType.PdfForm : (int)FilterType.Pdf;
                            file = await fileDao.SaveFileAsync(file, cloneStreamForSave);
                        }
                        finally
                        {
                            await cloneStreamForCheck.DisposeAsync();
                            await cloneStreamForSave.DisposeAsync();
                        }
                    }
                    else
                    {
                        file = await fileDao.SaveFileAsync(file, stream);
                    }
                }

                if (template.ThumbnailStatus == Thumbnail.Created)
                {
                    foreach (var size in thumbnailSettings.Sizes)
                    {
                        await (await globalStore.GetStoreAsync()).CopyAsync(String.Empty,
                            fileTemlateDao.GetUniqThumbnailPath(template, size.Width, size.Height),
                            String.Empty,
                            fileDao.GetUniqThumbnailPath(file, size.Width, size.Height));
                    }

                    await fileDao.SetThumbnailStatusAsync(file, Thumbnail.Created);

                    file.ThumbnailStatus = Thumbnail.Created;
                }
            }
            catch (Exception e)
            {
                throw GenerateException(e);
            }
        }

        await filesMessageService.SendAsync(MessageAction.FileCreated, file, file.Title);

        await webhookManager.PublishAsync(WebhookTrigger.FileCreated, file);

        var room = await folderDao.GetParentFoldersAsync(folder.Id).FirstOrDefaultAsync(f => DocSpaceHelper.IsRoom(f.FolderType));

        if (file.IsForm && room?.FolderType == FolderType.VirtualDataRoom)
        {
            var users = (await fileSharing.GetSharedInfoAsync(room))
                .Where(ace => ace is not { Access: FileShare.FillForms } && ace.Id != authContext.CurrentAccount.ID)
                .Select(ace => ace.Id)
                .ToList();
            if (users.Count != 0)
            {
                await fileMarker.MarkAsNewAsync(file, users);
            }
        }
        else
        {
            await fileMarker.MarkAsNewAsync(file);
        }
        await socketManager.CreateFileAsync(file);
        if (room != null && !DocSpaceHelper.FormsFillingSystemFolders.Contains(folder.FolderType))
        {
            var whoCanRead = await fileSecurity.WhoCanReadAsync(room, true);
            await notifyClient.SendDocumentCreatedInRoom(room, whoCanRead, file, authContext.CurrentAccount.ID);
        }

        return file;
    }

    /// <summary>
    /// Updates a file's content with the provided stream
    /// </summary>
    /// <typeparam name="T">Type of file ID</typeparam>
    /// <param name="fileId">File ID</param>
    /// <param name="stream">Content stream</param>
    /// <param name="fileExtension">File extension</param>
    /// <param name="encrypted">Whether the file is encrypted</param>
    /// <param name="forcesave">Whether to force save</param>
    /// <returns>Updated file entry</returns>
    public async Task<File<T>> UpdateFileStreamAsync<T>(T fileId, Stream stream, string fileExtension, bool encrypted, bool forcesave)
    {
        try
        {
            if (!forcesave && await fileTracker.IsEditingAsync(fileId))
            {
                await fileTracker.RemoveAsync(fileId);
                await socketManager.StopEditAsync(fileId);
            }

            var file = await entryManager.SaveEditingAsync(fileId,
                fileExtension,
                null,
                stream,
                encrypted ? FilesCommonResource.CommentEncrypted : null,
                encrypted: encrypted,
                forceSave: forcesave ? ForcesaveType.User : ForcesaveType.None);

            if (file != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileUpdated, file, file.Title);
                await socketManager.UpdateFileAsync(file);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
            }

            return file;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }

    /// <summary>
    /// Renames a file with the specified ID
    /// </summary>
    /// <typeparam name="T">Type of file ID</typeparam>
    /// <param name="fileId">File ID</param>
    /// <param name="title">New file title</param>
    /// <returns>Renamed file entry</returns>
    public async Task<File<T>> FileRenameAsync<T>(T fileId, string title)
    {
        try
        {
            var file = await daoFactory.GetFileDao<T>().GetFileAsync(fileId);
            FileOptions<T> result = null;

            var oldTitle = file.Title;

            if (file.MutableId)
            {
                await socketManager.DeleteFileAsync(file, async () =>
                {
                    result = await entryManager.FileRenameAsync(file, title);
                });
            }
            else
            {
                result = await entryManager.FileRenameAsync(file, title);
            }

            file = result.File;

            if (result.Renamed)
            {
                await filesMessageService.SendAsync(MessageAction.FileRenamed, file, file.Title, oldTitle);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

                //if (!file.ProviderEntry)
                //{
                //    FilesIndexer.UpdateAsync(FilesWrapper.GetFilesWrapper(ServiceProvider, file), true, r => r.Title);
                //}
            }

            if (file.RootFolderType == FolderType.USER
                && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
            {
                var folderDao = daoFactory.GetFolderDao<T>();
                if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
                {
                    file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
                }
            }

            if (file.MutableId)
            {
                await socketManager.CreateFileAsync(file);
            }
            else
            {
                await socketManager.UpdateFileAsync(file);
            }

            return file;
        }
        catch (Exception ex)
        {
            throw GenerateException(ex);
        }
    }
    
    private Exception GenerateException(Exception error, bool warning = false)
    {
        if (warning || error is ItemNotFoundException or SecurityException or ArgumentException or TenantQuotaException or InvalidOperationException)
        {
            logger.Information(error.ToString());
        }
        else
        {
            logger.ErrorFileStorageService(error);
        }

        if (error is ItemNotFoundException)
        {
            return !authContext.CurrentAccount.IsAuthenticated
                ? new SecurityException(FilesCommonResource.ErrorMessage_SecurityException)
                : error;
        }

        return new InvalidOperationException(error.Message, error);
    }
    
    private async Task<(MemoryStream, MemoryStream)> GetCloneMemoryStreams(Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return (await tempStream.CloneMemoryStream(memoryStream, 300), await tempStream.CloneMemoryStream(memoryStream));
    }
}