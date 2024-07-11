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

using Folder = Microsoft.SharePoint.Client.Folder;

namespace ASC.Files.Thirdparty.SharePoint;

[Scope]
internal class SharePointFileDao(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextManager,
    FileUtility fileUtility,
    CrossDao crossDao,
    SharePointDaoSelector sharePointDaoSelector,
    IFileDao<int> fileDao,
    SharePointDaoSelector regexDaoSelectorBase,
    Global global)
    : SharePointDaoBase(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextManager, fileUtility, regexDaoSelectorBase), IFileDao<string>
{
    private const string BytesTransferredKey = "BytesTransferred";
    
    public async Task InvalidateCacheAsync(string fileId)
    {
        await SharePointProviderInfo.InvalidateStorageAsync();
    }

    public async Task<File<string>> GetFileAsync(string fileId)
    {
        return await GetFileAsync(fileId, 1);
    }

    public async Task<File<string>> GetFileAsync(string fileId, int fileVersion)
    {
        return SharePointProviderInfo.ToFile(await SharePointProviderInfo.GetFileByIdAsync(fileId));
    }

    public async Task<File<string>> GetFileAsync(string parentId, string title)
    {
        var files = await SharePointProviderInfo.GetFolderFilesAsync(parentId);

        return SharePointProviderInfo.ToFile(files.FirstOrDefault(item => item.Name.Equals(title, StringComparison.InvariantCultureIgnoreCase)));
    }

    public async Task<File<string>> GetFileStableAsync(string fileId, int fileVersion = -1)
    {
        return SharePointProviderInfo.ToFile(await SharePointProviderInfo.GetFileByIdAsync(fileId));
    }

    public async IAsyncEnumerable<File<string>> GetFileHistoryAsync(string fileId)
    {
        var file = await GetFileAsync(fileId);
        yield return file;
    }

    public async IAsyncEnumerable<File<string>> GetFilesAsync(IEnumerable<string> fileIds)
    {
        if (fileIds == null || !fileIds.Any())
        {
            yield break;
        }

        foreach (var fileId in fileIds)
        {
            yield return SharePointProviderInfo.ToFile(await SharePointProviderInfo.GetFileByIdAsync(fileId));
        }
    }

    public IAsyncEnumerable<File<string>> GetFilesFilteredAsync(IEnumerable<string> fileIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, 
        string[] extension, bool searchInContent, bool checkShared = false)
    {
        if (fileIds == null || !fileIds.Any() || filterType == FilterType.FoldersOnly)
        {
            return AsyncEnumerable.Empty<File<string>>();
        }

        var files = GetFilesAsync(fileIds);

        //Filter
        if (subjectID != Guid.Empty)
        {
            files = files.WhereAwait(async x => subjectGroup
                                         ? await _userManager.IsUserInGroupAsync(x.CreateBy, subjectID)
                                         : x.CreateBy == subjectID);
        }

        switch (filterType)
        {
            case FilterType.DocumentsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Document);
                break;
            case FilterType.Pdf:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Pdf);
                break;
            case FilterType.PresentationsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Presentation);
                break;
            case FilterType.SpreadsheetsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Spreadsheet);
                break;
            case FilterType.ImagesOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Image);
                break;
            case FilterType.ArchiveOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Archive);
                break;
            case FilterType.MediaOnly:
                files = files.Where(x =>
                {
                    var fileType = FileUtility.GetFileTypeByFileName(x.Title);

                    return fileType is FileType.Audio or FileType.Video;
                });
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    searchText = searchText.Trim().ToLower();
                    files = files.Where(x => FileUtility.GetFileExtension(x.Title).Equals(searchText));
                }
                break;
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            files = files.Where(x => x.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        if (!extension.IsNullOrEmpty())
        {
            extension = extension.Select(e => e.Trim().ToLower()).ToArray();
            files = files.Where(x => extension.Contains(FileUtility.GetFileExtension(x.Title)));
        }

        return files;
    }

    public async IAsyncEnumerable<string> GetFilesAsync(string parentId)
    {
        var files = await SharePointProviderInfo.GetFolderFilesAsync(parentId);

        foreach (var entry in files)
        {
            yield return SharePointProviderInfo.ToFile(entry).Id;
        }
    }

    public async IAsyncEnumerable<File<string>> GetFilesAsync(string parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText,
        string[] extension, bool searchInContent, bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, string roomId = default, bool withShared = false)
    {
        if (filterType == FilterType.FoldersOnly)
        {
            yield break;
        }

        //Get only files
        var folderFiles = await SharePointProviderInfo.GetFolderFilesAsync(parentId);
        var files = folderFiles.Select(r => SharePointProviderInfo.ToFile(r)).ToAsyncEnumerable();

        //Filter
        if (subjectID != Guid.Empty)
        {
            files = files.WhereAwait(async x => subjectGroup
                                         ? await _userManager.IsUserInGroupAsync(x.CreateBy, subjectID)
                                         : x.CreateBy == subjectID);
        }

        switch (filterType)
        {
            case FilterType.DocumentsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Document);
                break;
            case FilterType.Pdf:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Pdf);
                break;
            case FilterType.PresentationsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Presentation);
                break;
            case FilterType.SpreadsheetsOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Spreadsheet);
                break;
            case FilterType.ImagesOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Image);
                break;
            case FilterType.ArchiveOnly:
                files = files.Where(x => FileUtility.GetFileTypeByFileName(x.Title) == FileType.Archive);
                break;
            case FilterType.MediaOnly:
                files = files.Where(x =>
                {
                    var fileType = FileUtility.GetFileTypeByFileName(x.Title);

                    return fileType is FileType.Audio or FileType.Video;
                });
                break;
            case FilterType.ByExtension:
                if (!string.IsNullOrEmpty(searchText))
                {
                    searchText = searchText.Trim().ToLower();
                    files = files.Where(x => FileUtility.GetFileExtension(x.Title).Equals(searchText));
                }
                break;
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            files = files.Where(x => x.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) != -1);
        }

        if (!extension.IsNullOrEmpty())
        {
            extension = extension.Select(e => e.Trim().ToLower()).ToArray();
            files = files.Where(x => extension.Contains(FileUtility.GetFileExtension(x.Title)));
        }

        orderBy ??= new OrderBy(SortedByType.DateAndTime, false);

        files = orderBy.SortedBy switch
        {
            SortedByType.Author => orderBy.IsAsc ? files.OrderBy(x => x.CreateBy) : files.OrderByDescending(x => x.CreateBy),
            SortedByType.AZ => orderBy.IsAsc ? files.OrderBy(x => x.Title) : files.OrderByDescending(x => x.Title),
            SortedByType.DateAndTime => orderBy.IsAsc ? files.OrderBy(x => x.ModifiedOn) : files.OrderByDescending(x => x.ModifiedOn),
            SortedByType.DateAndTimeCreation => orderBy.IsAsc ? files.OrderBy(x => x.CreateOn) : files.OrderByDescending(x => x.CreateOn),
            _ => orderBy.IsAsc ? files.OrderBy(x => x.Title) : files.OrderByDescending(x => x.Title)
        };

        await foreach (var f in files)
        {
            yield return f;
        }
    }

    public override Task<Stream> GetFileStreamAsync(File<string> file)
    {
        return GetFileStreamAsync(file, 0);
    }

    public async Task<Stream> GetFileStreamAsync(File<string> file, long offset)
    {
        var fileToDownload = await SharePointProviderInfo.GetFileByIdAsync(file.Id);
        if (fileToDownload == null)
        {
            throw new ArgumentNullException(nameof(file), FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var fileStream = await SharePointProviderInfo.GetFileStreamAsync(fileToDownload.ServerRelativeUrl, (int)offset);

        return fileStream;
    }

    
    public async Task<Stream> GetFileStreamAsync(File<string> file, long offset, long length)
    {
        return await GetFileStreamAsync(file, offset);
    }

    public async Task<long> GetFileSizeAsync(File<string> file)
    {
        var fileToDownload = await SharePointProviderInfo.GetFileByIdAsync(file.Id);
        if (fileToDownload == null)
        {
            throw new ArgumentNullException(nameof(file), FilesCommonResource.ErrorMessage_FileNotFound);
        }

        return SharePointProviderInfo.ToFile(fileToDownload).ContentLength;
    }
    
    public Task<string> GetPreSignedUriAsync(File<string> file, TimeSpan expires, string shareKey = null)
    {
        throw new NotSupportedException();
    }

    public Task<bool> IsSupportedPreSignedUriAsync(File<string> file)
    {
        return Task.FromResult(false);
    }

    public async Task<File<string>> SaveFileAsync(File<string> file, Stream fileStream, bool checkFolder)
    {
        return await SaveFileAsync(file, fileStream);
    }

    public async Task<File<string>> SaveFileAsync(File<string> file, Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        if (file.Id != null)
        {
            var sharePointFile = await SharePointProviderInfo.CreateFileAsync(file.Id, fileStream);

            var resultFile = SharePointProviderInfo.ToFile(sharePointFile);
            if (!sharePointFile.Name.Equals(file.Title))
            {
                var folder = await SharePointProviderInfo.GetFolderByIdAsync(file.ParentId);
                file.Title = await global.GetAvailableTitleAsync(file.Title, folder.ServerRelativeUrl, IsExistAsync);

                var id = await SharePointProviderInfo.RenameFileAsync(DaoSelector.ConvertId(resultFile.Id), file.Title);

                return await GetFileAsync(DaoSelector.ConvertId(id));
            }

            return resultFile;
        }

        if (file.ParentId != null)
        {
            var folder = await SharePointProviderInfo.GetFolderByIdAsync(file.ParentId);
            file.Title = await global.GetAvailableTitleAsync(file.Title, folder.ServerRelativeUrl, IsExistAsync);

            return SharePointProviderInfo.ToFile(await SharePointProviderInfo.CreateFileAsync(folder.ServerRelativeUrl + "/" + file.Title, fileStream));

        }

        return null;
    }

    public async Task<File<string>> ReplaceFileVersionAsync(File<string> file, Stream fileStream)
    {
        return await SaveFileAsync(file, fileStream);
    }
    public async Task DeleteFileAsync(string fileId,Guid ownerId)
    {
        await DeleteFileAsync(fileId);
    }
    public async Task DeleteFileAsync(string fileId)
    {
        await SharePointProviderInfo.DeleteFileAsync(fileId);
    }

    public async Task<bool> IsExistAsync(string title, string folderId)
    {
        var files = await SharePointProviderInfo.GetFolderFilesAsync(folderId);

        return files.Any(item => item.Name.Equals(title, StringComparison.InvariantCultureIgnoreCase));
    }
    

    public async Task<TTo> MoveFileAsync<TTo>(string fileId, TTo toFolderId, bool deleteLinks = false)
    {
        if (toFolderId is int tId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tId, deleteLinks));
        }

        if (toFolderId is string tsId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tsId, deleteLinks));
        }

        throw new NotImplementedException();
    }

    public async Task<int> MoveFileAsync(string fileId, int toFolderId, bool deleteLinks = false)
    {
        var moved = await crossDao.PerformCrossDaoFileCopyAsync(
            fileId, this, sharePointDaoSelector.ConvertId,
            toFolderId, fileDao, r => r,
            true)
            ;

        return moved.Id;
    }

    public async Task<string> MoveFileAsync(string fileId, string toFolderId, bool deleteLinks = false)
    {
        var newFileId = await SharePointProviderInfo.MoveFileAsync(fileId, toFolderId);
        await UpdatePathInDBAsync(SharePointProviderInfo.MakeId(fileId), newFileId);

        return newFileId;
    }

    public async Task<File<TTo>> CopyFileAsync<TTo>(string fileId, TTo toFolderId)
    {
        if (toFolderId is int tId)
        {
            return await CopyFileAsync(fileId, tId) as File<TTo>;
        }

        if (toFolderId is string tsId)
        {
            return await CopyFileAsync(fileId, tsId) as File<TTo>;
        }

        throw new NotImplementedException();
    }

    public async Task<File<int>> CopyFileAsync(string fileId, int toFolderId)
    {
        var moved = await crossDao.PerformCrossDaoFileCopyAsync(
            fileId, this, sharePointDaoSelector.ConvertId,
            toFolderId, fileDao, r => r,
            false)
            ;

        return moved;
    }

    public async Task<File<string>> CopyFileAsync(string fileId, string toFolderId)
    {
        return SharePointProviderInfo.ToFile(await SharePointProviderInfo.CopyFileAsync(fileId, toFolderId));
    }


    public async Task<string> FileRenameAsync(File<string> file, string newTitle)
    {
        var newFileId = await SharePointProviderInfo.RenameFileAsync(file.Id, newTitle);
        await UpdatePathInDBAsync(SharePointProviderInfo.MakeId(file.Id), newFileId);

        return newFileId;
    }

    public Task<string> UpdateCommentAsync(string fileId, int fileVersion, string comment)
    {
        return Task.FromResult(string.Empty);
    }

    public Task CompleteVersionAsync(string fileId, int fileVersion)
    {
        return Task.CompletedTask;
    }

    public Task ContinueVersionAsync(string fileId, int fileVersion)
    {
        return Task.FromResult(0);
    }

    public bool UseTrashForRemove(File<string> file)
    {
        return false;
    }

    public Task<ChunkedUploadSession<string>> CreateUploadSessionAsync(File<string> file, long contentLength)
    {
        return Task.FromResult(new ChunkedUploadSession<string>(FixId(file), contentLength) { UseChunks = false });
    }

    public async Task<File<string>> UploadChunkAsync(ChunkedUploadSession<string> uploadSession, Stream chunkStream, long chunkLength, int? chunkNumber = null)
    {
        if (uploadSession.UseChunks)
        {
            throw new NotImplementedException();
        }

        if (uploadSession.BytesTotal == 0)
        {
            uploadSession.BytesTotal = chunkLength;
        }

        uploadSession.File = await SaveFileAsync(uploadSession.File, chunkStream);
            
        uploadSession.Items[BytesTransferredKey] = chunkLength.ToString();

        return uploadSession.File;
    }

    public Task<File<string>> FinalizeUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        throw new NotImplementedException();
    }

    public Task AbortUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        return Task.FromResult(0);
        //throw new NotImplementedException();
    }

    private File<string> FixId(File<string> file)
    {
        if (file.Id != null)
        {
            file.Id = SharePointProviderInfo.MakeId(file.Id);
        }

        if (file.ParentId != null)
        {
            file.ParentId = SharePointProviderInfo.MakeId(file.ParentId);
        }

        return file;
    }

    public Task SetCustomOrder(string fileId, string parentFolderId, int order)
    {
        return Task.CompletedTask;
    }

    public Task InitCustomOrder(IEnumerable<string> fileIds, string parentFolderId)
    {
        return Task.CompletedTask;
    }

    public Task<long> GetTransferredBytesCountAsync(ChunkedUploadSession<string> uploadSession)
    {
        if (!long.TryParse(uploadSession.GetItemOrDefault<string>(BytesTransferredKey), out var transferred))
        {
            transferred = 0;
        }

        uploadSession.File = FixId(uploadSession.File);
        
        return Task.FromResult(transferred);
    }
}
