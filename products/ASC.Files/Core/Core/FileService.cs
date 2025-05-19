namespace ASC.Files.Core;

/// <summary>
/// Provides a comprehensive set of methods for managing file operations such as retrieving, creating, updating, renaming,
/// and restoring file versions within the system. The service also handles file versioning and history management.
/// </summary>
[Scope]
public class FileService(
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
    ILogger<FileService> logger,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    PathProvider pathProvider,
    LockerManager lockerManager,
    FilesLinkUtility filesLinkUtility,
    SettingsManager settingsManager,
    EncryptionKeyPairDtoHelper encryptionKeyPairHelper,
    SharingService sharingService,
    MentionWrapperCreator mentionWrapperCreator,
    ExternalShare externalShare,
    IEventBus eventBus,
    TenantManager tenantManager,
    BaseCommonLinkUtility baseCommonLinkUtility)
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

    /// <summary>
    /// Restores a specific version of a file and retrieves the edit history of the restored version.
    /// </summary>
    /// <typeparam name="T">Type of the file ID</typeparam>
    /// <param name="fileId">The ID of the file to restore</param>
    /// <param name="version">The version number to restore</param>
    /// <param name="url">Optional URL parameter for restoring the file remotely</param>
    /// <returns>A stream of edit history entries for the restored file</returns>
    public async IAsyncEnumerable<EditHistory> RestoreVersionAsync<T>(T fileId, int version, string url = null)
    {
        File<T> file;
        if (string.IsNullOrEmpty(url))
        {
            file = await entryManager.UpdateToVersionFileAsync(fileId, version);
        }
        else
        {
            var fileDao = daoFactory.GetFileDao<T>();
            var fromFile = await fileDao.GetFileAsync(fileId, version);
            var modifiedOnString = fromFile.ModifiedOnString;
            file = await entryManager.SaveEditingAsync(fileId, null, url, null, string.Format(FilesCommonResource.CommentRevertChanges, modifiedOnString));
        }

        await filesMessageService.SendAsync(MessageAction.FileRestoreVersion, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        await foreach (var f in daoFactory.GetFileDao<T>().GetEditHistoryAsync(documentServiceHelper, file.Id))
        {
            yield return f;
        }
    }

    /// <summary>
    /// Updates the specified file to the given version asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of the file identifier.</typeparam>
    /// <param name="fileId">The identifier of the file to update.</param>
    /// <param name="version">The version number to which the file should be updated.</param>
    /// <returns>A key-value pair containing the updated file and an asynchronous enumerable of its version history.</returns>
    public async Task<KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>> UpdateToVersionAsync<T>(T fileId, int version)
    {
        var file = await entryManager.UpdateToVersionFileAsync(fileId, version);
        await filesMessageService.SendAsync(MessageAction.FileRestoreVersion, file, file.Title, version.ToString(CultureInfo.InvariantCulture));

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return new KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>(file, GetFileHistoryAsync(fileId));
    }

    /// <summary>
    /// Finalizes the versioning process for the specified file and optionally continues the version chain.
    /// </summary>
    /// <typeparam name="T">The type of the file ID.</typeparam>
    /// <param name="fileId">The ID of the file whose version is to be completed.</param>
    /// <param name="version">The version of the file to complete.</param>
    /// <param name="continueVersion">Indicates whether to continue the version chain.</param>
    /// <returns>A key-value pair containing the completed file and an asynchronous enumerable of its version history.</returns>
    public async Task<KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>> CompleteVersionAsync<T>(T fileId, int version, bool continueVersion)
    {
        var file = await entryManager.CompleteVersionFileAsync(fileId, version, continueVersion);

        await filesMessageService.SendAsync(
            continueVersion ? MessageAction.FileDeletedVersion : MessageAction.FileCreatedVersion,
            file,
            file.Title, version == 0 ? (file.Version - 1).ToString(CultureInfo.InvariantCulture) : version.ToString(CultureInfo.InvariantCulture));

        if (file.RootFolderType == FolderType.USER
            && !Equals(file.RootCreateBy, authContext.CurrentAccount.ID))
        {
            var folderDao = daoFactory.GetFolderDao<T>();
            if (!await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.ParentId)))
            {
                file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<T>();
            }
        }

        await socketManager.UpdateFileAsync(file);

        return new KeyValuePair<File<T>, IAsyncEnumerable<File<T>>>(file, GetFileHistoryAsync(fileId));
    }

    /// <summary>
    /// Retrieves the version history of a file with the specified ID.
    /// </summary>
    /// <typeparam name="T">Type of the file ID.</typeparam>
    /// <param name="fileId">The ID of the file for which the version history is retrieved.</param>
    /// <returns>An asynchronous enumerable containing the file's version history.</returns>
    public async IAsyncEnumerable<File<T>> GetFileHistoryAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);
        if (!await fileSecurity.CanReadHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        var history = await fileDao.GetFileHistoryAsync(fileId).ToListAsync();

        var t1 = entryStatusManager.SetFileStatusAsync(history);
        var t2 = entryStatusManager.SetFormInfoAsync(history);
        await Task.WhenAll(t1, t2);

        foreach (var r in history)
        {
            yield return r;
        }
    }

    /// <summary>
    /// Locks or unlocks a file based on the specified parameters.
    /// </summary>
    /// <typeparam name="T">Type of file ID.</typeparam>
    /// <param name="fileId">The ID of the file to lock or unlock.</param>
    /// <param name="lockfile">A boolean value specifying whether to lock (true) or unlock (false) the file.</param>
    /// <returns>The locked or unlocked file entry.</returns>
    public async Task<File<T>> LockFileAsync<T>(T fileId, bool lockfile)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanLockAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        var tags = tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.Locked);
        var tagLocked = await tags.FirstOrDefaultAsync();

        if (tagLocked != null)
        {
            if (tagLocked.Owner != authContext.CurrentAccount.ID
                && file.Access != FileShare.RoomManager
                && !await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
            }
        }

        if (lockfile)
        {
            if (tagLocked == null)
            {
                tagLocked = new Tag("locked", TagType.Locked, authContext.CurrentAccount.ID).AddEntry(file);

                await tagDao.SaveTagsAsync(tagLocked);
            }

            var usersDrop = (await fileTracker.GetEditingByAsync(file.Id)).Where(uid => uid != authContext.CurrentAccount.ID).Select(u => u.ToString()).ToArray();
            if (usersDrop.Length > 0)
            {
                var docKey = await fileTracker.GetTrackerDocKey(file.Id);
                await documentServiceHelper.DropUserAsync(docKey, usersDrop, file.Id);
            }

            await filesMessageService.SendAsync(MessageAction.FileLocked, file, file.Title);
        }
        else
        {
            if (tagLocked != null)
            {
                await tagDao.RemoveTagsAsync(tagLocked);

                await filesMessageService.SendAsync(MessageAction.FileUnlocked, file, file.Title);
            }

            if (!file.ProviderEntry)
            {
                await UpdateCommentAsync(file.Id, file.Version, FilesCommonResource.UnlockComment);
            }
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

        await socketManager.UpdateFileAsync(file);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return file;
    }


    /// <summary>
    /// Enables or disables a custom filter tag for a specified file.
    /// </summary>
    /// <typeparam name="T">Type of the file ID.</typeparam>
    /// <param name="fileId">The identifier of the file for which the custom filter tag is to be updated.</param>
    /// <param name="enabled">Indicates whether the custom filter tag should be enabled or disabled.</param>
    /// <returns>The updated file with the applied changes to the custom filter tag.</returns>
    public async Task<File<T>> SetCustomFilterTagAsync<T>(T fileId, bool enabled)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        file.NotFoundIfNull();

        if (file.RootFolderType != FolderType.VirtualRooms || !fileUtility.CanWebCustomFilterEditing(file.Title))
        {
            throw new ArgumentException();
        }

        var folderDao = daoFactory.GetFolderDao<T>();
        var room = await DocSpaceHelper.GetParentRoom(file, folderDao);

        if (room == null || !await fileSecurity.CanEditAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var tagCustomFilter = await tagDao.GetTagsAsync(file.Id, FileEntryType.File, TagType.CustomFilter).FirstOrDefaultAsync();

        if (tagCustomFilter != null)
        {
            if (tagCustomFilter.Owner != authContext.CurrentAccount.ID
                && file.Access != FileShare.RoomManager
                && !await userManager.IsDocSpaceAdminAsync(authContext.CurrentAccount.ID))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
            }
        }

        if (enabled)
        {
            if (tagCustomFilter == null)
            {
                tagCustomFilter = new Tag("customfilter", TagType.CustomFilter, authContext.CurrentAccount.ID).AddEntry(file);

                await tagDao.SaveTagsAsync(tagCustomFilter);

                var usersDrop = (await fileTracker.GetEditingByAsync(file.Id)).Where(uid => uid != authContext.CurrentAccount.ID).Select(u => u.ToString()).ToArray();
                if (usersDrop.Length > 0)
                {
                    var docKey = await fileTracker.GetTrackerDocKey(file.Id);
                    await documentServiceHelper.DropUserAsync(docKey, usersDrop, file.Id);
                }
            }

            await filesMessageService.SendAsync(MessageAction.FileCustomFilterEnabled, file, file.Title);
        }
        else
        {
            if (tagCustomFilter != null)
            {
                await tagDao.RemoveTagsAsync(tagCustomFilter);
            }

            await filesMessageService.SendAsync(MessageAction.FileCustomFilterDisabled, file, file.Title);
        }

        await entryStatusManager.SetFileStatusAsync(file);

        await socketManager.UpdateFileAsync(file);

        return file;
    }

    /// <summary>
    /// Generates a presigned URI for the specified file.
    /// </summary>
    /// <typeparam name="T">Type of the file ID</typeparam>
    /// <param name="fileId">The ID of the file for which the presigned URI is generated</param>
    /// <returns>A <see cref="DocumentService.FileLink"/> containing the file type and the presigned URL</returns>
    public async Task<FileLink> GetPresignedUriAsync<T>(T fileId)
    {
        var file = await GetFileAsync(fileId, -1);
        var result = new FileLink
        {
            FileType = FileUtility.GetFileExtension(file.Title), 
            Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file))
        };

        result.Token = documentServiceHelper.GetSignature(result);

        return result;
    }

    /// <summary>
    /// Updates the comment for a specific file version in the data storage.
    /// </summary>
    /// <typeparam name="T">Type of the file ID.</typeparam>
    /// <param name="fileId">The unique identifier of the file.</param>
    /// <param name="version">The version of the file for which the comment will be updated.</param>
    /// <param name="comment">The new comment to be associated with the specified file version.</param>
    /// <returns>The updated comment for the specified file version.</returns>
    public async Task<string> UpdateCommentAsync<T>(T fileId, int version, string comment)
    {
        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId, version);
        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        if (!await fileSecurity.CanEditHistoryAsync(file))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_EditFile);
        }

        if (await lockerManager.FileLockedForMeAsync(file.Id))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_LockedFile);
        }

        if (file.RootFolderType == FolderType.TRASH)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_ViewTrashItem);
        }

        comment = await fileDao.UpdateCommentAsync(fileId, version, comment);

        await filesMessageService.SendAsync(MessageAction.FileUpdatedRevisionComment, file, [file.Title, version.ToString(CultureInfo.InvariantCulture)]);

        await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);

        return comment;
    }

    /// <summary>
    /// Saves the editing changes of a file to the storage.
    /// </summary>
    /// <typeparam name="T">Type of file ID</typeparam>
    /// <param name="fileId">The unique identifier of the file being edited.</param>
    /// <param name="fileExtension">The extension of the file being saved, such as ".docx" or ".pdf".</param>
    /// <param name="fileUri">The URI of the file's current state.</param>
    /// <param name="stream">The stream containing the updated file content.</param>
    /// <param name="forceSave">Specifies whether the save operation should be forced regardless of the editing context.</param>
    /// <returns>The file entry after it is saved, including updated metadata and content.</returns>
    public async Task<File<T>> SaveEditingAsync<T>(T fileId, string fileExtension, string fileUri, Stream stream, bool forceSave = false)
    {
        try
        {
            if (!forceSave && await fileTracker.IsEditingAloneAsync(fileId))
            {
                await fileTracker.RemoveAsync(fileId);
                await socketManager.StopEditAsync(fileId);
            }

            var file = await entryManager.SaveEditingAsync(fileId, fileExtension, fileUri, stream, forceSave: forceSave ? ForcesaveType.User : ForcesaveType.None, keepLink: true);

            if (file != null)
            {
                await filesMessageService.SendAsync(MessageAction.FileUpdated, file, file.Title);

                await webhookManager.PublishAsync(WebhookTrigger.FileUpdated, file);
            }

            return file;
        }
        catch (Exception e)
        {
            throw GenerateException(e);
        }
    }
    
    public async Task<List<AceShortWrapper>> SendEditorNotifyAsync<T>(T fileId, MentionMessageWrapper mentionMessage)
    {
        if (!authContext.IsAuthenticated)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var canRead = await fileSecurity.CanReadAsync(file);

        if (!canRead)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_ReadFile);
        }

        if (mentionMessage?.Emails == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_BadRequest);
        }

        var showSharingSettings = false;
        bool? canShare = null;
        if (file.Encrypted)
        {
            canShare = false;
            showSharingSettings = true;
        }


        var recipients = new List<Guid>();
        foreach (var email in mentionMessage.Emails)
        {
            canShare ??= await fileSharing.CanSetAccessAsync(file);

            var recipient = await userManager.GetUserByEmailAsync(email);
            if (recipient == null || recipient.Id == Constants.LostUser.Id)
            {
                showSharingSettings = canShare.Value;
                continue;
            }

            recipients.Add(recipient.Id);
        }

        var fileLink = filesLinkUtility.GetFileWebEditorUrl(file.Id);
        if (mentionMessage.ActionLink != null)
        {
            fileLink += "&" + FilesLinkUtility.Anchor + "=" + HttpUtility.UrlEncode(ActionLinkConfig.Serialize(mentionMessage.ActionLink));
        }

        var message = (mentionMessage.Message ?? "").Trim();
        const int maxMessageLength = 200;
        if (message.Length > maxMessageLength)
        {
            message = message[..maxMessageLength] + "...";
        }

        try
        {
            await notifyClient.SendEditorMentions(file, fileLink, recipients, message);
        }
        catch (Exception ex)
        {
            logger.ErrorWithException(ex);
        }

        return showSharingSettings ? await fileSharing.GetSharedInfoShortFileAsync(file) : null;
    }
    
    public async Task<List<EncryptionKeyPairDto>> GetEncryptionAccessAsync<T>(T fileId)
    {
        if (!await PrivacyRoomSettings.GetEnabledAsync(settingsManager))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var fileKeyPair = await encryptionKeyPairHelper.GetKeyPairAsync(fileId, sharingService);

        return [..fileKeyPair];
    }

    public async Task<IEnumerable<JsonElement>> CreateThumbnailsAsync(List<JsonElement> fileIds)
    {
        if (!authContext.IsAuthenticated && (await externalShare.GetLinkIdAsync()) == Guid.Empty)
        {
            throw FileStorageService.GenerateException(new SecurityException(FilesCommonResource.ErrorMessage_SecurityException),  logger, authContext);
        }

        try
        {
            var (fileIntIds, _) = FileOperationsManager.GetIds(fileIds);

            await eventBus.PublishAsync(new ThumbnailRequestedIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenantId()) { BaseUrl = baseCommonLinkUtility.GetFullAbsolutePath(""), FileIds = fileIntIds });
        }
        catch (Exception e)
        {
            logger.ErrorCreateThumbnails(e);
        }

        return fileIds;
    }

    public async Task<List<MentionWrapper>> ProtectUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated)
        {
            return null;
        }

        var fileDao = daoFactory.GetFileDao<T>();
        var file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var users = new List<MentionWrapper>();
        if (file.RootFolderType == FolderType.BUNCH)
        {
            //todo: request project team
            return [..users];
        }

        var acesForObject = await fileSharing.GetSharedInfoAsync(file);

        var usersInfo = new List<UserInfo>();
        foreach (var ace in acesForObject)
        {
            if (ace.Access == FileShare.Restrict)
            {
                continue;
            }

            if (ace.SubjectGroup)
            {
                usersInfo.AddRange(await userManager.GetUsersByGroupAsync(ace.Id));
            }
            else
            {
                usersInfo.Add(await userManager.GetUsersAsync(ace.Id));
            }
        }

        users = await usersInfo.Distinct()
            .Where(user => !user.Id.Equals(authContext.CurrentAccount.ID)
                           && !user.Id.Equals(Constants.LostUser.Id))
            .ToAsyncEnumerable()
            .SelectAwait(async user => await mentionWrapperCreator.CreateMentionWrapperAsync(user))
            .ToListAsync();

        users = users
            .OrderBy(user => user.User, UserInfoComparer.Default)
            .ToList();

        return [..users];
    }
    
    public Task<List<MentionWrapper>> SharedUsersAsync<T>(T fileId)
    {
        if (!authContext.IsAuthenticated)
        {
            return Task.FromResult<List<MentionWrapper>>(null);
        }

        return InternalSharedUsersAsync(fileId);
    }
    
    public async Task<List<MentionWrapper>> GetInfoUsersAsync(List<Guid> userIds)
    {
        if (!authContext.IsAuthenticated)
        {
            return null;
        }

        var users = new List<MentionWrapper>();

        foreach (var uid in userIds)
        {
            var user = await userManager.GetUsersAsync(uid);
            if (user.Id.Equals(Constants.LostUser.Id))
            {
                continue;
            }

            users.Add(await mentionWrapperCreator.CreateMentionWrapperAsync(user));
        }

        return users;
    }
    
    private async Task<List<MentionWrapper>> InternalSharedUsersAsync<T>(T fileId)
    {
        var fileDao = daoFactory.GetFileDao<T>();

        FileEntry<T> file = await fileDao.GetFileAsync(fileId);

        if (file == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FileNotFound);
        }
        
        var usersIdWithAccess = await WhoCanRead(file);
        var links = await fileSecurity.GetPureSharesAsync(file, ShareFilterType.Link, null, null)
            .Select(x => x.Subject).ToHashSetAsync();

        var users = usersIdWithAccess
            .Where(id => !id.Equals(authContext.CurrentAccount.ID) && !links.Contains(id))
            .Select(userManager.GetUsers);

        var result = await users
            .Where(u => u.Status != EmployeeStatus.Terminated)
            .ToAsyncEnumerable()
            .SelectAwait(async u => await mentionWrapperCreator.CreateMentionWrapperAsync(u))
            .OrderBy(u => u.User, UserInfoComparer.Default)
            .ToListAsync();

        return result;
    }

    private async Task<List<Guid>> WhoCanRead<T>(FileEntry<T> entry)
    {
        var whoCanReadTask = (await fileSecurity.WhoCanReadAsync(entry, true)).ToList();
        whoCanReadTask.AddRange((await userManager.GetUsersByGroupAsync(Constants.GroupAdmin.ID))
            .Select(x => x.Id));

        whoCanReadTask.Add((tenantManager.GetCurrentTenant()).OwnerId);

        var userIds = whoCanReadTask
            .Concat([entry.CreateBy])
            .Distinct()
            .ToList();

        return userIds;
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