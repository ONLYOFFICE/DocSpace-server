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

namespace ASC.Web.Files.Services.WCFService;

[Scope]
public class FileStorageService //: IFileStorageService
(
    GlobalFolderHelper globalFolderHelper,
    AuthContext authContext,
    UserManager userManager,
    FilesLinkUtility filesLinkUtility,
    BaseCommonLinkUtility baseCommonLinkUtility,
    ILoggerProvider optionMonitor,
    PathProvider pathProvider,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    DocumentServiceHelper documentServiceHelper,
    DocumentServiceConnector documentServiceConnector,
    TenantManager tenantManager,
    EntryStatusManager entryStatusManager,
    MentionWrapperCreator mentionWrapperCreator,
    FileChecker fileChecker,
    CommonLinkUtility commonLinkUtility,
    ShortUrl shortUrl,
    IDbContextFactory<UrlShortenerDbContext> dbContextFactory)
{
    private readonly ILogger _logger = optionMonitor.CreateLogger("ASC.Files");
    
    public async Task<List<FileEntry>> GetItemsAsync<TId>(IEnumerable<TId> filesId, IEnumerable<TId> foldersId, FilterType filter, bool subjectGroup, Guid? subjectId = null, string search = "")
    {
        subjectId ??= Guid.Empty;

        var entries = AsyncEnumerable.Empty<FileEntry<TId>>();

        var folderDao = daoFactory.GetFolderDao<TId>();
        var fileDao = daoFactory.GetFileDao<TId>();

        entries = entries.Concat(fileSecurity.FilterReadAsync(folderDao.GetFoldersAsync(foldersId)));
        entries = entries.Concat(fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)));
        entries = entryManager.FilterEntries(entries, filter, subjectGroup, subjectId.Value, search, true);

        var result = new List<FileEntry>();
        var files = new List<File<TId>>();
        var folders = new List<Folder<TId>>();

        await foreach (var fileEntry in entries)
        {
            if (fileEntry is File<TId> file)
            {
                if (fileEntry.RootFolderType == FolderType.USER
                    && !Equals(fileEntry.RootCreateBy, authContext.CurrentAccount.ID)
                    && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(file.FolderIdDisplay)))
                {
                    file.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<TId>();
                }

                if (!Equals(file.Id, default(TId)))
                {
                    files.Add(file);
                }
            }
            else if (fileEntry is Folder<TId> folder)
            {
                if (fileEntry.RootFolderType == FolderType.USER
                    && !Equals(fileEntry.RootCreateBy, authContext.CurrentAccount.ID)
                    && !await fileSecurity.CanReadAsync(await folderDao.GetFolderAsync(folder.FolderIdDisplay)))
                {
                    folder.FolderIdDisplay = await globalFolderHelper.GetFolderShareAsync<TId>();
                }

                if (!Equals(folder.Id, default(TId)))
                {
                    folders.Add(folder);
                }
            }

            result.Add(fileEntry);
        }

        var setFilesStatus = entryStatusManager.SetFileStatusAsync(files);
        var setFavorites = entryStatusManager.SetIsFavoriteFoldersAsync(folders);

        await Task.WhenAll(setFilesStatus, setFavorites);

        return result;
    }

    #region MoveOrCopy

    public async Task<List<object>> MoveOrCopyDestFolderCheckAsync<T1>(IEnumerable<JsonElement> filesId, T1 destFolderId)
    {
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(filesId);

        var checkedFiles = new List<object>();

        var filesInts = await MoveOrCopyDestFolderCheckAsync(fileIntIds, destFolderId);

        foreach (var i in filesInts)
        {
            checkedFiles.Add(i);
        }

        var filesStrings = await MoveOrCopyDestFolderCheckAsync(fileStringIds, destFolderId);

        foreach (var i in filesStrings)
        {
            checkedFiles.Add(i);
        }

        return checkedFiles;
    }

    public async Task<(List<object>, List<object>)> MoveOrCopyFilesCheckAsync<T1>(IEnumerable<JsonElement> filesId, IEnumerable<JsonElement> foldersId, T1 destFolderId)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(foldersId);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(filesId);

        var checkedFiles = new List<object>();
        var checkedFolders = new List<object>();

        var (filesInts, folderInts) = await MoveOrCopyFilesCheckAsync(fileIntIds, folderIntIds, destFolderId);

        foreach (var i in filesInts)
        {
            checkedFiles.Add(i);
        }

        foreach (var i in folderInts)
        {
            checkedFolders.Add(i);
        }

        var (filesStrings, folderStrings) = await MoveOrCopyFilesCheckAsync(fileStringIds, folderStringIds, destFolderId);

        foreach (var i in filesStrings)
        {
            checkedFiles.Add(i);
        }

        foreach (var i in folderStrings)
        {
            checkedFolders.Add(i);
        }

        return (checkedFiles, checkedFolders);
    }

    private async Task<List<TFrom>> MoveOrCopyDestFolderCheckAsync<TFrom, TTo>(IEnumerable<TFrom> filesId, TTo destFolderId)
    {
        var checkedFiles = new List<TFrom>();

        var destFolderDao = daoFactory.GetFolderDao<TTo>();
        var fileDao = daoFactory.GetFileDao<TFrom>();

        var toRoom = await destFolderDao.GetFolderAsync(destFolderId);

        if (!DocSpaceHelper.IsRoom(toRoom.FolderType))
        {
            var (roomId, _) = await destFolderDao.GetParentRoomInfoFromFileEntryAsync(toRoom);
            toRoom = await destFolderDao.GetFolderAsync(roomId);
        }

        if (toRoom.FolderType == FolderType.FillingFormsRoom)
        {
            foreach (var id in filesId)
            {
                var file = await fileDao.GetFileAsync(id);
                var fileType = FileUtility.GetFileTypeByFileName(file.Title);

                if (fileType == FileType.Pdf && await fileChecker.CheckExtendedPDF(file))
                {
                    checkedFiles.Add(id);
                }
            }
        }
        else
        {
            checkedFiles.AddRange(filesId);
        }

        return checkedFiles;
    }

    private async Task<(List<TFrom>, List<TFrom>)> MoveOrCopyFilesCheckAsync<TFrom, TTo>(IEnumerable<TFrom> filesId, IEnumerable<TFrom> foldersId, TTo destFolderId)
    {
        var checkedFiles = new List<TFrom>();
        var checkedFolders = new List<TFrom>();
        var folderDao = daoFactory.GetFolderDao<TFrom>();
        var fileDao = daoFactory.GetFileDao<TFrom>();
        var destFolderDao = daoFactory.GetFolderDao<TTo>();
        var destFileDao = daoFactory.GetFileDao<TTo>();

        var toFolder = await destFolderDao.GetFolderAsync(destFolderId);
        if (toFolder == null)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (!await fileSecurity.CanCreateAsync(toFolder))
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException_Create);
        }

        foreach (var id in filesId)
        {
            var file = await fileDao.GetFileAsync(id);
            if (file is { Encrypted: false }
                && await destFileDao.IsExistAsync(file.Title, file.Category, toFolder.Id))
            {
                checkedFiles.Add(id);
            }
        }

        var folders = folderDao.GetFoldersAsync(foldersId);
        var foldersProject = folders.Where(folder => folder.FolderType == FolderType.BUNCH);
        var toSubfolders = destFolderDao.GetFoldersAsync(toFolder.Id);

        await foreach (var folderProject in foldersProject)
        {
            var toSub = await toSubfolders.FirstOrDefaultAsync(to => Equals(to.Title, folderProject.Title));
            if (toSub == null)
            {
                continue;
            }

            var filesPr = fileDao.GetFilesAsync(folderProject.Id).ToListAsync();
            var foldersTmp = folderDao.GetFoldersAsync(folderProject.Id);
            var foldersPr = foldersTmp.Select(d => d.Id).ToListAsync();

            var (cFiles, cFolders) = await MoveOrCopyFilesCheckAsync(await filesPr, await foldersPr, toSub.Id);
            checkedFiles.AddRange(cFiles);
            checkedFolders.AddRange(cFolders);
        }

        try
        {
            foreach (var pair in await folderDao.CanMoveOrCopyAsync(foldersId, toFolder.Id))
            {
                checkedFolders.Add(pair.Key);
            }
        }
        catch (Exception e)
        {
            throw GenerateException(e, _logger, authContext);
        }

        return (checkedFiles, checkedFolders);
    }

    #endregion


    public async Task<(List<int>, List<int>)> GetTrashContentAsync()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        var fileDao = daoFactory.GetFileDao<int>();
        var trashId = await folderDao.GetFolderIDTrashAsync(true);
        var foldersIdTask = await folderDao.GetFoldersAsync(trashId).Select(f => f.Id).ToListAsync();
        var filesIdTask = await fileDao.GetFilesAsync(trashId).ToListAsync();

        return (foldersIdTask, filesIdTask);
    }
    
    public async Task<FilesStatisticsResultDto> GetFilesUsedSpace()
    {
        var folderDao = daoFactory.GetFolderDao<int>();
        return await folderDao.GetFilesUsedSpace();
    }
    
    public async Task<FileReference> GetReferenceDataAsync<T>(string fileId, string portalName, T sourceFileId, string path, string link)
    {
        File<T> file = null;
        var fileDao = daoFactory.GetFileDao<T>();
        if (portalName == tenantManager.GetCurrentTenantId().ToString())
        {
            file = await fileDao.GetFileAsync((T)Convert.ChangeType(fileId, typeof(T)));
        }

        if (file == null && !string.IsNullOrEmpty(path) && string.IsNullOrEmpty(link))
        {
            var source = await fileDao.GetFileAsync(sourceFileId);

            if (source == null)
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_FileNotFound };
            }

            if (!await fileSecurity.CanReadAsync(source))
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFile };
            }

            var folderDao = daoFactory.GetFolderDao<T>();
            var folder = await folderDao.GetFolderAsync(source.ParentId);
            if (!await fileSecurity.CanReadAsync(folder))
            {
                return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFolder };
            }

            var list = fileDao.GetFilesAsync(folder.Id, new OrderBy(SortedByType.AZ, true), FilterType.FilesOnly, false, Guid.Empty, path, null, false);
            file = await list.FirstOrDefaultAsync(fileItem => fileItem.Title == path);
        }

        if (file == null && !string.IsNullOrEmpty(link))
        {
            if (!link.StartsWith(baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FilesBaseAbsolutePath)))
            {
                return new FileReference { Url = link };
            }

            var start = commonLinkUtility.ServerRootPath + "/s/";
            if (link.StartsWith(start))
            {
                await using var context = await dbContextFactory.CreateDbContextAsync();
                var decode = shortUrl.Decode(link[start.Length..]);
                var sl = await context.ShortLinks.FindAsync(decode);
                if (sl != null)
                {
                    link = sl.Link;
                }
            }

            var url = new UriBuilder(link);
            var id = HttpUtility.ParseQueryString(url.Query)[FilesLinkUtility.FileId];
            if (!string.IsNullOrEmpty(id))
            {
                if (fileId is string)
                {
                    var dao = daoFactory.GetFileDao<string>();
                    file = await dao.GetFileAsync(id) as File<T>;
                }
                else
                {
                    if (int.TryParse(id, out var resultId))
                    {
                        var dao = daoFactory.GetFileDao<int>();
                        file = await dao.GetFileAsync(resultId) as File<T>;
                    }
                }
            }
        }

        if (file == null)
        {
            return new FileReference { Error = FilesCommonResource.ErrorMessage_FileNotFound };
        }

        if (!await fileSecurity.CanReadAsync(file))
        {
            return new FileReference { Error = FilesCommonResource.ErrorMessage_SecurityException_ReadFile };
        }

        var fileStable = file;
        if (file.Forcesave != ForcesaveType.None)
        {
            fileStable = await fileDao.GetFileStableAsync(file.Id, file.Version);
        }

        var docKey = await documentServiceHelper.GetDocKeyAsync(fileStable);

        var fileReference = new FileReference
        {
            Path = file.Title,
            ReferenceData = new FileReferenceData { FileKey = file.Id.ToString(), InstanceId = (tenantManager.GetCurrentTenantId()).ToString() },
            Url = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file, lastVersion: true)),
            FileType = file.ConvertedExtension.Trim('.'),
            Key = docKey,
            Link = baseCommonLinkUtility.GetFullAbsolutePath(filesLinkUtility.GetFileWebEditorUrl(file.Id))
        };
        fileReference.Token = documentServiceHelper.GetSignature(fileReference);
        return fileReference;
    }
    
    internal static Exception GenerateException(Exception error, ILogger logger, AuthContext authContext, bool warning = false)
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
}

public class FileModel<T, TTempate>
{
    public T ParentId { get; init; }
    public string Title { get; set; }
    public TTempate TemplateId { get; init; }
    public int FormId { get; init; }
}